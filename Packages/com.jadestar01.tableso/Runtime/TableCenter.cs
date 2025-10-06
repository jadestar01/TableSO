using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TableSO.Scripts
{
    [CreateAssetMenu(fileName = "TableCenter", menuName = "TableSO/TableCenter")]
    public class TableCenter : ScriptableObject
    {
        [Header("Registered Tables")]
        [SerializeField] private List<ScriptableObject> registeredTables = new();
        
        private Dictionary<Type, ScriptableObject> tableCache = new();
        private bool isCacheInitialized = false;

        public async void Initalize()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            List<ScriptableObject> customTables = new();
            List<Task> nonCustomUpdateTasks = new();
    
            foreach (var table in registeredTables)
            {
                if (table is IUpdatable updatable1)
                    updatable1.isUpdated = false;
            
                if (table is ITableType type)
                {
                    if (type.tableType == TableType.Custom)
                        customTables.Add(table);
                    else if (table is IUpdatable updatable2)
                        nonCustomUpdateTasks.Add(updatable2.UpdateData());
                }
            }
    
            await Task.WhenAll(nonCustomUpdateTasks);
            Debug.Log($"[TableSO] {customTables.Count} custom tables found");

            foreach (var table in customTables)
            {
                if (table is ICustomizable custom)
                {
                    List<Task> refUpdateTasks = new();
                    foreach (var refTable in custom.refTableTypes)
                    {
                        var target = registeredTables.FirstOrDefault(t => t != null && t.GetType() == refTable);
                        if (target is IUpdatable u && !u.isUpdated)
                            refUpdateTasks.Add(u.UpdateData());
                    }
                    await Task.WhenAll(refUpdateTasks);
                }

                if (table is IUpdatable updatable)
                    await updatable.UpdateData();
            }
            
            sw.Stop();
            Debug.Log($"[TableSO] Elapsed Time : {sw.ElapsedMilliseconds} ms");
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