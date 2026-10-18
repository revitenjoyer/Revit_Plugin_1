using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitVolumeCalc.Core;
using RevitVolumeCalc.UI;

namespace RevitVolumeCalc.Commands
{
    /// <summary>
    /// Команда кнопки на ленте: считает объём текущего выделения и показывает (или обновляет) окно.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class CalculateVolumeCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApplication = commandData.Application;
            UIDocument uiDocument = uiApplication.ActiveUIDocument;
            if (uiDocument == null)
            {
                message = "Нет открытого документа.";
                return Result.Failed;
            }

            try
            {
                VolumeReport report = VolumeCalculator.Calculate(uiDocument);
                VolumeWindowHost.ShowReport(uiApplication, report);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
