using System;
using UnityEngine;
using System.Collections.Generic;

namespace TableSO.Scripts
{
    public class CsvTableSO<TKey, TData> : TableSO<TKey, TData>
        where TData : class, IIdentifiable<TKey> 
        where TKey : IConvertible
    {
    }
}