using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
#endif

namespace TableSO.Scripts.Utility
{
    /// <summary>
    /// Utility class for loading Addressable assets
    /// Provides both sync and async loading methods
    /// </summary>
    public static class AddressableAssetLoader
    {
#if ADDRESSABLES_ENABLED
        private static Dictionary<string, UnityEngine.Object> _cachedAssets = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// Asynchronously load an asset by address
        /// </summary>
        public static async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AddressableAssetLoader] Empty address provided");
                return null;
            }

            // Check cache first
            if (_cachedAssets.TryGetValue(address, out UnityEngine.Object cachedAsset))
            {
                return cachedAsset as T;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                var asset = await handle.Task;
                
                if (asset != null)
                {
                    _cachedAssets[address] = asset;
                }
                
                return asset;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableAssetLoader] Failed to load asset '{address}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronously load an asset by address (blocking operation)
        /// Note: This should only be used when async loading is not possible
        /// </summary>
        public static T LoadAssetSync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AddressableAssetLoader] Empty address provided");
                return null;
            }

            // Check cache first
            if (_cachedAssets.TryGetValue(address, out UnityEngine.Object cachedAsset))
            {
                return cachedAsset as T;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                var asset = handle.WaitForCompletion();
                
                if (asset != null)
                {
                    _cachedAssets[address] = asset;
                }
                
                return asset;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableAssetLoader] Failed to load asset sync '{address}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load multiple assets asynchronously
        /// </summary>
        public static async Task<List<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
        {
            List<T> results = new List<T>();
            List<Task<T>> tasks = new List<Task<T>>();

            foreach (string address in addresses)
            {
                tasks.Add(LoadAssetAsync<T>(address));
            }

            var assets = await Task.WhenAll(tasks);
            results.AddRange(assets);

            return results;
        }

        /// <summary>
        /// Preload assets into cache
        /// </summary>
        public static async Task PreloadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
        {
            await LoadAssetsAsync<T>(addresses);
            Debug.Log($"[AddressableAssetLoader] Preloaded {addresses.Count} assets");
        }

        /// <summary>
        /// Release a cached asset
        /// </summary>
        public static void ReleaseAsset(string address)
        {
            if (_cachedAssets.TryGetValue(address, out UnityEngine.Object asset))
            {
                _cachedAssets.Remove(address);
                
                try
                {
                    Addressables.Release(asset);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AddressableAssetLoader] Error releasing asset '{address}': {e.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all cached assets
        /// </summary>
        public static void ClearCache()
        {
            foreach (var kvp in _cachedAssets)
            {
                try
                {
                    Addressables.Release(kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AddressableAssetLoader] Error releasing cached asset: {e.Message}");
                }
            }
            
            _cachedAssets.Clear();
            Debug.Log("[AddressableAssetLoader] Cache cleared");
        }

        /// <summary>
        /// Get the number of cached assets
        /// </summary>
        public static int GetCacheCount()
        {
            return _cachedAssets.Count;
        }

        /// <summary>
        /// Check if an asset is cached
        /// </summary>
        public static bool IsAssetCached(string address)
        {
            return _cachedAssets.ContainsKey(address);
        }

#else
        // Fallback methods when Addressables is not available
        public static async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            Debug.LogWarning("[AddressableAssetLoader] Addressables not available, using Resources.Load");
            return Resources.Load<T>(address);
        }

        public static T LoadAssetSync<T>(string address) where T : UnityEngine.Object
        {
            Debug.LogWarning("[AddressableAssetLoader] Addressables not available, using Resources.Load");
            return Resources.Load<T>(address);
        }

        public static async Task<List<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
        {
            List<T> results = new List<T>();
            foreach (string address in addresses)
            {
                var asset = Resources.Load<T>(address);
                if (asset != null)
                    results.Add(asset);
            }
            return results;
        }

        public static async Task PreloadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
        {
            Debug.LogWarning("[AddressableAssetLoader] Addressables not available, preloading skipped");
        }

        public static void ReleaseAsset(string address)
        {
            Debug.LogWarning("[AddressableAssetLoader] Addressables not available, release skipped");
        }

        public static void ClearCache()
        {
            Debug.LogWarning("[AddressableAssetLoader] Addressables not available, cache clear skipped");
        }

        public static int GetCacheCount() => 0;
        public static bool IsAssetCached(string address) => false;
#endif
    }
}