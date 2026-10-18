using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RevitVolumeCalc.Commands;
using RevitVolumeCalc.UI;

namespace RevitVolumeCalc
{
    /// <summary>
    /// Точка входа плагина: создаёт вкладку на ленте и кнопку запуска.
    /// </summary>
    public class App : IExternalApplication
    {
        private const string TabName = "Объёмы";
        private const string PanelName = "Подсчёт";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                try
                {
                    application.CreateRibbonTab(TabName);
                }
                catch (Exception)
                {
                    // Вкладка с таким именем уже есть (например, её создала другая надстройка) —
                    // используем её. Если причина другая, CreateRibbonPanel ниже выбросит
                    // исключение, и оно будет показано пользователю.
                }

                RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                var buttonData = new PushButtonData(
                    "RevitVolumeCalc_Calculate",
                    "Посчитать\nобъём",
                    assemblyPath,
                    typeof(CalculateVolumeCommand).FullName);

                buttonData.ToolTip = "Суммарный объём выделенных элементов, в том числе по категориям.";
                buttonData.LongDescription =
                    "Считается встроенный параметр «Объём». Элементы без него показываются " +
                    "отдельной строкой «Без объёма» и в сумму не входят.";
                buttonData.LargeImage = LoadPng("Volume32.png");
                buttonData.Image = LoadPng("Volume16.png");

                panel.AddItem(buttonData);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RevitVolumeCalc", "Не удалось создать кнопку на ленте:\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            VolumeWindowHost.CloseWindow();
            return Result.Succeeded;
        }

        /// <summary>
        /// Загружает PNG из встроенных ресурсов сборки. Возвращает null, если ресурс не найден:
        /// кнопка тогда просто будет без иконки.
        /// </summary>
        private static BitmapSource LoadPng(string fileName)
        {
            Assembly assembly = typeof(App).Assembly;
            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
            {
                return null;
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                var decoder = new PngBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
                BitmapSource frame = decoder.Frames[0];
                if (frame.CanFreeze && !frame.IsFrozen)
                {
                    frame.Freeze();
                }
                return frame;
            }
        }
    }
}
