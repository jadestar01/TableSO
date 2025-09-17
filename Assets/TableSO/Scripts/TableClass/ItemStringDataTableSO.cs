using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class ItemStringDataTableSO : TableSO.Scripts.CsvTableSO<int, TableData.ItemStringData>
    {
        public override string csvPath { get => "ItemStringData"; }
    }
}
