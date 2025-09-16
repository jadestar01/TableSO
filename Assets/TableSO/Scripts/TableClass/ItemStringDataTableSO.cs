using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class ItemStringDataTableSO : TableSO.Scripts.TableSO<int, TableData.ItemStringData>
    {
        public string fileName = "ItemStringData";
    }
}
