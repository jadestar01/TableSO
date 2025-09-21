using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;
using TableSO.Scripts.Generator;
using UnityEditor.AddressableAssets;

namespace TableSO.Scripts.Editor
{
    public class TableSOEditor : EditorWindow
    {
        private enum Tab
        {
            Center,
            CsvTable,
            AssetTable,
            MergeTable
        }

        private Tab currentTab = Tab.Center;
        private Vector2 scrollPosition;

        // CsvTable Tab Variables
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

        // MergeTable Variables
        private string mergeTableName = "";
        private bool refAutoRegister = true;
        private List<ScriptableObject> selectedReferenceTables = new List<ScriptableObject>();
        private bool showAdvancedOptions = false;
        private Vector2 operationsScrollPosition;
        private Vector2 tablesScrollPosition;
        private string mergeTableKeyType = "string";
        private string[] commonKeyTypes = { "string", "int", "float", "bool" };

        // Asset List Variables
        private Vector2 assetListScrollPosition;
        private bool showAssetList = true;
        private Dictionary<string, bool> assetFoldouts = new Dictionary<string, bool>();

        public bool autoReload = false;

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

        [MenuItem("TableSO/TableSO Editor %t")]
        public static void ShowWindow()
        {
            var window = GetWindow<TableSOEditor>("TableSO Editor");
            window.minSize = new Vector2(500, 400);
        }
        
        private void OnEnable()
        {
            autoReload = EditorPrefs.GetBool("AutoReload");
            LoadStyles();
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool("AutoReload", autoReload);
        }
        
        #region Editor Drawing Methods
        private void LoadStyles()
        {
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

            if (GUILayout.Button("CsvTable", currentTab == Tab.CsvTable ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.CsvTable;
            }

            if (GUILayout.Button("AssetTable", currentTab == Tab.AssetTable ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.AssetTable;
            }

            if (GUILayout.Button("MergeTable", currentTab == Tab.MergeTable ? activeButtonStyle : buttonStyle))
            {
                currentTab = Tab.MergeTable;
            }

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
                case Tab.CsvTable:
                    DrawTableTab();
                    break;
                case Tab.AssetTable:
                    DrawAssetTableTab();
                    break;
                case Tab.MergeTable:
                    DrawMergeTableTab();
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

            EditorGUILayout.Space(10);

            var allTables = GetTablesByInterface();
            var dataTables = allTables.Where(t => GetTableType(t) == TableType.Csv).ToList();
            var assetTables = allTables.Where(t => GetTableType(t) == TableType.Asset).ToList();
            var mergeTables = allTables.Where(t => GetTableType(t) == TableType.Merge).ToList();
            
            DrawTableStatistics(allTables.Count, dataTables.Count, assetTables.Count, mergeTables.Count);

            EditorGUILayout.Space(20);

            DrawQuickActions(tableCenter);

            EditorGUILayout.Space(20);

            DrawAllTablesList();
        }

        private void DrawAllTablesList()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };

            showAssetList = EditorGUILayout.Foldout(showAssetList, "All Registered Tables", true);
            
            if (!showAssetList) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            assetListScrollPosition = EditorGUILayout.BeginScrollView(assetListScrollPosition, GUILayout.MaxHeight(300));

            var allTables = GetTablesByInterface();
            var dataTables = allTables.Where(t => GetTableType(t) == TableType.Csv).ToList();
            var assetTables = allTables.Where(t => GetTableType(t) == TableType.Asset).ToList();
            var mergeTables = allTables.Where(t => GetTableType(t) == TableType.Merge).ToList();

            DrawTableSection("Data Tables", dataTables, new Color(0.3f, 0.8f, 0.3f));

            DrawTableSection("Asset Tables", assetTables, new Color(0.9f, 0.6f, 0.2f));

            DrawTableSection("Merge Tables", mergeTables, new Color(0.8f, 0.3f, 0.8f));

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
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
                string path = EditorUtility.OpenFilePanel("Select CSV File", FilePath.CSV_PATH, "csv");
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

            if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
            {
                DrawCSVPreview();
            }

            EditorGUILayout.Space(20);

            GUI.enabled = !string.IsNullOrEmpty(csvFilePath) && 
                         !string.IsNullOrEmpty(tableName) && 
                         File.Exists(csvFilePath);

            if (GUILayout.Button("Update Csv Data", GUILayout.Height(40)))
            {
                UpdateCsvData();
            }
            if (GUILayout.Button("Generate Csv Table", GUILayout.Height(40)))
            {
                GenerateCsvTable();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(20);

            var dataTables = GetTablesByInterface().Where(t => GetTableType(t) == TableType.Csv).ToList();
            DrawTabSpecificTableList("Data Tables", dataTables, new Color(0.3f, 0.8f, 0.3f));
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
                string path = EditorUtility.OpenFolderPanel("Select Asset Folder", FilePath.ASSET_PATH, "");
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

            if (!string.IsNullOrEmpty(selectedFolderPath) && Directory.Exists(selectedFolderPath))
            {
                DrawAssetPreview();
            }

            EditorGUILayout.Space(20);

            GUI.enabled = !string.IsNullOrEmpty(selectedFolderPath) && 
                         !string.IsNullOrEmpty(assetTableName) && 
                         Directory.Exists(selectedFolderPath);

            if (GUILayout.Button("Generate Asset Table", GUILayout.Height(40)))
            {
                GenerateAssetTable();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(20);

            var assetTables = GetTablesByInterface().Where(t => GetTableType(t) == TableType.Asset).ToList();
            DrawTabSpecificTableList("Asset Tables", assetTables, new Color(0.9f, 0.6f, 0.2f));
        }

        private void DrawMergeTableTab()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 5)
            };

