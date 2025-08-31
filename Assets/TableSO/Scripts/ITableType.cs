using UnityEngine;

namespace TableSO.Scripts
{
    public interface ITableType
    {
        public TableType tableType { get; set; }
    }
    
    public enum TableType
    {
        Data,
        Asset,
        Reference
    }
}
