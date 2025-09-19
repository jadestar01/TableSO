using System;
using System.Collections.Generic;
using TableSO.Scripts.Generator;
using UnityEngine;

namespace TableSO.Scripts
{
    public class CsvTableSO<TKey, TData> : TableSO<TKey, TData>, ICsvPath
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
        public virtual string csvPath { get => csvPath; }

        protected virtual void OnEnable() => tableType = TableType.Csv;

        public override async void UpdateData()
        {
            dataList?.Clear();
            dataList = new List<TData>(await CsvDataLoader.LoadCsvDataAsync<TData>(csvPath));
        } 
    }
}