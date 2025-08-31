using System;
using UnityEngine;

/// <summary>
/// Asset Data Class - Made by TableSO AssetTableGenerator
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class ItemIconAsset : IIdentifiable<string>
    {
        [field: SerializeField] public string ID { get; internal set; }

        [field: SerializeField] public Sprite Asset { get; internal set; }

        [field: SerializeField] public string AddressablePath { get; internal set; }

        public ItemIconAsset(string id, Sprite asset, string addressablePath = "")
        {
            this.ID = id;
            this.Asset = asset;
            this.AddressablePath = addressablePath;
        }
    }
}
