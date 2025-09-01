using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace TableSO.Scripts
{
    /// <summary>
    /// Reference Table that links to other tables and provides custom operations
    /// </summary>
    public abstract class RefTableSO<TKey, TData> : TableSO<TKey, TData>, ITableType
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        [Header("Reference Table Settings")]
        [SerializeField] protected List<ScriptableObject> referencedTables = new();
        [SerializeField] protected bool autoUpdateOnReferencedTableChange = true;

        protected Dictionary<Type, ScriptableObject> tableCache = new();
        protected bool isTableCacheInitialized = false;

        protected override void OnEnable()
        {
            tableType = TableType.Reference;
            InitializeTableCache();
            CacheData();
        }

        protected virtual void InitializeTableCache()
        {
            tableCache.Clear();
            
            foreach (var table in referencedTables)
            {
                if (table != null)
                {
                    tableCache[table.GetType()] = table;
                }
            }
            
            isTableCacheInitialized = true;
        }

        /// <summary>
        /// Add a table reference
        /// </summary>
        public virtual void AddTableReference(ScriptableObject table)
        {
            if (table == null) return;
            
            if (!referencedTables.Contains(table))
            {
                referencedTables.Add(table);
                
                if (isTableCacheInitialized)
                {
                    tableCache[table.GetType()] = table;
                }
                
                OnTableReferenceAdded(table);
                Debug.Log($"[RefTable] Added reference to {table.GetType().Name}");
            }
        }

        /// <summary>
        /// Remove a table reference
        /// </summary>
        public virtual void RemoveTableReference(ScriptableObject table)
        {
            if (referencedTables.Remove(table))
            {
                if (isTableCacheInitialized && table != null)
                {
                    tableCache.Remove(table.GetType());
                }
                
                OnTableReferenceRemoved(table);
                Debug.Log($"[RefTable] Removed reference to {table?.GetType().Name}");
            }
        }

        /// <summary>
        /// Get referenced table by type
        /// </summary>
        public virtual T GetReferencedTable<T>() where T : ScriptableObject
        {
            if (!isTableCacheInitialized)
            {
                InitializeTableCache();
            }

            if (tableCache.TryGetValue(typeof(T), out ScriptableObject table))
            {
                return table as T;
            }
            
            return null;
        }

        /// <summary>
        /// Get all referenced tables
        /// </summary>
        public virtual List<ScriptableObject> GetAllReferencedTables()
        {
            return new List<ScriptableObject>(referencedTables);
        }

        /// <summary>
        /// Check if a table is referenced
        /// </summary>
        public virtual bool HasTableReference<T>() where T : ScriptableObject
        {
            return GetReferencedTable<T>() != null;
        }

        /// <summary>
        /// Refresh data based on referenced tables
        /// </summary>
        public virtual void RefreshFromReferencedTables()
        {
            if (!isTableCacheInitialized)
            {
                InitializeTableCache();
            }

            OnRefreshFromReferencedTables();
            UpdateData();
        }

        /// <summary>
        /// Override this to define custom behavior when a table reference is added
        /// </summary>
        protected virtual void OnTableReferenceAdded(ScriptableObject table)
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Override this to define custom behavior when a table reference is removed
        /// </summary>
        protected virtual void OnTableReferenceRemoved(ScriptableObject table)
        {
            // Override in derived classes for custom behavior
        }

        /// <summary>
        /// Override this to define custom refresh logic based on referenced tables
        /// </summary>
        protected virtual void OnRefreshFromReferencedTables()
        {
            // Override in derived classes for custom refresh logic
        }

        public virtual void ValidateTableReferences()
        {
            List<ScriptableObject> invalidReferences = new();
            
            foreach (var table in referencedTables)
            {
                if (table == null)
                {
                    invalidReferences.Add(table);
                }
            }

            foreach (var invalidRef in invalidReferences)
            {
                referencedTables.Remove(invalidRef);
            }

            if (invalidReferences.Count > 0)
            {
                Debug.LogWarning($"[RefTable] Removed {invalidReferences.Count} invalid table references from {name}");
                InitializeTableCache();
            }
        }

        /// <summary>
        /// Get data from a referenced table by table type and key
        /// </summary>
        protected virtual T GetDataFromReferencedTable<T, TRefKey>(TRefKey key) 
            where T : ScriptableObject
        {
            var referencedTable = GetReferencedTable<T>();
            if (referencedTable == null) return null;

            // Use reflection to call GetData method
            var getDataMethod = referencedTable.GetType().GetMethod("GetData", new[] { typeof(TRefKey) });
            if (getDataMethod != null)
            {
                return getDataMethod.Invoke(referencedTable, new object[] { key }) as T;
            }

            return null;
        }

        /// <summary>
        /// Execute custom operation (override this for specific RefTable implementations)
        /// </summary>
        public virtual TResult ExecuteCustomOperation<TResult>(string operationName, params object[] parameters)
        {
            // This method should be overridden in generated RefTable classes
            Debug.LogWarning($"[RefTable] Custom operation '{operationName}' not implemented in {GetType().Name}");
            return default(TResult);
        }
    }
}