using System;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Asset Data Class - Made by TableSO AssetTableGenerator
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class ExampleIconAsset : IIdentifiable<string>
    {
        [field: SerializeField] public string ID { get; internal set; }

        [field: SerializeField] public Sprite Asset { get; internal set; }

        public ExampleIconAsset(string id, Sprite asset)
        {
            this.ID = id;
            this.Asset = asset;
        }
    }
}
