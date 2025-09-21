using UnityEngine;
using System;
using TableSO.Scripts;

namespace Table
{
    public class ExampleIconAssetTableSO : TableSO.Scripts.AssetTableSO<TableData.ExampleIconAsset>
    {
        public override TableType tableType => TableType.Asset;

        [SerializeField] private string assetFolderPath = "Assets/TableSO/Asset/ExampleIcon";
        public override string label { get => "ExampleIconAssetTableSO"; }
        public override Type assetType { get => typeof(Sprite); }
    }
}
