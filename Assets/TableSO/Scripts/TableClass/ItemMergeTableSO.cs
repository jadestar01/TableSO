using UnityEngine;
using TableData;
using System.Collections.Generic;
using System.Linq;
using System;
using TableSO.Scripts;

/// <summary>
/// Merge Table - Made by TableSO MergeTableGenerator
/// Key Type: int
/// Referenced Tables: EquippableItemDataTableSO, ItemIconAssetTableSO, ItemStringDataTableSO
/// </summary>

namespace Table
{
    public class ItemMergeTableSO : TableSO.Scripts.MergeTableSO<int, TableData.Item>
    {
        public string fileName => "ItemMergeTableSO";
        [SerializeField] private EquippableItemDataTableSO EquippableItemDataTable;
        [SerializeField] private ItemIconAssetTableSO ItemIconAssetTable;
        [SerializeField] private ItemStringDataTableSO ItemStringDataTable;

        public override List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(EquippableItemDataTableSO),
            typeof(ItemIconAssetTableSO),
            typeof(ItemStringDataTableSO),
        };

        public override void UpdateData()
        {
            // TODO: Implement UpdateDatalogic
        }

        public override TableData.Item GetData(int key)
        {
            // TODO: Implement GetData logic
            // This should return the RefData that matches the key
            // You may want to create data dynamically based on referenced tables
            return base.GetData(key);
        }
    }
}
