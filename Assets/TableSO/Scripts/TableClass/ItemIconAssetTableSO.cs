using UnityEngine;
using TableData;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TableSO.Scripts;

namespace Table
{
    public class ItemIconAssetTableSO : TableSO.Scripts.AssetTableSO<TableData.ItemIconAsset>, IAssetData, IUpdatable
    {
        [SerializeField] private string assetFolderPath = "Assets/TableSO/Asset/ItemIcon";
        public string fileName => "ItemIconAssetTableSO";
        public Type assetType => typeof(Sprite);

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadAllAssetsFromFolder();
        }

        private void LoadAllAssetsFromFolder()
        {
#if UNITY_EDITOR
            if (dataList == null)
                dataList = new List<TableData.ItemIconAsset>();

            dataList.Clear();

            // Load all Sprite assets from the specified folder
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { assetFolderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (asset != null)
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    var assetData = new TableData.ItemIconAsset(assetName, asset, assetName);
                    dataList.Add(assetData);
                }
            }

            // Sort by name for consistency
            dataList = dataList.OrderBy(data => data.ID).ToList();
            
            // Mark as updated to refresh cache
            isUpdated = true;
            CacheData();
#endif
        }

        /// <summary>
        /// Get Sprite asset by ID (direct reference)
        /// </summary>
        public Sprite GetAsset(string id)
        {
            var data = GetData(id);
            return data?.Asset;
        }

        /// <summary>
        /// Get Sprite asset by ID asynchronously (Addressable)
        /// </summary>
        public async Task<Sprite> GetSpriteAsync(string id)
        {
            return await LoadAssetAsync<Sprite>(id);
        }

        /// <summary>
        /// Get Sprite asset by ID synchronously (Addressable)
        /// </summary>
        public Sprite GetSpriteSync(string id)
        {
            return LoadAssetSync<Sprite>(id);
        }

        /// <summary>
        /// Get addressable path for asset by ID
        /// </summary>
        public string GetAddressablePath(string id)
        {
            var data = GetData(id);
            return data?.AddressablePath ?? string.Empty;
        }

        /// <summary>
        /// Get all Sprite assets
        /// </summary>
        public Sprite[] GetAllSprites()
        {
            if (isUpdated) CacheData();
            return dataDict.Values.Select(data => data.Asset).Where(asset => asset != null).ToArray();
        }

        /// <summary>
        /// Manually refresh assets from folder (Editor only)
        /// </summary>
        [ContextMenu("Refresh Assets from Folder")]
        public void RefreshAssetsFromFolder()
        {
#if UNITY_EDITOR
            LoadAllAssetsFromFolder();
            EditorUtility.SetDirty(this);
            Debug.Log($"[TableSO] Refreshed {dataList.Count} assets for {name}");
#endif
        }
    }
}
