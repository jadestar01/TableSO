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

namespace TableSO.Scripts.Generator
{
    public class AssetTableGenerator : EditorWindow
    {
        private string selectedFolderPath = "Assets/";
        private Type selectedAssetType = typeof(Sprite);
        private string tableName = "";
        private bool autoRegisterToTableCenter = true;
        private bool createAddressableGroup = true;
        private string addressableGroupName = "";
        
        private readonly Dictionary<string, Type> supportedTypes = new Dictionary<string, Type>()
        {
            {"Sprite", typeof(Sprite)},
            {"Texture2D", typeof(Texture2D)},
            {"AudioClip", typeof(AudioClip)},
            {"AnimationClip", typeof(AnimationClip)},
            {"GameObject", typeof(GameObject)},
            {"Material", typeof(Material)},
            {"ScriptableObject", typeof(ScriptableObject)},
            {"TextAsset", typeof(TextAsset)}
        };

        [MenuItem("TableSO/Asset Table Generator")]
        public static void ShowWindow()
        {
            GetWindow<AssetTableGenerator>("Asset Table Generator");
        }
        
        public static void GenerateAssetTable(string selectedFolderPath ,string tableName,
            Type selectedAssetType, bool createAddressableGroup,string addressableGroupName, bool autoRegister)
        {
            try
            {
                var assets = GetAssetsInFolder(selectedFolderPath, selectedAssetType);
                
                if (assets.Count == 0)
                {
                    EditorUtility.DisplayDialog("Warning", $"No {selectedAssetType.Name} assets found in the selected folder.", "OK");
                    return;
                }

                // Generate Data Class
                GenerateAssetDataClass(tableName, selectedAssetType);
                
                // Generate TableSO Class
                GenerateAssetTableSO(tableName, selectedAssetType, selectedFolderPath);
                
                if (createAddressableGroup)
                {
                    CreateAddressableGroup(assets, addressableGroupName);
                }

                // Refresh to compile new scripts
                AssetDatabase.Refresh();
                
                Debug.Log($"[TableSO] Asset table '{tableName}' generated successfully with {assets.Count} assets");
                EditorUtility.DisplayDialog("Success", $"Asset table '{tableName}' generated successfully!", "OK");
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
            
            string[] guids = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { folderPath });
            
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
            classCode.AppendLine("        [field: SerializeField] public string AddressablePath { get; internal set; }");
            classCode.AppendLine();
            classCode.AppendLine($"        public {className}(string id, {assetType.Name} asset, string addressablePath = \"\")");
            classCode.AppendLine("        {");
            classCode.AppendLine("            this.ID = id;");
            classCode.AppendLine("            this.Asset = asset;");
            classCode.AppendLine("            this.AddressablePath = addressablePath;");
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
            tableCode.AppendLine("using TableData;");
            tableCode.AppendLine("using System.Threading.Tasks;");
            tableCode.AppendLine("using System.Linq;");
            tableCode.AppendLine("using System;");
            tableCode.AppendLine("using System.Collections.Generic;");
            tableCode.AppendLine("using UnityEditor;");
            tableCode.AppendLine("using System.IO;");
            tableCode.AppendLine("using TableSO.Scripts;");
            tableCode.AppendLine();
            tableCode.AppendLine("namespace Table");
            tableCode.AppendLine("{");
            tableCode.AppendLine($"    [CreateAssetMenu(fileName = \"{className}TableSO\", menuName = \"TableSO/AssetTable/{className}Table\")]");
            tableCode.AppendLine($"    public class {className}TableSO : TableSO.Scripts.AssetTableSO<TableData.{className}>, IAssetData");
            tableCode.AppendLine("    {");
            tableCode.AppendLine($"        [SerializeField] private string assetFolderPath = \"{folderPath}\";");
            tableCode.AppendLine($"        public string fileName => \"{className}TableSO\";");
            tableCode.AppendLine($"        public Type assetType => typeof({assetType.Name});");
            tableCode.AppendLine();
            tableCode.AppendLine("        protected override void OnEnable()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            base.OnEnable();");
            tableCode.AppendLine("            LoadAllAssetsFromFolder();");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine("        private void LoadAllAssetsFromFolder()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("#if UNITY_EDITOR");
            tableCode.AppendLine("            if (dataList == null)");
            tableCode.AppendLine($"                dataList = new List<TableData.{className}>();");
            tableCode.AppendLine();
            tableCode.AppendLine("            dataList.Clear();");
            tableCode.AppendLine();
            tableCode.AppendLine($"            // Load all {assetType.Name} assets from the specified folder");
            tableCode.AppendLine($"            string[] guids = AssetDatabase.FindAssets(\"t:{assetType.Name}\", new[] {{ assetFolderPath }});");
            tableCode.AppendLine();
            tableCode.AppendLine("            foreach (string guid in guids)");
            tableCode.AppendLine("            {");
            tableCode.AppendLine("                string assetPath = AssetDatabase.GUIDToAssetPath(guid);");
            tableCode.AppendLine($"                var asset = AssetDatabase.LoadAssetAtPath<{assetType.Name}>(assetPath);");
            tableCode.AppendLine();
            tableCode.AppendLine("                if (asset != null)");
            tableCode.AppendLine("                {");
            tableCode.AppendLine("                    string assetName = Path.GetFileNameWithoutExtension(assetPath);");
            tableCode.AppendLine($"                    var assetData = new TableData.{className}(assetName, asset, assetName);");
            tableCode.AppendLine("                    dataList.Add(assetData);");
            tableCode.AppendLine("                }");
            tableCode.AppendLine("            }");
            tableCode.AppendLine();
            tableCode.AppendLine("            // Sort by name for consistency");
            tableCode.AppendLine("            dataList = dataList.OrderBy(data => data.ID).ToList();");
            tableCode.AppendLine("            ");
            tableCode.AppendLine("            // Mark as updated to refresh cache");
            tableCode.AppendLine("            isUpdated = true;");
            tableCode.AppendLine("            CacheData();");
            tableCode.AppendLine("#endif");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine($"        /// <summary>");
            tableCode.AppendLine($"        /// Get {assetType.Name} asset by ID (direct reference)");
            tableCode.AppendLine($"        /// </summary>");
            tableCode.AppendLine($"        public {assetType.Name} GetAsset(string id)");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            var data = GetData(id);");
            tableCode.AppendLine("            return data?.Asset;");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine($"        /// <summary>");
            tableCode.AppendLine($"        /// Get {assetType.Name} asset by ID asynchronously (Addressable)");
            tableCode.AppendLine($"        /// </summary>");
            tableCode.AppendLine($"        public async Task<{assetType.Name}> Get{assetType.Name}Async(string id)");
            tableCode.AppendLine("        {");
            tableCode.AppendLine($"            return await LoadAssetAsync<{assetType.Name}>(id);");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine($"        /// <summary>");
            tableCode.AppendLine($"        /// Get {assetType.Name} asset by ID synchronously (Addressable)");
            tableCode.AppendLine($"        /// </summary>");
            tableCode.AppendLine($"        public {assetType.Name} Get{assetType.Name}Sync(string id)");
            tableCode.AppendLine("        {");
            tableCode.AppendLine($"            return LoadAssetSync<{assetType.Name}>(id);");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine("        /// <summary>");
            tableCode.AppendLine("        /// Get addressable path for asset by ID");
            tableCode.AppendLine("        /// </summary>");
            tableCode.AppendLine("        public string GetAddressablePath(string id)");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            var data = GetData(id);");
            tableCode.AppendLine("            return data?.AddressablePath ?? string.Empty;");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine($"        /// <summary>");
            tableCode.AppendLine($"        /// Get all {assetType.Name} assets");
            tableCode.AppendLine($"        /// </summary>");
            tableCode.AppendLine($"        public {assetType.Name}[] GetAll{assetType.Name}s()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            if (isUpdated) CacheData();");
            tableCode.AppendLine($"            return dataDict.Values.Select(data => data.Asset).Where(asset => asset != null).ToArray();");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine("        /// <summary>");
            tableCode.AppendLine("        /// Manually refresh assets from folder (Editor only)");
            tableCode.AppendLine("        /// </summary>");
            tableCode.AppendLine("        [ContextMenu(\"Refresh Assets from Folder\")]");
            tableCode.AppendLine("        public void RefreshAssetsFromFolder()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("#if UNITY_EDITOR");
            tableCode.AppendLine("            LoadAllAssetsFromFolder();");
            tableCode.AppendLine("            EditorUtility.SetDirty(this);");
            tableCode.AppendLine("            Debug.Log($\"[TableSO] Refreshed {dataList.Count} assets for {name}\");");
            tableCode.AppendLine("#endif");
            tableCode.AppendLine("        }");
            tableCode.AppendLine("    }");
            tableCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.TABLE_CLASS_PATH);
            string tableFilePath = Path.Combine(FilePath.TABLE_CLASS_PATH, $"{className}TableSO.cs");
            File.WriteAllText(tableFilePath, tableCode.ToString());
        }

        private static void CreateAssetTableSO(string className, List<UnityEngine.Object> assets, bool autoRegisterToTableCenter)
        {
            try
            {
                // Find the generated TableSO class type
                var tableSOType = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => t.Name == $"{className}TableSO");

                if (tableSOType == null)
                {
                    // Try to find in all loaded assemblies
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        tableSOType = assembly.GetTypes()
                            .FirstOrDefault(t => t.Name == $"{className}TableSO");
                        if (tableSOType != null) break;
                    }
                }

                if (tableSOType == null)
                {
                    Debug.LogError($"[TableSO] Could not find generated class {className}TableSO. Please compile and try again.");
                    return;
                }

                // Create ScriptableObject instance
                var tableInstance = ScriptableObject.CreateInstance(tableSOType);
                
                // Use reflection to set the dataList
                var dataListField = tableSOType.BaseType.GetField("dataList", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dataListField != null)
                {
                    // Create data entries
                    var dataType = System.Type.GetType($"TableData.{className}");
                    if (dataType != null)
                    {
                        var dataList = Activator.CreateInstance(typeof(List<>).MakeGenericType(dataType));
                        var addMethod = dataList.GetType().GetMethod("Add");

                        foreach (var asset in assets)
                        {
                            string assetName = GetAssetName(asset);
                            string _assetPath = AssetDatabase.GetAssetPath(asset);
                            
                            var dataInstance = Activator.CreateInstance(dataType, assetName, asset, _assetPath);
                            addMethod.Invoke(dataList, new[] { dataInstance });
                        }

                        dataListField.SetValue(tableInstance, dataList);
                    }
                }

                // Save the ScriptableObject
                EnsureDirectoryExists(FilePath.TABLE_OUTPUT_PATH);
                string assetPath = Path.Combine(FilePath.TABLE_OUTPUT_PATH, $"{className}TableSO.asset");
                AssetDatabase.CreateAsset(tableInstance, assetPath);
                
                // Register to TableCenter if option is enabled
                if (autoRegisterToTableCenter)
                {
                    RegisterToTableCenter(tableInstance as ScriptableObject);
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Ping the created asset
                EditorGUIUtility.PingObject(tableInstance);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error creating ScriptableObject: {e.Message}");
            }
        }

