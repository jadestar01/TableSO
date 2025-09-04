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
    public abstract class RefTableSO<TKey, TData> : TableSO<TKey, TData>, ITableType, IUpdatable
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        [Header("Reference Table Settings")]
        [SerializeField] protected List<ScriptableObject> referencedTables = new();
        [SerializeField] protected bool autoUpdateOnReferencedTableChange = true;

        protected bool isTableCacheInitialized = false;

        protected override void OnEnable()
        {
            tableType = TableType.Reference;
            CacheData();
        }


        /// <summary>
        /// Override this to define custom refresh logic based on referenced tables
        /// </summary>
        protected virtual void OnRefreshFromReferencedTables()
        {
            // Override in derived classes for custom refresh logic
        }
        
        #region IUpdatable Implementation
        public virtual void UpdateData()
        {
        }
        #endregion
    }
}