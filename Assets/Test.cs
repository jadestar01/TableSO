using System;
using Table;
using TableSO.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public TableCenter tableCenter;

    public Image image;
    public Button button;

    private void Start()
    {
    }

    public void OnClick()
    {
        tableCenter.GetTable<ItemRefTableSO>().GetData(1001);
    }
}

public enum ItemQuality
{
    Melee,
    Magic,
    Accessary
}

public enum ItemType
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
