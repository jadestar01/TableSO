#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace TableSO.Scripts.Generator
{
    public class AssetTableGenerator
    {
        public static void GenerateAssetTable(string selectedFolderPath ,string tableName,
            Type selectedAssetType, bool createAddressableGroup)
        {
            try
            {
                var assets = GetAssetsInFolder(selectedFolderPath, selectedAssetType);
                
                if (assets.Count == 0)
                {
                    EditorUtility.DisplayDialog("Warning", $"No {selectedAssetType.Name} assets found in the selected folder.", "OK");
                    return;
                }

                GenerateAssetDataClass(tableName, selectedAssetType);
                GenerateAssetTableSO(tableName, selectedAssetType, selectedFolderPath);
                
                if (createAddressableGroup)
                {
                    CreateAddressableGroup(selectedFolderPath, $"{tableName}TableSO");
                }

                AssetDatabase.Refresh();
                
                Debug.Log($"[TableSO] Asset table '{tableName}' generated successfully with {assets.Count} assets");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating asset table: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate asset table:\n{e.Message}", "OK");
            }
        }

        private static List<UnityEngine.Object> GetAssetsInFolder(string folderPath, Type assetType)
        {
            List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            string[] guids;
            if (assetType == typeof(GameObject))
                guids = AssetDatabase.FindAssets($"t:Prefab", new[] { folderPath });
            else
                guids = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { folderPath });

            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            return assets.OrderBy(a => a.name).ToList();
        }

        private static string GetAssetName(UnityEngine.Object asset)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset));
        }

        private static void GenerateAssetDataClass(string className, Type assetType)
        {
            StringBuilder classCode = new StringBuilder();
            
            classCode.AppendLine("using System;");
            classCode.AppendLine("using UnityEngine;");
            classCode.AppendLine("using TableSO.Scripts;");
            classCode.AppendLine();
            classCode.AppendLine("/// <summary>");
            classCode.AppendLine("/// Asset Data Class - Made by TableSO AssetTableGenerator");
            classCode.AppendLine("/// </summary>");
            classCode.AppendLine();
            classCode.AppendLine("namespace TableData");
            classCode.AppendLine("{");
            classCode.AppendLine("    [System.Serializable]");
            classCode.AppendLine($"    public class {className} : IIdentifiable<string>");
            classCode.AppendLine("    {");
            classCode.AppendLine("        [field: SerializeField] public string ID { get; internal set; }");
            classCode.AppendLine();
            classCode.AppendLine($"        [field: SerializeField] public {assetType.Name} Asset {{ get; internal set; }}");
            classCode.AppendLine();
            classCode.AppendLine($"        public {className}(string id, {assetType.Name} asset)");
            classCode.AppendLine("        {");
            classCode.AppendLine("            this.ID = id;");
            classCode.AppendLine("            this.Asset = asset;");
            classCode.AppendLine("        }");
            classCode.AppendLine("    }");
            classCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.DATA_CLASS_PATH);
            string classFilePath = Path.Combine(FilePath.DATA_CLASS_PATH, $"{className}.cs");
            File.WriteAllText(classFilePath, classCode.ToString());
        }

        private static void GenerateAssetTableSO(string className, Type assetType, string folderPath)
        {
            StringBuilder tableCode = new StringBuilder();

            tableCode.AppendLine("using UnityEngine;");
            tableCode.AppendLine("using System;");
            tableCode.AppendLine("using TableSO.Scripts;");
            tableCode.AppendLine();
            tableCode.AppendLine("namespace Table");
            tableCode.AppendLine("{");
            tableCode.AppendLine(
                $"    public class {className}TableSO : TableSO.Scripts.AssetTableSO<TableData.{className}>");
            tableCode.AppendLine("    {");
            tableCode.AppendLine($"        public override TableType tableType => TableType.Asset;\n");
            tableCode.AppendLine($"        [SerializeField] private string assetFolderPath = \"{folderPath}\";");
            tableCode.AppendLine($"        public override string label {{ get => \"{className}TableSO\"; }}");
            tableCode.AppendLine($"        public override Type assetType {{ get => typeof({assetType.Name}); }}");
            tableCode.AppendLine("    }");
            tableCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.TABLE_CLASS_PATH);
            string tableFilePath = Path.Combine(FilePath.TABLE_CLASS_PATH, $"{className}TableSO.cs");
            File.WriteAllText(tableFilePath, tableCode.ToString());
        }
        
        
        private static void CreateAddressableGroup(string folderPath, string tableName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[TableSO] Addressables not initialized.");
                return;
            }

            var group = settings.FindGroup(tableName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    tableName,
                    false,
                    false,
                    true,
                    null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema)
                );

                var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundleSchema != null)
                {
                    bundleSchema.BuildPath.SetVariableByName(settings, "LocalBuildPath");
                    bundleSchema.LoadPath.SetVariableByName(settings, "LocalLoadPath");

                    bundleSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
                }
            }
            
            string folderGUID = AssetDatabase.AssetPathToGUID(folderPath);
            if (string.IsNullOrEmpty(folderGUID))
                return;

            var entry = settings.CreateOrMoveEntry(folderGUID, group, false, false);
            Debug.Log("folderPath");
            entry.address = folderPath;
            entry.SetLabel(tableName, true, true);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            Debug.Log($"[TableSO] Created Addressable group '{tableName}'");
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif