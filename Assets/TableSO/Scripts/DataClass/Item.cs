using System;
using System.Collections.Generic;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Merge Data Class - Made by TableSO MergeTableGenerator
/// Key Type: int
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class Item : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }
        /// <summary>
        /// Merge to EquippableItemDataTableSO data
        /// </summary>
        [field: SerializeField] public string EquippableItemDataID { get; set; }
        /// <summary>
        /// Merge to ItemIconAssetTableSO data
        /// </summary>
        [field: SerializeField] public string ItemIconAssetID { get; set; }
        /// <summary>
        /// Merge to ItemStringDataTableSO data
        /// </summary>
        [field: SerializeField] public string ItemStringDataID { get; set; }

        // Add your custom fields here
        // Example:
        // [field: SerializeField] public int CustomValue { get; set; }
        // [field: SerializeField] public string Description { get; set; }
    }
}
