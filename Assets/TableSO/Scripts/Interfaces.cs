using System;
using System.Collections.Generic;

namespace TableSO.Scripts
{
    public interface IIdentifiable<T> where T : IConvertible
    {
        public T ID { get; }
    }
    
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
    
    public interface IAssetData
    {
        public Type assetType { get; }
        public string fileName { get; }   
    }
    
    public interface IUpdatable
    {
        public void UpdateData();
    }

    public interface IConsultable
    {
        public List<Type> refTableTypes { get; set; }
    }
}