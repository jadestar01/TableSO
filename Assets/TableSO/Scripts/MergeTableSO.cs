using System;
using System.Collections.Generic;
using UnityEngine;

namespace TableSO.Scripts
{
    /// <summary>
    /// Merge Table that links to other tables and provides custom operations
    /// </summary>
    public abstract class MergeTableSO<TKey, TData> : TableSO<TKey, TData>, IMergable
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        [Header("Merge Table Settings")]
        [SerializeField] protected List<ScriptableObject> referencedTables = new();
        public List<Type> refTableTypes { get; set; }
    
        protected override void OnEnable()
        {
            tableType = TableType.Merge;
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