using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RevitVolumeCalc.Core;
using ExternalEvent = Autodesk.Revit.UI.ExternalEvent;
using ExternalEventRequest = Autodesk.Revit.UI.ExternalEventRequest;

namespace RevitVolumeCalc.UI
{
    /// <summary>
    /// Немодальное окно с результатом. Напрямую Revit API не вызывает:
    /// все действия в модели идут через ExternalEvent.
    /// </summary>
    public partial class VolumeWindow : Window
    {
        private readonly RevitRequestHandler _handler;
        private readonly ExternalEvent _externalEvent;
        private VolumeReport _report;

        internal VolumeWindow(RevitRequestHandler handler, ExternalEvent externalEvent)
        {
            InitializeComponent();
            _handler = handler;
            _externalEvent = externalEvent;
        }

        internal void SetReport(VolumeReport report)
        {
            _report = report;

            TotalText.Text = VolumeFormat.FormatWithUnit(report.TotalVolumeM3);

            if (report.SelectedCount == 0)
            {
                SummaryText.Text = "Ничего не выделено. Выделите элементы в модели и нажмите «Пересчитать».";
            }
            else
            {
                string summary =
                    "С объёмом: " + report.ElementsWithVolume + " шт. · без объёма: " +
                    report.ElementsWithoutVolume + " шт.";
                if (report.GroupsExpanded)
                {
                    summary += "\nГруппы и сборки раскрыты до отдельных элементов.";
                }
                summary += "\nДокумент: " + report.DocumentTitle;
                SummaryText.Text = summary;
            }

            var rows = new List<VolumeRow>(report.CategoryRows);
            if (report.NoVolumeRow != null)
            {
                rows.Add(report.NoVolumeRow);
            }

            RowsGrid.ItemsSource = rows;
            RowsGrid.UnselectAll();
        }

        internal void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
        }

        private void OnRowClick(object sender, MouseButtonEventArgs e)
        {
            var gridRow = sender as DataGridRow;
            var row = gridRow == null ? null : gridRow.Item as VolumeRow;
            if (row == null || _report == null)
            {
                return;
            }

            _handler.RequestSelect(_report.DocumentKey, row.Name, row.ElementIds);
            RaiseExternalEvent("Выделяю: " + row.Name + "…");
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            _handler.RequestRefresh();
            RaiseExternalEvent("Пересчитываю…");
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RaiseExternalEvent(string pendingStatus)
        {
            ExternalEventRequest result = _externalEvent.Raise();
            if (result == ExternalEventRequest.Accepted || result == ExternalEventRequest.Pending)
            {
                SetStatus(pendingStatus);
            }
            else
            {
                SetStatus("Revit сейчас занят (например, открыт диалог или идёт редактирование). Повторите действие.");
            }
        }
    }
}
