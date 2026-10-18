using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitVolumeCalc.Core
{
    /// <summary>
    /// Подсчёт объёма по встроенному параметру «Объём» (BuiltInParameter.HOST_VOLUME_COMPUTED).
    /// Объём никогда не вычисляется по геометрии.
    /// </summary>
    internal static class VolumeCalculator
    {
        internal const string NoVolumeRowName = "Без объёма";
        private const string NoCategoryName = "Без категории";

        public static VolumeReport Calculate(UIDocument uiDocument)
        {
            Document document = uiDocument.Document;
            ICollection<ElementId> selectedIds = uiDocument.Selection.GetElementIds();

            bool groupsExpanded;
            List<Element> elements = ExpandSelection(document, selectedIds, out groupsExpanded);

            var categories = new Dictionary<ElementId, CategoryAccumulator>();
            var withoutVolume = new List<ElementId>();
            double totalVolumeM3 = 0.0;
            int elementsWithVolume = 0;

            foreach (Element element in elements)
            {
                double volumeM3;
                if (!TryGetVolumeM3(element, out volumeM3))
                {
                    withoutVolume.Add(element.Id);
                    continue;
                }

                Category category = element.Category;
                ElementId categoryKey = category != null ? category.Id : ElementId.InvalidElementId;

                CategoryAccumulator accumulator;
                if (!categories.TryGetValue(categoryKey, out accumulator))
                {
                    accumulator = new CategoryAccumulator(category != null ? category.Name : NoCategoryName);
                    categories.Add(categoryKey, accumulator);
                }

                accumulator.VolumeM3 += volumeM3;
                accumulator.ElementIds.Add(element.Id);
                totalVolumeM3 += volumeM3;
                elementsWithVolume++;
            }

            List<VolumeRow> categoryRows = categories.Values
                .OrderByDescending(a => a.VolumeM3)
                .ThenBy(a => a.Name)
                .Select(a => new VolumeRow(a.Name, a.VolumeM3, a.ElementIds, false))
                .ToList();

            VolumeRow noVolumeRow = withoutVolume.Count > 0
                ? new VolumeRow(NoVolumeRowName, 0.0, withoutVolume, true)
                : null;

            return new VolumeReport(
                GetDocumentKey(document),
                document.Title,
                selectedIds.Count,
                groupsExpanded,
                totalVolumeM3,
                elementsWithVolume,
                categoryRows,
                noVolumeRow);
        }

        /// <summary>
        /// Ключ документа. У несохранённого проекта PathName пустой, поэтому добавляем Title.
        /// </summary>
        public static string GetDocumentKey(Document document)
        {
            return document.Title + "|" + document.PathName;
        }

        /// <summary>
        /// Читает встроенный параметр «Объём» и переводит его из внутренних единиц (куб. футы) в м³.
        /// false — параметра нет или он не заполнен (элемент попадёт в «Без объёма»).
        /// Значение 0 считается допустимым объёмом.
        /// </summary>
        private static bool TryGetVolumeM3(Element element, out double volumeM3)
        {
            volumeM3 = 0.0;

            Parameter parameter = element.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
            if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
            {
                return false;
            }

            double internalValue = parameter.AsDouble();
            if (double.IsNaN(internalValue) || double.IsInfinity(internalValue))
            {
                return false;
            }

            volumeM3 = UnitUtils.ConvertFromInternalUnits(internalValue, UnitTypeId.CubicMeters);
            return true;
        }

        /// <summary>
        /// Раскрывает группы (в том числе вложенные) и сборки до отдельных элементов.
        /// Каждый элемент учитывается один раз, даже если он выделен и сам, и в составе группы.
        /// </summary>
        private static List<Element> ExpandSelection(
            Document document,
            IEnumerable<ElementId> selectedIds,
            out bool groupsExpanded)
        {
            groupsExpanded = false;
            var result = new List<Element>();
            var visited = new HashSet<ElementId>();
            var pending = new Stack<ElementId>(selectedIds);

            while (pending.Count > 0)
            {
                ElementId id = pending.Pop();
                if (object.ReferenceEquals(id, null) || id == ElementId.InvalidElementId || !visited.Add(id))
                {
                    continue;
                }

                Element element = document.GetElement(id);
                if (element == null)
                {
                    continue;
                }

                var group = element as Group;
                if (group != null)
                {
                    groupsExpanded = true;
                    foreach (ElementId memberId in group.GetMemberIds())
                    {
                        pending.Push(memberId);
                    }
                    continue;
                }

                var assembly = element as AssemblyInstance;
                if (assembly != null)
                {
                    groupsExpanded = true;
                    foreach (ElementId memberId in assembly.GetMemberIds())
                    {
                        pending.Push(memberId);
                    }
                    continue;
                }

                result.Add(element);
            }

            return result;
        }

        private sealed class CategoryAccumulator
        {
            public CategoryAccumulator(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public double VolumeM3 { get; set; }

            public List<ElementId> ElementIds { get; } = new List<ElementId>();
        }
    }
}
