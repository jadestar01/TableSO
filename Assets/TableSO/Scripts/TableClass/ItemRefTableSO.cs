using UnityEngine;
using TableData;
using System.Collections.Generic;
using System.Linq;
using System;
using TableSO.Scripts;

/// <summary>
/// Reference Table - Made by TableSO RefTableGenerator
/// Key Type: int
/// Referenced Tables: ItemDataTableSO, ItemStringDataTableSO, ItemIconAssetTableSO
/// </summary>

namespace Table
{
    public class ItemRefTableSO : TableSO.Scripts.RefTableSO<int, TableData.Item>, IReferencable, IUpdatable
    {
        public string fileName => "ItemRefTableSO";
        [SerializeField] private ItemDataTableSO ItemDataTable;
        [SerializeField] private ItemStringDataTableSO ItemStringDataTable;
        [SerializeField] private ItemIconAssetTableSO ItemIconAssetTable;

        public List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(ItemDataTableSO),
            typeof(ItemStringDataTableSO),
            typeof(ItemIconAssetTableSO),
        };

        public override void UpdateData()
        {
            foreach (var itemData in ItemDataTable.GetAllData())
            {
                var stringData = ItemStringDataTable.GetData(itemData.ID);
                var iconData = ItemIconAssetTable.GetData(itemData.ID.ToString());
                if (stringData != null &&
                    iconData != null)
                {
                    dataList.Add(new Item(iconData.Asset, stringData.Name, itemData.ItemType, itemData.ItemQuality));
                }
            }
        }

        /// <summary>
        /// Get RefTable data by key
        /// </summary>
        public override TableData.Item GetData(int key)
        {
            // TODO: Implement GetData logic
            // This should return the RefData that matches the key
            // You may want to create data dynamically based on referenced tables
            return base.GetData(key);
        }
    }
}
