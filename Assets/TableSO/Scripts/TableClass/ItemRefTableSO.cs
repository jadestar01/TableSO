using UnityEngine;
using TableData;
using System.Collections.Generic;
using System.Linq;
using System;
using TableSO.Scripts;

namespace Table
{
    public class ItemRefTableSO : TableSO.Scripts.RefTableSO<string, TableData.Item>, IReferencable
    {
        public string fileName => "ItemRefTableSO";
        [SerializeField] private ItemDataTableSO ItemData;
        [SerializeField] private ItemIconAssetTableSO ItemIconAsset;
        [SerializeField] private ItemStringDataTableSO ItemStringData;
        public List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(ItemDataTableSO),
            typeof(ItemIconAssetTableSO),
            typeof(ItemStringDataTableSO),
        };
        protected override void OnRefreshFromReferencedTables()
        {
            // TODO: Implement custom refresh logic
            // This method is called when RefreshFromReferencedTables() is invoked
            // You can update your dataList based on referenced tables here
            
            base.OnRefreshFromReferencedTables();
        }
    }
}