            EditorGUILayout.LabelField("Merge Table Generator", titleStyle);

            DrawSectionHeader("Table Settings");
            mergeTableName = EditorGUILayout.TextField("Merge Table Name", mergeTableName);
            
            EditorGUILayout.Space();
            DrawKeyTypeSelection();
            
            EditorGUILayout.Space();

            DrawSectionHeader("Referenced Tables");
            DrawReferencedTablesSelection();
            
            EditorGUILayout.Space();

            DrawSectionHeader("Options");
            refAutoRegister = EditorGUILayout.Toggle("Auto Register to TableCenter", refAutoRegister);
            
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUILayout.HelpBox("Advanced options for MergeTable generation", MessageType.Info);
            }

            EditorGUILayout.Space(20);

            GUI.enabled = !string.IsNullOrEmpty(mergeTableName) && 
                         selectedReferenceTables.Count > 0 &&
                         !string.IsNullOrEmpty(mergeTableKeyType);

            if (GUILayout.Button("Generate Merge Table", GUILayout.Height(40)))
            {
                GenerateMergeTable();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(20);

            var mergeTables = GetTablesByInterface().Where(t => GetTableType(t) == TableType.Merge).ToList();
            DrawTabSpecificTableList("Merge Tables", mergeTables, new Color(0.8f, 0.3f, 0.8f));
        }
        #endregion

        #region Interface-based CsvTable Detection Methods
        private List<ScriptableObject> GetTablesByInterface()
        {
            var tables = new List<ScriptableObject>();
            
            string searchPath = "Assets";
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { searchPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                
                if (asset != null && IsTableType(asset))
                {
                    tables.Add(asset);
                }
            }
            
            return tables.OrderBy(t => t.name).ToList();
        }

        private bool IsTableType(ScriptableObject obj)
        {
            return obj is ITableType;
        }

        private TableType GetTableType(ScriptableObject obj)
        {
            if (obj is ITableType tableType)
            {
                return tableType.tableType;
            }
            return TableType.Csv;
        }

        #endregion

        #region Asset List Drawing Methods

