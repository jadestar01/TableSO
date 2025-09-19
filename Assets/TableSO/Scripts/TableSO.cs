using System;
using UnityEngine;
using System.Collections.Generic;

namespace TableSO.Scripts
{
    public class TableSO<TKey, TData> : ScriptableObject, ITableType, IUpdatable
        where TData : class, IIdentifiable<TKey> where TKey : IConvertible
    {
        public string fileName { get; }
        
        public TableType tableType { get; set; }
        
        [SerializeField] 
        public List<TData> dataList;
        protected Dictionary<TKey, TData> dataDict;
        
        #region Unity Event
        protected virtual void OnEnable()
        {
            tableType = TableType.Csv;
            UpdateData();
            CacheData();
            Debug.Log($"[{GetType().Name}] ({typeof(TKey).Name}, {typeof(TData).Name}) : {dataList.Count}");
        }
        #endregion
        
        #region Utils
        public List<TKey> GetAllKey()
        {
            List<TKey> keys = new List<TKey>();
            foreach (var kvp in dataDict)
                keys.Add(kvp.Key);
            return keys;
        }

        public virtual TData GetData(TKey key)
        {
            if (Contains(key))
                return dataDict[key];
            else
                return null;
        }

        public bool Contains(TKey ID)
        {
            return dataDict.ContainsKey(ID);
        }
        #endregion

        #region Data
        public virtual void CacheData()
        {
            dataDict = new Dictionary<TKey, TData>();

            for (int i = 0; i < dataList.Count; i++)
            {
                var item = dataList[i];
                if (!dataDict.ContainsKey(item.ID))
                    dataDict.Add(item.ID, item); // Key = ID, Value = 객체
                else
                    Debug.LogWarning($"[TableSO] Duplicate key detected: {item.ID}");
            }
        }

        public virtual void UpdateData()
        {
        }
        #endregion
    }
}