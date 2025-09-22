using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TableSO.Scripts
{
    public abstract class CustomTableSO<TKey, TData> : TableSO<TKey, TData>, ICustomizable
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        public override TableType tableType => TableType.Custom;
        
        [Header("Custom Table Settings")]
        [SerializeField] protected List<ScriptableObject> referencedTables = new();
        public virtual List<Type> refTableTypes { get; set; }
        
        #region IUpdatable Implementation
        public override async Task UpdateData()
        {
            CacheData();
            base.UpdateData();
        }
        #endregion
    }
}