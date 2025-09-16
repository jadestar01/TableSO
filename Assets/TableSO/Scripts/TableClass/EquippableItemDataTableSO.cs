using UnityEngine;
using TableData;
using TableSO.Scripts;

namespace Table
{
    public class EquippableItemDataTableSO : TableSO.Scripts.TableSO<int, TableData.EquippableItemData>
    {
        public string fileName = "EquippableItemData";
    }
}
