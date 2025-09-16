using System;
using System.Collections.Generic;
using System.IO;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;

namespace TableSO.Scripts
{
    [CreateAssetMenu(fileName = "TableCenter", menuName = "TableSO/TableCenter")]
    public class TableCenter : ScriptableObject
    {
        private const string GENERATED_TABLES_FOLDER = "Assets/TableSO/Table";
        private const string GENERATED_CODE_FOLDER = "Assets/TableSO/Scripts/TableClass";
        
        [Header("Registered Tables")]
        [SerializeField] private List<ScriptableObject> registeredTables = new();
        
        private Dictionary<Type, ScriptableObject> tableCache = new();
        private bool isCacheInitialized = false;
        
        public void ClearRegisteredTables() => registeredTables.Clear();

        public T GetTable<T>() where T : ScriptableObject
        {
            if (!isCacheInitialized)
            {
                InitializeCache();
            }

            Type tableType = typeof(T);
            
            if (tableCache.TryGetValue(tableType, out ScriptableObject table))
            {
                return table as T;
            }
            
            Debug.LogError($"[TableSO] Cannot find table type {tableType.Name}. Please check if it's registered in TableCenter");
            return null;
        }
        

        public void RegisterTable(ScriptableObject table)
        {
            if (table == null)
            {
                Debug.LogWarning("[TableSO] Cannot register null table");
                return;
            }

            if (!registeredTables.Contains(table))
            {
                registeredTables.Add(table);

                if (isCacheInitialized)
                {
                    tableCache[table.GetType()] = table;
                }
            }
        }

        public void UnregisterTable(ScriptableObject table)
        {
            if (registeredTables.Remove(table))
            {
                if (isCacheInitialized && table != null)
                {
                    tableCache.Remove(table.GetType());
                }
                
            }
        }

        private void InitializeCache()
        {
            tableCache.Clear();
            
            foreach (var table in registeredTables)
            {
                if (table != null)
                {
                    tableCache[table.GetType()] = table;
                }
            }
            
            isCacheInitialized = true;
        }

        private void OnValidate()
        {
            // Invalidate cache when changes occur in Inspector
            isCacheInitialized = false;
        }

        // Method to check registered table list in editor
        public List<ScriptableObject> GetRegisteredTables()
        {
            return new List<ScriptableObject>(registeredTables);
        }

        [ContextMenu("Clear All Tables")]
        private void ClearAllTables()
        {
            registeredTables.Clear();
            tableCache.Clear();
            isCacheInitialized = false;
            Debug.Log("[TableSO] All tables removed from TableCenter");
        }

        [ContextMenu("Refresh Cache")]
        public void RefreshCache()
        {
            InitializeCache();
            Debug.Log("[TableSO] TableCenter cache refreshed");
        }

        public int GetAssetTableCount()
        {
            int i = 0;
            foreach (var table in registeredTables)
                if (table != null && ((ITableType)table).tableType == TableType.Asset)
                    i++;
            
            return i;
        }

        public int GetCsvTableCount()
        {
            int i = 0;
            foreach (var table in registeredTables)
                if (table != null && ((ITableType)table).tableType == TableType.Data)
                    i++;

            return i;
        }

        public int GetRefTableCount()
        {
            int i = 0;
            foreach (var table in registeredTables)
                if (table != null && ((ITableType)table).tableType == TableType.Merge)
                    i++;

            return i;
        }

        public int GetTableCount()
        {
            int i = 0;
            
            foreach (var table in registeredTables)
            {
                if (table != null)
                    i++;
            }

            return i;
        }
    }

    // Editor extension for automatic registration to TableCenter
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TableCenter))]
    public class TableCenterEditor : UnityEditor.Editor
    {
        private const string GENERATED_TABLES_FOLDER = "Assets/TableSO/Table";
        private const string GENERATED_CODE_FOLDER = "Assets/TableSO/Scripts/TableClass";
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TableCenter tableCenter = (TableCenter)target;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Auto Register All Tables"))
            {
                AutoRegisterAllTables(tableCenter);
            }
            
            if (GUILayout.Button("Refresh Cache"))
            {
                tableCenter.RefreshCache();
            }
        }

        private void AutoRegisterAllTables(TableCenter tableCenter)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { FilePath.TABLE_OUTPUT_PATH });
            int registeredCount = 0;

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                
                if (so != null && so.GetType().Name.EndsWith("TableSO"))
                {
                    tableCenter.RegisterTable(so);
                    registeredCount++;
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(tableCenter);
            Debug.Log($"[TableSO] {registeredCount} tables automatically registered");
        }
        
        public void CleanGeneratedAssets()
        {
            if (EditorUtility.DisplayDialog("Confirm",
                    "생성된 모든 TableSO 에셋을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                    "삭제", "취소"))
            {
                CleanupGeneratedAssets();
            }
        }

        private void CleanupGeneratedAssets()
        {
            try
            {
                if (Directory.Exists(GENERATED_TABLES_FOLDER))
                {
                    var assetFiles = Directory.GetFiles(GENERATED_TABLES_FOLDER, "*.asset", SearchOption.AllDirectories);

                    foreach (var file in assetFiles)
                    {
                        string relativePath = file.Replace('\\', '/');
                        if (relativePath.StartsWith(Application.dataPath))
                        {
                            relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                        }

                        AssetDatabase.DeleteAsset(relativePath);
                    }

                    Debug.Log($"[TableSO] {assetFiles.Length}개의 생성된 에셋을 정리했습니다.");
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] 에셋 정리 중 오류: {e.Message}");
            }
        }

    }
#endif
}

namespace Table
{
}

namespace TableData
{
}