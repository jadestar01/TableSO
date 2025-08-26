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
        //button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (tableCenter == null)
        {
            Debug.LogError("TableCenter가 할당되지 않았습니다!");
            return;
        }
    
        var iconTable = tableCenter.GetTable<IconAssetTableSO>();
        if (iconTable == null)
        {
            Debug.LogError("IconAssetTableSO가 TableCenter에 등록되지 않았습니다!");
            return;
        }
    
        Debug.Log(iconTable.HasAsset("Coal"));
    
        var data = iconTable.GetAsset("Coal");
        if (data == null)
        {
            Debug.LogError("'Coal' 아이콘을 찾을 수 없습니다!");
            return;
        }

        if (image == null)
        {
            Debug.LogError("Image이 null임");
            return;
        }

        image.sprite = data;
    }

}
