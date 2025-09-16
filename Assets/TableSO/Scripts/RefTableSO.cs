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
    public abstract class RefTableSO<TKey, TData> : TableSO<TKey, TData>
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
            UpdateData();
            CacheData();
        }
        
        
        #region IUpdatable Implementation
        public virtual void UpdateData()
        {
        }
        #endregion
    }
}