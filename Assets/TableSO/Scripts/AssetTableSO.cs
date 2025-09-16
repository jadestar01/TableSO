using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TableSO.Scripts.Utility;
using Object = UnityEngine.Object;

namespace TableSO.Scripts
{
    public abstract class AssetTableSO<TData> : TableSO<string, TData>, IAssetData
        where TData : class, IIdentifiable<string>
    {
        public Type assetType { get; }
        
        [Header("Asset Table Settings")]
        [SerializeField] protected bool preloadOnAwake = false;
        [SerializeField] protected bool useAddressableLoading = true;
        
        protected bool isPreloaded = false;
        
        protected override void OnEnable()
        {
            tableType = TableType.Asset;
            UpdateData();
            CacheData();
        }

        protected virtual void Start()
        {
            if (preloadOnAwake && useAddressableLoading)
            {
                _ = PreloadAllAssetsAsync();
            }
        }
        
        public virtual async Task PreloadAllAssetsAsync()
        {
            if (!useAddressableLoading || isPreloaded)
                return;

            if (isUpdated) CacheData();

            List<string> addresses = GetAllAddressablePaths();
            
            if (addresses.Count > 0)
            {
                await AddressableAssetLoader.PreloadAssetsAsync<Object>(addresses);
                isPreloaded = true;
                Debug.Log($"[TableSO] Preloaded {addresses.Count} assets for {name}");
            }
        }
        
        protected virtual List<string> GetAllAddressablePaths()
        {
            List<string> paths = new List<string>();
            
            foreach (var data in dataDict.Values)
            {
                // This assumes the asset data has an AddressablePath property
                // Override this method in derived classes if needed
                var pathProperty = data.GetType().GetProperty("AddressablePath");
                if (pathProperty != null)
                {
                    string path = pathProperty.GetValue(data) as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }
            }
            
            return paths;
        }
        
        public virtual async Task<T> LoadAssetAsync<T>(string id) where T : Object
        {
            var data = GetData(id);
            if (data == null) return null;

            var pathProperty = data.GetType().GetProperty("AddressablePath");
            if (pathProperty != null && useAddressableLoading)
            {
                string addressablePath = pathProperty.GetValue(data) as string;
                if (!string.IsNullOrEmpty(addressablePath))
                {
                    return await AddressableAssetLoader.LoadAssetAsync<T>(addressablePath);
                }
            }

            var assetProperty = data.GetType().GetProperty("Asset");
            if (assetProperty != null)
            {
                return assetProperty.GetValue(data) as T;
            }

            return null;
        }
        
        public virtual T LoadAssetSync<T>(string id) where T : Object
        {
            var data = GetData(id);
            if (data == null) return null;

            var pathProperty = data.GetType().GetProperty("AddressablePath");
            if (pathProperty != null && useAddressableLoading)
            {
                string addressablePath = pathProperty.GetValue(data) as string;
                if (!string.IsNullOrEmpty(addressablePath))
                {
                    return AddressableAssetLoader.LoadAssetSync<T>(addressablePath);
                }
            }

            var assetProperty = data.GetType().GetProperty("Asset");
            if (assetProperty != null)
            {
                return assetProperty.GetValue(data) as T;
            }

            return null;
        }
        
        public override void UpdateData()
        {
            base.UpdateData();
        }
    }
}