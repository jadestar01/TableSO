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

        public Sprite itemSprite;
        public string itemName;
        public ItemType itemType;
        public ItemQuality itemQuality;
        
        public Item(Sprite itemSprite, string itemName, ItemType itemType, ItemQuality itemQuality)
        {
            this.itemSprite = itemSprite;
            this.itemName = itemName;
            this.itemType = itemType;
            this.itemQuality = itemQuality;
        }
    }
}
