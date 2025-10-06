using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace TableSO.Scripts
{
    public abstract class AssetTableSO<TData> : TableSO<string, TData>, IAssetData
        where TData : class, IIdentifiable<string>
    {
        // Constructor cache to avoid repeated reflection
        private static ConstructorInfo _cachedConstructor;
        private static Type _cachedAssetType;
        
        // Generic method cache
        private static MethodInfo _cachedGenericMethod;
        private static Type _lastAssetTypeUsed;

        public override TableType tableType => TableType.Asset;
        public virtual string label { get; }
        public virtual Type assetType { get; }
        
        public override async Task UpdateData()
        {
            ReleaseData();
            await LoadAllAssetsWithLabelGeneric(label, assetType);
            ClearConstructorCache();
            CacheData();
            base.UpdateData();
        }

        protected async Task LoadAllAssetsWithLabelGeneric(string label, Type assetType)
        {
            if (_cachedGenericMethod == null || _lastAssetTypeUsed != assetType)
            {
                MethodInfo method = typeof(AssetTableSO<TData>).GetMethod(
                    nameof(LoadAllAssetsWithLabel),
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                _cachedGenericMethod = method.MakeGenericMethod(assetType);
                _lastAssetTypeUsed = assetType;
            }

            var task = (Task)_cachedGenericMethod.Invoke(this, new object[] { label });
            await task;
        }
        
        protected async Task LoadAllAssetsWithLabel<TAsset>(string label) where TAsset : UnityEngine.Object
        {
            Type currentAssetType = typeof(TAsset);
            if (_cachedConstructor == null || _cachedAssetType != currentAssetType)
            {
                _cachedConstructor = typeof(TData).GetConstructor(new Type[] { typeof(string), currentAssetType });
                _cachedAssetType = currentAssetType;
            }

            if (_cachedConstructor == null)
            {
                UnityEngine.Debug.LogError($"[AssetTableSO] No constructor found for TData with parameters (string, {currentAssetType.Name})");
                return;
            }

            var handle = Addressables.LoadAssetsAsync<TAsset>(label, null);
            
            IList<TAsset> assets;
            try
            {
                assets = await handle.Task;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[AssetTableSO] Failed to load assets with label '{label}': {e.Message}");
                return;
            }

            if (assets == null || assets.Count == 0)
            {
                dataList = new List<TData>(0);
                return;
            }

            dataList = new List<TData>(assets.Count);

            object[] constructorArgs = new object[2];

            for (int i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null) continue;

                constructorArgs[0] = asset.name;
                constructorArgs[1] = asset;

                try
                {
                    TData item = (TData)_cachedConstructor.Invoke(constructorArgs);
                    dataList.Add(item);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[AssetTableSO] Failed to create TData instance for asset '{asset.name}': {e.Message}");
                }
            }
        }

        // Optional: Clear caches when needed (e.g., domain reload, cleanup)
        protected static void ClearConstructorCache()
        {
            _cachedConstructor = null;
            _cachedAssetType = null;
            _cachedGenericMethod = null;
            _lastAssetTypeUsed = null;
        }
    }
}