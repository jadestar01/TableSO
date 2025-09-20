using System;
using System.Collections.Generic;
using UnityEngine;

namespace TableSO.Scripts
{
    public abstract class MergeTableSO<TKey, TData> : TableSO<TKey, TData>, IMergable
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        public override TableType tableType => TableType.Merge;
        
        [Header("Merge Table Settings")]
        [SerializeField] protected List<ScriptableObject> referencedTables = new();
        public virtual List<Type> refTableTypes { get; set; }
        
        #region IUpdatable Implementation
        public override void UpdateData()
        {
            CacheData();
            base.UpdateData();
        }
        #endregion
    }
}