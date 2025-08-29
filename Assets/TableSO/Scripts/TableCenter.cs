using System;
using System.Collections.Generic;
using TableSO.FileUtility;
using UnityEngine;

namespace TableSO.Scripts
{
    [CreateAssetMenu(fileName = "TableCenter", menuName = "TableSO/TableCenter")]
    public class TableCenter : ScriptableObject
    {
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
                
                Debug.Log($"[TableSO] Table {table.GetType().Name} registered to TableCenter");
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
                
                Debug.Log($"[TableSO] Table {table?.GetType().Name} removed from TableCenter");
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
            {
                if (table != null &&
                    table.GetType().IsGenericType &&
                    table.GetType().GetGenericTypeDefinition() == typeof(AssetTableSO<>))
                    i++;
            }

            return i++;
        }

        public int GetCsvTableCount()
        {
            int i = 0;
            foreach (var table in registeredTables)
            {
                if (table != null &&
                    table.GetType().IsGenericType &&
                    table.GetType().GetGenericTypeDefinition() == typeof(TableSO<,>) &&
                    table.GetType().GetGenericTypeDefinition() != typeof(AssetTableSO<>))
                    i++;
            }

            return i++;
        }

        public int GetRefTableCount()
        {
            return 0;
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
    }
#endif
}

namespace Table
{
}

namespace TableData
{
}