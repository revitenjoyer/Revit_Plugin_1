using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;

namespace RevitVolumeCalc.Core
{
    /// <summary>
    /// Результат подсчёта по одному выделению.
    /// </summary>
    public sealed class VolumeReport
    {
        internal VolumeReport(
            string documentKey,
            string documentTitle,
            int selectedCount,
            bool groupsExpanded,
            double totalVolumeM3,
            int elementsWithVolume,
            IReadOnlyList<VolumeRow> categoryRows,
            VolumeRow noVolumeRow)
        {
            DocumentKey = documentKey;
            DocumentTitle = documentTitle;
            SelectedCount = selectedCount;
            GroupsExpanded = groupsExpanded;
            TotalVolumeM3 = totalVolumeM3;
            ElementsWithVolume = elementsWithVolume;
            CategoryRows = categoryRows;
            NoVolumeRow = noVolumeRow;
        }

        /// <summary>Ключ документа, по которому проверяем, что выделяем элементы в том же проекте.</summary>
        internal string DocumentKey { get; }

        public string DocumentTitle { get; }

        /// <summary>Сколько элементов было выделено пользователем (до раскрытия групп).</summary>
        public int SelectedCount { get; }

        /// <summary>В выделении были группы или сборки, и они раскрыты до отдельных элементов.</summary>
        public bool GroupsExpanded { get; }

        /// <summary>Точная (неокруглённая) сумма объёмов, м³.</summary>
        public double TotalVolumeM3 { get; }

        public int ElementsWithVolume { get; }

        public int ElementsWithoutVolume
        {
            get { return NoVolumeRow == null ? 0 : NoVolumeRow.Count; }
        }

        public IReadOnlyList<VolumeRow> CategoryRows { get; }

        /// <summary>Строка «Без объёма» или null, если таких элементов нет.</summary>
        public VolumeRow NoVolumeRow { get; }
    }

    /// <summary>
    /// Строка таблицы: категория или «Без объёма».
    /// </summary>
    public sealed class VolumeRow
    {
        internal VolumeRow(string name, double volumeM3, IReadOnlyList<ElementId> elementIds, bool isNoVolumeRow)
        {
            Name = name;
            VolumeM3 = volumeM3;
            ElementIds = elementIds;
            IsNoVolumeRow = isNoVolumeRow;
        }

        public string Name { get; }

        /// <summary>Точный (неокруглённый) объём, м³.</summary>
        public double VolumeM3 { get; }

        public bool IsNoVolumeRow { get; }

        public int Count
        {
            get { return ElementIds.Count; }
        }

        /// <summary>Объём для отображения, округлён до 0,01 м³.</summary>
        public string VolumeText
        {
            get { return IsNoVolumeRow ? "—" : VolumeFormat.Format(VolumeM3); }
        }

        internal IReadOnlyList<ElementId> ElementIds { get; }
    }

    internal static class VolumeFormat
    {
        private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

        /// <summary>Округление до 0,01 м³ (половина — от нуля) и формат «1 234,56».</summary>
        public static string Format(double volumeM3)
        {
            return Math.Round(volumeM3, 2, MidpointRounding.AwayFromZero).ToString("N2", Russian);
        }

        public static string FormatWithUnit(double volumeM3)
        {
            return Format(volumeM3) + " м³";
        }
    }
}
