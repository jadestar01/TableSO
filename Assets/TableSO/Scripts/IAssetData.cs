using System;
using UnityEngine;

namespace TableSO.Scripts
{
    public interface IAssetData
    {
        public Type assetType { get; }
        public string fileName { get; }   
    }
}