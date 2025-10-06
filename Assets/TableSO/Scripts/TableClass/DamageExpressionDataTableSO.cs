using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class DamageExpressionDataTableSO : TableSO.Scripts.CsvTableSO<int, TableData.DamageExpressionData>
    {
        public override TableType tableType => TableType.Csv;

        public override string csvPath { get => "DamageExpressionData"; }
    }
}
