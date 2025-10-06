using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class EquippableItemDataTableSO : TableSO.Scripts.CsvTableSO<int, TableData.EquippableItemData>
    {
        public override TableType tableType => TableType.Csv;

        public override string csvPath { get => "EquippableItemData"; }
    }
}