        private static void RegisterToTableCenter(ScriptableObject tableInstance)
        {
            try
            {
                // Find TableCenter asset
                string[] guids = AssetDatabase.FindAssets("t:TableCenter");
                if (guids.Length > 0)
                {
                    string tableCenterPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var tableCenter = AssetDatabase.LoadAssetAtPath<TableCenter>(tableCenterPath);
                    
                    if (tableCenter != null)
                    {
                        tableCenter.RegisterTable(tableInstance);
                        EditorUtility.SetDirty(tableCenter);
                        Debug.Log($"[TableSO] {tableInstance.name} registered to TableCenter");
                    }
                }
                else
                {
                    Debug.LogWarning("[TableSO] No TableCenter found. Please create one first.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TableSO] Could not register to TableCenter: {e.Message}");
            }
        }

        private static void CreateAddressableGroup(List<UnityEngine.Object> assets, string groupName)
        {
            try
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    Debug.LogWarning("[TableSO] Addressables not initialized. Please initialize Addressables first.");
                    return;
                }

                // Create or find existing group
                var group = settings.FindGroup(groupName);
                if (group == null)
                {
                    group = settings.CreateGroup(groupName, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
                }

                // Add assets to the group
                foreach (var asset in assets)
                {
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                    
                    var entry = settings.CreateOrMoveEntry(assetGUID, group, false, false);
                    entry.address = GetAssetName(asset); // Use filename as address
                }

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                Debug.Log($"[TableSO] Created Addressable group '{groupName}' with {assets.Count} assets");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TableSO] Could not create Addressable group: {e.Message}");
            }
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