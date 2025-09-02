using System;
using System.Collections.Generic;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Reference Data Class - Made by TableSO RefTableGenerator
/// Key Type: int
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class Item : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }
        /// <summary>
        /// Reference to ItemDataTableSO data
        /// </summary>
        [field: SerializeField] public string ItemDataID { get; set; }
        /// <summary>
        /// Reference to ItemStringDataTableSO data
        /// </summary>
        [field: SerializeField] public string ItemStringDataID { get; set; }
        /// <summary>
        /// Reference to ItemIconAssetTableSO data
        /// </summary>
        [field: SerializeField] public string ItemIconAssetID { get; set; }

        // Add your custom fields here
        // Example:
        // [field: SerializeField] public int CustomValue { get; set; }
        // [field: SerializeField] public string Description { get; set; }
    }
}
