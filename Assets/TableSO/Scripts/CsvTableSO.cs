using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TableSO.Scripts.Generator;
using UnityEngine;

namespace TableSO.Scripts
{
    public class CsvTableSO<TKey, TData> : TableSO<TKey, TData>, ICsvPath
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        public virtual string csvPath { get => csvPath; }

        protected virtual void OnEnable()
        {
            tableType = TableType.Data;
            UpdateData();
            CacheData();
        }

        [ContextMenu("Test")]
        public override async void UpdateData()
        {
            dataList.Clear();
            dataList = new List<TData>(await CsvDataLoader.LoadCsvDataAsync<TData>(csvPath));
        } 
    }
}