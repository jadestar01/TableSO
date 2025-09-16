using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class MaterialItemDataTableSO : TableSO.Scripts.TableSO<int, TableData.MaterialItemData>
    {
        public string fileName = "MaterialItemData";
    }
}
