using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.AddressableAssets;

namespace TableSO.Scripts
{
    public abstract class AssetTableSO<TData> : TableSO<string, TData>, IAssetData
        where TData : class, IIdentifiable<string>
    {
        public virtual string label { get; }
        public virtual Type assetType { get; }

        protected override void OnEnable() => tableType = TableType.Asset;

        public override void UpdateData()
        {
            LoadAllAssetsWithLabelGeneric(label, assetType);
        }

        protected void LoadAllAssetsWithLabelGeneric(string label, Type assetType)
        {
            MethodInfo method = typeof(AssetTableSO<TData>).GetMethod(nameof(LoadAllAssetsWithLabel), 
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo genericMethod = method.MakeGenericMethod(assetType);
            
            genericMethod.Invoke(this, new object[] { label });
        }

        protected void LoadAllAssetsWithLabel<TAsset>(string label) where TAsset : UnityEngine.Object
        {
            var constructor = typeof(TData).GetConstructor(new Type[] { typeof(string), typeof(TAsset) });
            if (constructor == null)
                return;
            
            dataList = new List<TData>();
            Addressables.LoadAssetsAsync<TAsset>(label, null).Completed += handle => {
                foreach (var asset in handle.Result)
                {
                    string id = asset.name;
                    TData item = constructor.Invoke(new object[] { id, asset }) as TData;
                    dataList.Add(item);
                } 
            };
        }
    }
}