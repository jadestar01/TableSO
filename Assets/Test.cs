using System;
using Table;
using TableSO.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public TableCenter tableCenter;

    public Button button;

    private void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        var data = tableCenter.GetTable<ItemStringDataTableSO>().GetData(1101);
        Debug.Log(data.Name);
        Debug.Log(data.Description);
    }
}
