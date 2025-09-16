using System;
using NUnit.Framework;
using Table;
using TableData;
using TableSO.Scripts;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private TableCenter tableCenter;

    private void Start()
    {
        
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