        private void DrawTabSpecificTableList(string title, List<ScriptableObject> tables, Color headerColor)
        {
            if (tables.Count == 0)
            {
                DrawInfoBox($"No {title} found in project", MessageType.Info);
                return;
            }

            string foldoutKey = title.Replace(" ", "");
            if (!assetFoldouts.ContainsKey(foldoutKey))
                assetFoldouts[foldoutKey] = true;

            assetFoldouts[foldoutKey] = EditorGUILayout.Foldout(assetFoldouts[foldoutKey], $"{title} ({tables.Count})", true);
            
            if (!assetFoldouts[foldoutKey]) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            assetListScrollPosition = EditorGUILayout.BeginScrollView(assetListScrollPosition, GUILayout.MaxHeight(250));

            foreach (var table in tables)
            {
                DrawAssetItem(table, headerColor);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTableSection(string sectionName, List<ScriptableObject> tables, Color sectionColor)
        {
            if (tables.Count == 0) return;

            if (!assetFoldouts.ContainsKey(sectionName))
                assetFoldouts[sectionName] = true;

            var headerStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            assetFoldouts[sectionName] = EditorGUILayout.Foldout(assetFoldouts[sectionName], 
                $"{sectionName} ({tables.Count})", true, headerStyle);

            if (!assetFoldouts[sectionName]) return;

            EditorGUI.indentLevel++;
            
            foreach (var table in tables)
            {
                DrawAssetItem(table, sectionColor);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void DrawAssetItem(ScriptableObject asset, Color accentColor)
        {
            if (asset == null) return;

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
            {
                string soPath = $"{FilePath.TABLE_OUTPUT_PATH}{asset.name}.asset";
                string tablePath = $"{FilePath.TABLE_CLASS_PATH}{asset.name}.cs";
                string dataPath = $"{FilePath.DATA_CLASS_PATH}{asset.name.Replace("TableSO", "")}.cs";
                
                if (asset is ITableType tableType)
                {
                    if (tableType.tableType == TableType.Asset)
                    {
                        var settings = AddressableAssetSettingsDefaultObject.Settings;
                        if (settings == null)
                        {
                            Debug.LogError("[TableSO] Cannot Find Addressable Setting");
                            return;
                        }

                        string groupName = $"{asset.name}";
                        var group = settings.FindGroup(groupName);
                        if (group == null)
                        { 
                            Debug.LogError($"[TableSO] Cannot Find Group {groupName}");
                            return;
                        }
                        settings.RemoveGroup(group);
                    }
                    else if (tableType.tableType == TableType.Merge)
                    {
                        dataPath = dataPath.Replace("Merge", "");
                    }
                }

                AssetDatabase.DeleteAsset(soPath);
                AssetDatabase.DeleteAsset(tablePath);
                AssetDatabase.DeleteAsset(dataPath);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            var icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(asset));
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
            }

            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = {textColor = accentColor}
            };

            if (GUILayout.Button(asset.name, nameStyle, GUILayout.ExpandWidth(true)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Key Type Selection

        private void DrawKeyTypeSelection()
        {
            DrawSectionHeader("Key Type Configuration");
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            var helpStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                padding = new RectOffset(10, 10, 8, 8)
            };
            
            EditorGUILayout.LabelField("Specify the key type for the MergeTable. You can use built-in types (string, int, etc.) or custom types (e.g., ItemType enum).", helpStyle);
            
            EditorGUILayout.Space(5);
            
            // 일반적인 키 타입 버튼들
            EditorGUILayout.LabelField("Common Types:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (string keyType in commonKeyTypes)
            {
                var buttonStyle = mergeTableKeyType == keyType ? 
                    new GUIStyle(GUI.skin.button) { normal = { background = MakeTex(1, 1, new Color(0.3f, 0.5f, 0.8f, 1f)) } } : 
                    GUI.skin.button;
                    
                if (GUILayout.Button(keyType, buttonStyle))
                {
                    mergeTableKeyType = keyType;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 사용자 정의 타입 입력
            EditorGUILayout.LabelField("Custom Type:", EditorStyles.boldLabel);
            mergeTableKeyType = EditorGUILayout.TextField("Key Type", mergeTableKeyType);
            
            // 키 타입 유효성 검사 및 안내
            if (!string.IsNullOrEmpty(mergeTableKeyType))
            {
                if (IsBuiltInType(mergeTableKeyType))
                {
                    DrawInfoBox($"Using built-in type: {mergeTableKeyType}", MessageType.Info);
                }
                else
                {
                    DrawInfoBox($"Using custom type: {mergeTableKeyType}\nMake sure this type exists in your project and implements IConvertible if needed.", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private bool IsBuiltInType(string typeName)
        {
            string[] builtInTypes = { "string", "int", "float", "double", "bool", "char", "byte", "short", "long", "uint", "ushort", "ulong" };
            return builtInTypes.Contains(typeName.ToLower());
        }

        #endregion

        #region Merge Tables Selection

        private void DrawReferencedTablesSelection()
        {
            var availableTables = GetTablesByInterface(); // 인터페이스 기반으로 변경
            
            if (availableTables.Count == 0)
            {
                DrawInfoBox("No tables found in the project. Please create some tables first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            
            EditorGUILayout.LabelField($"Available Tables ({availableTables.Count} found)", headerStyle);
            
            tablesScrollPosition = EditorGUILayout.BeginScrollView(tablesScrollPosition, GUILayout.MaxHeight(150));
            
            foreach (var table in availableTables)
            {
                if (table == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                Color color = Color.black;
                if (table is ITableType tt)
                {
                    if (tt.tableType == TableType.Csv) color = new Color(0.3f, 0.8f, 0.3f);
                    else if (tt.tableType == TableType.Asset) color = new Color(0.9f, 0.6f, 0.2f);
                    else if (tt.tableType == TableType.Merge) color = new Color(0.8f, 0.3f, 0.8f);
                }

                var fieldStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontSize = 12,
                    normal = {textColor = color}
                };
                
                bool isSelected = selectedReferenceTables.Contains(table);
                bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newSelection != isSelected)
                {
                    if (newSelection)
                    {
                        selectedReferenceTables.Add(table);
                    }
                    else
                    {
                        selectedReferenceTables.Remove(table);
                    }
                }
                
                // 테이블 정보 표시 (타입 포함)
                string tableInfo = table.name;
                if (table is ITableType tableType)
                {
                    tableInfo += $" ({tableType.tableType})";
                }
                
                EditorGUILayout.LabelField(tableInfo, fieldStyle);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                selectedReferenceTables.Clear();
                selectedReferenceTables.AddRange(availableTables);
            }
            
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selectedReferenceTables.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (selectedReferenceTables.Count > 0)
            {
                DrawInfoBox($"{selectedReferenceTables.Count} table(s) selected", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region UI Helper Methods

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
            DrawStatBox("Csv Tables", data, new Color(0.3f, 0.8f, 0.3f));
            DrawStatBox("Asset Tables", asset, new Color(0.9f, 0.6f, 0.2f));
            DrawStatBox("Merge Tables", reference, new Color(0.8f, 0.3f, 0.8f));
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

            EditorGUILayout.LabelField("Utility", titleStyle);
            EditorGUILayout.Space(5);

            autoReload = EditorGUILayout.Toggle("Auto Reloader", autoReload);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh All Tables", GUILayout.Height(25)))
            {
                Debug.Log("[TableSO] Refreshing all tables...");
                AssemblyReloadHandler.InitializeGeneratedTables();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Select TableCenter in Project", GUILayout.Height(25)))
            {
                Selection.activeObject = tableCenter;
                EditorGUIUtility.PingObject(tableCenter);
            }
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

        #endregion

        #region Generation Methods
        private void CreateTableCenter()
        {
            Debug.Log("[TableSO] Creating TableCenter...");
            
            string path = $"{FilePath.CENTER_PATH}TableCenter.asset";

            if (!Directory.Exists(FilePath.CENTER_PATH))
                Directory.CreateDirectory(FilePath.CENTER_PATH);

            if (AssetDatabase.LoadAssetAtPath(path, typeof(TableCenter)) != null)
                return;
            
            TableCenter tableCenter = ScriptableObject.CreateInstance<TableCenter>();
            
            AssetDatabase.CreateAsset(tableCenter, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = tableCenter;
            
            EditorUtility.DisplayDialog("Success",
                "TableCenter has been successfully created.",
                "OK");
        }

        private void GenerateCsvTable()
        {
            try
            {
                CsvTableGenerator.GenerateCsvTable(csvFilePath);
                Debug.Log($"[TableSO] Csv table '{tableName}' generation completed from {csvFilePath}");
                EditorUtility.DisplayDialog("Success", $"CSV table '{tableName}' generated successfully!", "OK");

            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating table from CSV: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate table:\n{e.Message}", "OK");
            }
        }

        private void UpdateCsvData()
        {
            try
            {
                CsvTableGenerator.UpdateCsvData(csvFilePath);
                Debug.Log($"[TableSO] Csv data '{tableName}' update completed from {csvFilePath}");
                EditorUtility.DisplayDialog("Success", $"CSV data '{tableName}' updated successfully!", "OK");

            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error updating data from CSV: {e.Message}");
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
                    assetCreateAddressable);
                
                Debug.Log($"[TableSO] Asset table '{assetTableName}' generation completed with {assets.Count} assets");
                EditorUtility.DisplayDialog("Success", $"Asset table '{assetTableName}' generated successfully with {assets.Count} assets!", "OK");
                
                // Clear form after successful generation
                selectedFolderPath = "Assets/";
                assetTableName = "";
                addressableGroupName = "";
                
                // Refresh the display
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating asset table: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate asset table:\n{e.Message}", "OK");
            }
        }

        private void GenerateMergeTable()
        {
            try
            {
                // Validate input
                if (!MergeTableGenerator.ValidateTableReferences(selectedReferenceTables))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid table references detected. Please check your selections.", "OK");
                    return;
                }

                // 키 타입 검증
                if (string.IsNullOrEmpty(mergeTableKeyType))
                {
                    EditorUtility.DisplayDialog("Error", "Key type must be specified.", "OK");
                    return;
                }

                // 키 타입을 포함하여 MergeTable 생성
                MergeTableGenerator.GenerateMergeTable(mergeTableName, selectedReferenceTables, mergeTableKeyType, refAutoRegister);
                
                Debug.Log($"[TableSO] MergeTable '{mergeTableName}' generated successfully with key type '{mergeTableKeyType}'");
                EditorUtility.DisplayDialog("Success", $"MergeTable '{mergeTableName}' generated successfully!", "OK");
                
                // Clear form after successful generation
                mergeTableName = "";
                mergeTableKeyType = "string";
                selectedReferenceTables.Clear();
                
                // Refresh the display
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error in MergeTable generation: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate MergeTable:\n{e.Message}", "OK");
            }
        }

        #endregion

        #region Utility Methods

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

        #endregion
    }
}