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
using System.Reflection;
using TableSO.Scripts.Generator;

namespace TableSO.Scripts.Editor
{
    public class TableSOEditor : EditorWindow
    {
        private enum Tab
        {
            Center,
            Table,
            AssetTable,
            RefTable
        }

        private Tab currentTab = Tab.Center;
        private Vector2 scrollPosition;

        // Table Tab Variables
        private string csvFilePath = "";
        private string tableName = "";
        private bool tableAutoRegister = true;

        // AssetTable Tab Variables
        private string selectedFolderPath = "Assets/";
        private Type selectedAssetType = typeof(Sprite);
        private string assetTableName = "";
        private bool assetAutoRegister = true;
        private bool assetCreateAddressable = true;
        private string addressableGroupName = "";

        // Supported asset types
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

        [MenuItem("TableSO/TableSO Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<TableSOEditor>("TableSO Editor");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            LoadStyles();
        }

        private void LoadStyles()
        {
            // Style loading will be handled in OnGUI for better compatibility
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabButtons();
            DrawContent();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            };

            EditorGUILayout.LabelField("TableSO Editor", headerStyle);
            
            var lineRect = GUILayoutUtility.GetRect(1, 2);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1));
            
            EditorGUILayout.Space(5);
        }

        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35
            };

            var activeButtonStyle = new GUIStyle(buttonStyle);
            activeButtonStyle.normal.background = MakeTex(1, 1, new Color(0.3f, 0.5f, 0.8f, 1f));
            activeButtonStyle.hover.background = MakeTex(1, 1, new Color(0.3f, 0.5f, 0.8f, 1f));
            activeButtonStyle.active.background = MakeTex(1, 1, new Color(0.2f, 0.4f, 0.7f, 1f));

            if (GUILayout.Button("Center", currentTab == Tab.Center ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.Center;
            }

            if (GUILayout.Button("Table", currentTab == Tab.Table ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.Table;
            }

            if (GUILayout.Button("AssetTable", currentTab == Tab.AssetTable ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.AssetTable;
            }

            GUI.enabled = false; // RefTable is not implemented yet
            if (GUILayout.Button("RefTable", currentTab == Tab.RefTable ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.RefTable;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }

        private void DrawContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case Tab.Center:
                    DrawCenterTab();
                    break;
                case Tab.Table:
                    DrawTableTab();
                    break;
                case Tab.AssetTable:
                    DrawAssetTableTab();
                    break;
                case Tab.RefTable:
                    DrawRefTableTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCenterTab()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField("TableCenter Overview", titleStyle);

            // Find TableCenter
            string[] guids = AssetDatabase.FindAssets("t:TableCenter");
            
            if (guids.Length == 0)
            {
                DrawInfoBox("No TableCenter found in project", MessageType.Warning);
                
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Create TableCenter", GUILayout.Height(30)))
                {
                    CreateTableCenter();
                }
                return;
            }

            string tableCenterPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var tableCenter = AssetDatabase.LoadAssetAtPath<TableCenter>(tableCenterPath);

            if (tableCenter == null)
            {
                DrawInfoBox("Failed to load TableCenter", MessageType.Error);
                return;
            }

            // TableCenter Info
            DrawInfoBox($"TableCenter found: {tableCenter.name}", MessageType.Info);
            
            EditorGUILayout.Space(10);

            // Count different table types
            int totalTables = tableCenter.GetTableCount();
            int assetTables = tableCenter.GetAssetTableCount();
            int dataTables = tableCenter.GetCsvTableCount();
            int refTables = tableCenter.GetRefTableCount();
            
            // Display statistics
            DrawTableStatistics(totalTables, dataTables, assetTables, refTables);

            EditorGUILayout.Space(20);

            // Quick actions
            DrawQuickActions(tableCenter);
        }

        private void DrawTableStatistics(int total, int data, int asset, int reference)
        {
            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15)
            };

            EditorGUILayout.BeginVertical(boxStyle);
            
            var statStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            EditorGUILayout.LabelField("Registered Tables Statistics", statStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            DrawStatBox("Total", total, new Color(0.2f, 0.6f, 0.9f));
            DrawStatBox("Data Tables", data, new Color(0.3f, 0.8f, 0.3f));
            DrawStatBox("Asset Tables", asset, new Color(0.9f, 0.6f, 0.2f));
            DrawStatBox("Ref Tables", reference, new Color(0.8f, 0.3f, 0.8f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatBox(string label, int count, Color color)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            var countStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };

            EditorGUILayout.LabelField(count.ToString(), countStyle);
            EditorGUILayout.LabelField(label, labelStyle);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = originalColor;
        }

        private void DrawQuickActions(TableCenter tableCenter)
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };

            EditorGUILayout.LabelField("Quick Actions", titleStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh All Tables", GUILayout.Height(25)))
            {
                // Implement table refresh logic
                Debug.Log("[TableSO] Refreshing all tables...");
            }

            if (GUILayout.Button("Validate Tables", GUILayout.Height(25)))
            {
                // Implement table validation logic
                Debug.Log("[TableSO] Validating tables...");
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Select TableCenter in Project", GUILayout.Height(25)))
            {
                Selection.activeObject = tableCenter;
                EditorGUIUtility.PingObject(tableCenter);
            }
        }

        private void DrawTableTab()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField("CSV Table Generator", titleStyle);

            DrawSectionHeader("CSV File Selection");
            
            EditorGUILayout.BeginHorizontal();
            csvFilePath = EditorGUILayout.TextField("CSV File", csvFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select CSV File", "Assets", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        csvFilePath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        csvFilePath = path;
                    }
                    
                    // Auto-generate table name from CSV filename
                    tableName = Path.GetFileNameWithoutExtension(csvFilePath);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            DrawSectionHeader("Table Settings");
            tableName = EditorGUILayout.TextField("Table Name", tableName);

            EditorGUILayout.Space();

            DrawSectionHeader("Options");
            tableAutoRegister = EditorGUILayout.Toggle("Auto Register to TableCenter", tableAutoRegister);

            EditorGUILayout.Space();

            // Preview CSV content if file is selected
            if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
            {
                DrawCSVPreview();
            }

            EditorGUILayout.Space(20);

            // Generate button
            GUI.enabled = !string.IsNullOrEmpty(csvFilePath) && 
                         !string.IsNullOrEmpty(tableName) && 
                         File.Exists(csvFilePath);

            if (GUILayout.Button("Generate Table from CSV", GUILayout.Height(40)))
            {
                GenerateTableFromCSV();
            }
            GUI.enabled = true;
        }

        private void DrawAssetTableTab()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField("Asset Table Generator", titleStyle);

            DrawSectionHeader("Target Folder");
            
            EditorGUILayout.BeginHorizontal();
            selectedFolderPath = EditorGUILayout.TextField("Folder Path", selectedFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Asset Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            DrawSectionHeader("Asset Type");
            string[] typeNames = supportedTypes.Keys.ToArray();
            int selectedIndex = Array.IndexOf(typeNames, selectedAssetType.Name);
            if (selectedIndex == -1) selectedIndex = 0;
            
            selectedIndex = EditorGUILayout.Popup("Type", selectedIndex, typeNames);
            selectedAssetType = supportedTypes[typeNames[selectedIndex]];

            EditorGUILayout.Space();

            DrawSectionHeader("Table Settings");
            assetTableName = EditorGUILayout.TextField("Table Name", assetTableName);
            
            if (string.IsNullOrEmpty(assetTableName))
            {
                string folderName = Path.GetFileName(selectedFolderPath);
                if (!string.IsNullOrEmpty(folderName))
                {
                    assetTableName = $"{folderName}Asset";
                }
            }

            EditorGUILayout.Space();

            DrawSectionHeader("Options");
            assetAutoRegister = EditorGUILayout.Toggle("Auto Register to TableCenter", assetAutoRegister);
            assetCreateAddressable = EditorGUILayout.Toggle("Create Addressable Group", assetCreateAddressable);
            
            if (assetCreateAddressable)
            {
                addressableGroupName = EditorGUILayout.TextField("Addressable Group Name", 
                    string.IsNullOrEmpty(addressableGroupName) ? assetTableName : addressableGroupName);
            }

            EditorGUILayout.Space();

            // Preview assets
            if (!string.IsNullOrEmpty(selectedFolderPath) && Directory.Exists(selectedFolderPath))
            {
                DrawAssetPreview();
            }

            EditorGUILayout.Space(20);

            // Generate button
            GUI.enabled = !string.IsNullOrEmpty(selectedFolderPath) && 
                         !string.IsNullOrEmpty(assetTableName) && 
                         Directory.Exists(selectedFolderPath);

            if (GUILayout.Button("Generate Asset Table", GUILayout.Height(40)))
            {
                GenerateAssetTable();
            }
            GUI.enabled = true;
        }

        private void DrawRefTableTab()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField("Reference Table Generator", titleStyle);
            
            EditorGUILayout.Space(20);
            
            DrawInfoBox("RefTable functionality will be implemented in future updates.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            var placeholderStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(20, 20, 20, 20),
                fontSize = 12
            };
            
            EditorGUILayout.LabelField("Planned Features:\n• Cross-table references\n• Foreign key relationships\n• Dynamic data linking\n• Reference validation", placeholderStyle);
        }

        private void DrawSectionHeader(string title)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField(title, headerStyle);
        }

        private void DrawInfoBox(string message, MessageType messageType)
        {
            EditorGUILayout.HelpBox(message, messageType);
        }

        private void DrawCSVPreview()
        {
            try
            {
                string[] lines = File.ReadAllLines(csvFilePath);
                if (lines.Length >= 2)
                {
                    DrawInfoBox($"CSV Preview - {lines.Length - 2} data rows found", MessageType.Info);
                    
                    var previewStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        fontSize = 10,
                        padding = new RectOffset(10, 10, 10, 10)
                    };

                    EditorGUILayout.LabelField("Headers: " + lines[0], previewStyle);
                    EditorGUILayout.LabelField("Types: " + lines[1], previewStyle);
                    
                    if (lines.Length > 2)
                    {
                        EditorGUILayout.LabelField("Sample Data: " + lines[2], previewStyle);
                    }
                }
            }
            catch (Exception e)
            {
                DrawInfoBox($"Error reading CSV: {e.Message}", MessageType.Error);
            }
        }

        private void DrawAssetPreview()
        {
            var assets = GetAssetsInFolder(selectedFolderPath, selectedAssetType);
            DrawInfoBox($"Found {assets.Count} {selectedAssetType.Name} assets", MessageType.Info);
            
            if (assets.Count > 0 && assets.Count <= 10)
            {
                var previewStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10,
                    padding = new RectOffset(10, 10, 10, 10)
                };

                string preview = "Preview:\n" + string.Join("\n", assets.Take(10).Select(a => $"• {GetAssetName(a)} -> {a.name}"));
                EditorGUILayout.LabelField(preview, previewStyle);
            }
            else if (assets.Count > 10)
            {
                var previewStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10,
                    padding = new RectOffset(10, 10, 10, 10)
                };

                string preview = $"Preview (first 10 of {assets.Count}):\n" + 
                               string.Join("\n", assets.Take(10).Select(a => $"• {GetAssetName(a)} -> {a.name}"));
                EditorGUILayout.LabelField(preview, previewStyle);
            }
        }

        private void CreateTableCenter()
        {
            // Implementation for creating TableCenter
            Debug.Log("[TableSO] Creating TableCenter...");
            EditorUtility.DisplayDialog("Info", "TableCenter creation functionality needs to be implemented.", "OK");
        }

        private void GenerateTableFromCSV()
        {
            // Use the existing TableGenerator logic
            try
            {
                // Call the existing method from TableGenerator
                var method = typeof(TableSO.Scripts.Generator.TableGenerator)
                    .GetMethod("GenerateTableFromCSV", BindingFlags.NonPublic | BindingFlags.Static);
                
                TableGenerator.GenerateTableFromCSV(csvFilePath);

                return;
                if (method != null)
                {
                    method.Invoke(null, new object[] { csvFilePath });
                    Debug.Log($"[TableSO] Table '{tableName}' generated successfully from CSV");
                    EditorUtility.DisplayDialog("Success", $"Table '{tableName}' generated successfully!", "OK");
                }
                else
                {
                    Debug.LogError("[TableSO] Could not find GenerateTableFromCSV method");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating table from CSV: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate table:\n{e.Message}", "OK");
            }
        }

        private void GenerateAssetTable()
        {
            try
            {
                var assets = GetAssetsInFolder(selectedFolderPath, selectedAssetType);
                
                if (assets.Count == 0)
                {
                    EditorUtility.DisplayDialog("Warning", $"No {selectedAssetType.Name} assets found in the selected folder.", "OK");
                    return;
                }

                AssetTableGenerator.GenerateAssetTable(selectedFolderPath, assetTableName, selectedAssetType,
                    assetCreateAddressable, addressableGroupName, assetAutoRegister);
                // Use existing AssetTableGenerator logic
                // This would need to be adapted from the existing AssetTableGenerator class
                Debug.Log($"[TableSO] Asset table '{assetTableName}' generation started with {assets.Count} assets");
                // EditorUtility.DisplayDialog("Info", "Asset table generation functionality needs to be fully integrated.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating asset table: {e.Message}");
                // EditorUtility.DisplayDialog("Error", $"Failed to generate asset table:\n{e.Message}", "OK");
            }
        }

        private List<UnityEngine.Object> GetAssetsInFolder(string folderPath, Type assetType)
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

        private string GetAssetName(UnityEngine.Object asset)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset));
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}