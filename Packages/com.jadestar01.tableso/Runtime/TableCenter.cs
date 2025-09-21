using System;
using System.Collections.Generic;
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

        private async void OnEnable()
        {
            List<ScriptableObject> mergeTables = new();
            foreach (var table in registeredTables)
                if (table is ITableType type)
                {
                    if (type.tableType == TableType.Merge)
                        mergeTables.Add(table);
                    else
                    {
                        if (table is IUpdatable updatable)
                            await updatable.UpdateData();
                    }
                }
            
            Debug.Log($"[TableSO] {mergeTables.Count} merge tables found");
            
            foreach (var table in mergeTables)
                if (table is IUpdatable updatable)
                    await updatable.UpdateData();
        }
        
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
        
        #region Register
        public void ClearRegisteredTables() => registeredTables.Clear();
        
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
        #endregion
        
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
            isCacheInitialized = false;
        }

        public List<ScriptableObject> GetRegisteredTables()
        {
            return new List<ScriptableObject>(registeredTables);
        }
    }
}

namespace Table
{
}

namespace TableData
{
}