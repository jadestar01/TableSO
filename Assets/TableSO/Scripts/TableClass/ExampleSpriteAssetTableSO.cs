using UnityEngine;
using System;
using TableSO.Scripts;

namespace Table
{
    public class ExampleSpriteAssetTableSO : TableSO.Scripts.AssetTableSO<TableData.ExampleSpriteAsset>
    {
        public override TableType tableType => TableType.Asset;

        [SerializeField] private string assetFolderPath = "Assets/TableSO/Asset/ExampleSprite";
        public override string label { get => "ExampleSpriteAssetTableSO"; }
        public override Type assetType { get => typeof(Sprite); }
    }
}
