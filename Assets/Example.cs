using System;
using NUnit.Framework;
using Table;
using TableData;
using TableSO.Scripts;
using TMPro;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private TableCenter tableCenter;
    [SerializeField] private TextMeshProUGUI text;
    
    private void Update()
    {
        string str = "";
        str += $"{tableCenter.GetTable<EquippableItemDataTableSO>().GetType().Name} : {tableCenter.GetTable<EquippableItemDataTableSO>().GetAllKey().Count}\n";
        str += $"{tableCenter.GetTable<ItemStringDataTableSO>().GetType().Name} : {tableCenter.GetTable<ItemStringDataTableSO>().GetAllKey().Count}\n";
        str += $"{tableCenter.GetTable<ItemIconAssetTableSO>().GetType().Name} : {tableCenter.GetTable<ItemIconAssetTableSO>().GetAllKey().Count}\n";

        text.text = str;
    }
}

public enum ItemQuality
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum ItemType
{
    Melee,
    Magic,
    Consume,
    Material,
    Accessary
}

public enum MaterialType
{
    Normal,
    Special
}