using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using TableSO.Scripts.Utility;

namespace TableSO.Scripts
{
    public abstract class AssetTableSO<TData> : TableSO<string, TData> 
        where TData : class, IIdentifiable<string>
    {
        [Header("Asset Table Settings")]
        [SerializeField] protected bool preloadOnAwake = false;
        [SerializeField] protected bool useAddressableLoading = true;
        
        protected bool isPreloaded = false;

        protected virtual void Start()
        {
            if (preloadOnAwake && useAddressableLoading)
            {
                _ = PreloadAllAssetsAsync();
            }
        }

        public virtual List<string> GetAllAssetIDs()
        {
            if (isUpdated) CacheData();
            return dataDict.Keys.ToList();
        }
        
        public virtual List<TData> GetAssetsByFilter(System.Func<TData, bool> predicate)
        {
            if (isUpdated) CacheData();
            return dataDict.Values.Where(predicate).ToList();
        }
        
        public virtual bool HasAsset(string id)
        {
            return IsContains(id) && GetData(id) != null;
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
        
        public virtual async Task<List<T>> LoadAssetsAsync<T>(List<string> ids) where T : Object
        {
            List<Task<T>> loadTasks = new List<Task<T>>();
            
            foreach (string id in ids)
            {
                loadTasks.Add(LoadAssetAsync<T>(id));
            }

            var results = await Task.WhenAll(loadTasks);
            return results.Where(r => r != null).ToList();
        }
        
        public virtual int GetAssetCount()
        {
            if (isUpdated) CacheData();
            return dataDict.Count;
        }
        
        public virtual void ClearPreloadedAssets()
        {
            if (useAddressableLoading)
            {
                AddressableAssetLoader.ClearCache();
                isPreloaded = false;
            }
        }

        protected override void OnDataUpdated()
        {
            base.OnDataUpdated();
            isPreloaded = false; // Reset preload status when data is updated
        }
        
        [ContextMenu("Validate Asset References")]
        public virtual void ValidateAssetReferences()
        {
            if (isUpdated) CacheData();
            
            int validCount = 0;
            int invalidCount = 0;
            
            foreach (var kvp in dataDict)
            {
                var data = kvp.Value;
                var assetProperty = data.GetType().GetProperty("Asset");
                
                if (assetProperty != null)
                {
                    var asset = assetProperty.GetValue(data) as Object;
                    if (asset != null)
                    {
                        validCount++;
                    }
                    else
                    {
                        invalidCount++;
                        Debug.LogWarning($"[TableSO] Missing asset reference for ID: {kvp.Key}");
                    }
                }
            }
            
            Debug.Log($"[TableSO] Validation complete - Valid: {validCount}, Invalid: {invalidCount}");
        }
    }
}