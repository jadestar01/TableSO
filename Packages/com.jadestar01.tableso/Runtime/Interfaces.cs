using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Csv,
        Asset,
        Merge
    }
    
    public interface IAssetData
    {
        public Type assetType { get; } 
        public string label { get; }   
    }
    
    public interface IUpdatable
    {
        public Task UpdateData();
    }

    public interface IMergable
    {
        public List<Type> refTableTypes { get; set; }
    }

    public interface ICsvPath
    {
        public string csvPath { get; }
    }
}