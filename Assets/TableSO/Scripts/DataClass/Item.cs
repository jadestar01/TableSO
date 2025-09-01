using System;
using System.Collections.Generic;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Reference Data Class - Made by TableSO RefTableGenerator
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class Item : IIdentifiable<string>
    {
        [field: SerializeField] public string ID { get; internal set; }

        [field: SerializeField] public string ItemDataID { get; internal set; }
        [field: SerializeField] public string ItemIconAssetID { get; internal set; }
        [field: SerializeField] public string ItemStringDataID { get; internal set; }


        public Item(string id, string itemdataID = "", string itemiconassetID = "", string itemstringdataID = "")
        {
            this.ID = id;
            this.ItemDataID = itemdataID;
            this.ItemIconAssetID = itemiconassetID;
            this.ItemStringDataID = itemstringdataID;
        }
    }
}
