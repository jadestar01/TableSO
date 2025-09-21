using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace TableSO.Scripts
{
    public class TableSO<TKey, TData> : ScriptableObject, ITableType, IUpdatable
        where TData : class, IIdentifiable<TKey> where TKey : IConvertible
    {
        public string fileName { get; }
        
        public virtual TableType tableType { get; set; }
        
        [SerializeField] 
        public List<TData> dataList;
        protected Dictionary<TKey, TData> dataDict;
        
        #region Utils
        public List<TKey> GetAllKey()
        {
            return dataDict == null ? new List<TKey>() : dataDict.Keys.ToList();
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

        public virtual Task UpdateData()
        {
            Debug.Log($"[{GetType().Name}] ({typeof(string).Name}, {typeof(TData).Name}) : {dataDict?.Count}");
            return Task.CompletedTask;
        }

        public void ReleaseData()
        {
            dataList?.Clear();
            dataDict?.Clear();
        }
        #endregion
    }
}