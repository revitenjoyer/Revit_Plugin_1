using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitVolumeCalc.UI;

namespace RevitVolumeCalc.Core
{
    internal enum RevitRequestType
    {
        None,
        Refresh,
        Select
    }

    /// <summary>
    /// Немодальное окно не может обращаться к Revit API напрямую. Оно оставляет здесь запрос
    /// и вызывает ExternalEvent.Raise(); Revit затем выполняет Execute в допустимом контексте API.
    /// Если до выполнения пришло несколько запросов, выполняется последний.
    /// </summary>
    internal sealed class RevitRequestHandler : IExternalEventHandler
    {
        private readonly object _sync = new object();
        private RevitRequestType _requestType = RevitRequestType.None;
        private string _documentKey;
        private string _rowName;
        private List<ElementId> _idsToSelect;

        public void RequestRefresh()
        {
            lock (_sync)
            {
                _requestType = RevitRequestType.Refresh;
                _documentKey = null;
                _rowName = null;
                _idsToSelect = null;
            }
        }

        public void RequestSelect(string documentKey, string rowName, IEnumerable<ElementId> ids)
        {
            lock (_sync)
            {
                _requestType = RevitRequestType.Select;
                _documentKey = documentKey;
                _rowName = rowName;
                _idsToSelect = ids.ToList();
            }
        }

        public void Execute(UIApplication app)
        {
            RevitRequestType requestType;
            string documentKey;
            string rowName;
            List<ElementId> ids;

            lock (_sync)
            {
                requestType = _requestType;
                documentKey = _documentKey;
                rowName = _rowName;
                ids = _idsToSelect;

                _requestType = RevitRequestType.None;
                _documentKey = null;
                _rowName = null;
                _idsToSelect = null;
            }

            try
            {
                switch (requestType)
                {
                    case RevitRequestType.Refresh:
                        Refresh(app);
                        break;
                    case RevitRequestType.Select:
                        SelectElements(app, documentKey, rowName, ids);
                        break;
                }
            }
            catch (Exception ex)
            {
                VolumeWindowHost.ShowStatus("Ошибка: " + ex.Message);
            }
        }

        public string GetName()
        {
            return "RevitVolumeCalc: запрос из окна подсчёта объёма";
        }

        private static void Refresh(UIApplication app)
        {
            UIDocument uiDocument = app.ActiveUIDocument;
            if (uiDocument == null)
            {
                VolumeWindowHost.ShowStatus("Нет открытого документа.");
                return;
            }

            VolumeReport report = VolumeCalculator.Calculate(uiDocument);
            VolumeWindowHost.UpdateReport(report);
            VolumeWindowHost.ShowStatus("Пересчитано по текущему выделению.");
        }

        private static void SelectElements(UIApplication app, string documentKey, string rowName, List<ElementId> ids)
        {
            UIDocument uiDocument = app.ActiveUIDocument;
            if (uiDocument == null)
            {
                VolumeWindowHost.ShowStatus("Нет открытого документа.");
                return;
            }

            Document document = uiDocument.Document;
            if (VolumeCalculator.GetDocumentKey(document) != documentKey)
            {
                VolumeWindowHost.ShowStatus(
                    "Активен другой документ. Нажмите «Пересчитать», чтобы посчитать его выделение.");
                return;
            }

            // Элементы могли удалить после подсчёта — выделяем только существующие.
            List<ElementId> existingIds = (ids ?? new List<ElementId>())
                .Where(id => document.GetElement(id) != null)
                .ToList();

            uiDocument.Selection.SetElementIds(existingIds);

            string status = "Выделено в модели: " + rowName + " — " + existingIds.Count + " шт.";
            int missing = (ids == null ? 0 : ids.Count) - existingIds.Count;
            if (missing > 0)
            {
                status += " (" + missing + " уже нет в модели)";
            }
            VolumeWindowHost.ShowStatus(status);
        }
    }
}
