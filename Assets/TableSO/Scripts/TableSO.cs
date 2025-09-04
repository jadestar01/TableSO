using System;
using UnityEngine;
using System.Collections.Generic;

namespace TableSO.Scripts
{
    public class TableSO<TKey, TData> : ScriptableObject, ITableType, IUpdatable
        where TData : class, IIdentifiable<TKey> where TKey : IConvertible
    {
        public TableType tableType { get; set; }

        public bool isUpdated = true;
        
        [SerializeField] 
        protected List<TData> dataList = new();   // Inspector에서 관리할 데이터
        
        protected Dictionary<TKey, TData> dataDict;

        private void Awake()
        {
            isUpdated = false;
            
            CacheData();
        }

        // TODO : 나중에 삭제해야 함.
        public List<TData> GetAllData()
        {
            return dataList;
        }

        public virtual TData GetData(TKey key)
        {
            if (isUpdated) CacheData();
            if (IsContains(key))
                return dataDict[key];
            else
                return null;
        }

        public bool IsContains(TKey ID)
        {
            return dataDict.ContainsKey(ID);
        }

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

            isUpdated = false;
        }
        
        protected virtual void OnDataUpdated()
        {
            
        }

        public virtual void UpdateData()
        {
            CacheData();
        }

        protected virtual void OnEnable()
        {
            tableType = TableType.Data;
            CacheData();
        }
    }
}