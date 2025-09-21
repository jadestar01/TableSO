using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TableSO.Scripts
{
    public abstract class AssetTableSO<TData> : TableSO<string, TData>, IAssetData
        where TData : class, IIdentifiable<string>
    {
        public override TableType tableType => TableType.Asset;
        public virtual string label { get; }
        public virtual Type assetType { get; }
        
        public override async Task UpdateData()
        {
            ReleaseData();
            await LoadAllAssetsWithLabelGeneric(label, assetType);
            CacheData();
            base.UpdateData();
        }

        protected async Task LoadAllAssetsWithLabelGeneric(string label, Type assetType)
        {
            MethodInfo method = typeof(AssetTableSO<TData>).GetMethod(
                nameof(LoadAllAssetsWithLabel),
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            MethodInfo genericMethod = method.MakeGenericMethod(assetType);

            var task = (Task)genericMethod.Invoke(this, new object[] { label });
            await task;
        }
        
        protected async Task LoadAllAssetsWithLabel<TAsset>(string label) where TAsset : UnityEngine.Object
        {
            var constructor = typeof(TData).GetConstructor(new Type[] { typeof(string), typeof(TAsset) });
            if (constructor == null)
                return;

            dataList = new List<TData>();

            var handle = Addressables.LoadAssetsAsync<TAsset>(label, null);
            var assets = await handle.Task;

            foreach (var asset in assets)
            {
                string id = asset.name;
                TData item = constructor.Invoke(new object[] { id, asset }) as TData;
                dataList.Add(item);
            }
        }

    }
}