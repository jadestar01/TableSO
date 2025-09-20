using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class ExampleDataTableSO : TableSO.Scripts.CsvTableSO<int, TableData.ExampleData>
    {
        public override TableType tableType => TableType.Csv;

        public override string csvPath { get => "ExampleData"; }

        public void Method()
        {
        }
    }
}
