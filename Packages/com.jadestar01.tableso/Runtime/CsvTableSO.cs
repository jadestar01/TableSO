using System;
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
        public override TableType tableType => TableType.Csv;
        public virtual string csvPath { get => csvPath; }
        
        public override async Task UpdateData()
        {
            ReleaseData();
            dataList = new List<TData>(await CsvDataLoader.LoadCsvDataAsync<TData>(csvPath));
            CacheData();
            base.UpdateData();
        } 
    }
}