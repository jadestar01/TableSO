using UnityEngine;
using System;

namespace Table
{
    public class ItemIconAssetTableSO : TableSO.Scripts.AssetTableSO<TableData.ItemIconAsset>
    {
        [SerializeField] private string assetFolderPath = "Assets/TableSO/Asset/ItemIcon";
        public override string label { get => "ItemIconAssetTableSO"; }
        public override Type assetType { get => typeof(Sprite); }
    }
}
