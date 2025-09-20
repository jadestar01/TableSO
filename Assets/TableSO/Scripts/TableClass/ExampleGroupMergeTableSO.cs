using UnityEngine;
using TableData;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using TableSO.Scripts;

/// <summary>
/// Merge Table - Made by TableSO MergeTableGenerator
/// Key Type: int
/// Referenced Tables: ExampleDataTableSO, ExampleSpriteAssetTableSO
/// </summary>

namespace Table
{
    public class ExampleGroupMergeTableSO : TableSO.Scripts.MergeTableSO<int, TableData.ExampleGroup>
    {
        public override TableType tableType => TableType.Merge;

        public string fileName => "ExampleGroupMergeTableSO";
        [SerializeField] private ExampleDataTableSO ExampleDataTable;
        [SerializeField] private ExampleSpriteAssetTableSO ExampleSpriteAssetTable;

        public override List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(ExampleDataTableSO),
            typeof(ExampleSpriteAssetTableSO),
        };

        public override async Task UpdateData()
        {
            ReleaseData();
            foreach (var data in ExampleDataTable.dataList)
            {
                List<Sprite> spriteList = new List<Sprite>();
                foreach (var spriteData in data.IconName)
                    spriteList.Add(ExampleSpriteAssetTable.GetData(spriteData).Asset);
                
                dataList.Add(new TableData.ExampleGroup(data.ID, spriteList, data.EnumEle, data.Text));
            }
            base.UpdateData();
        }

        public override TableData.ExampleGroup GetData(int key)
        {
            // TODO: Implement GetData logic
            // This should return the RefData that matches the key
            // You may want to create data dynamically based on referenced tables
            return base.GetData(key);
        }
    }
}
