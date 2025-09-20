using System;
using System.Collections.Generic;
using Table;
using TableSO.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
    [SerializeField] private TableCenter tableCenter;
    
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private List<Button> buttons = new();
    [SerializeField] private Image imageTemplate;
    [SerializeField] private List<Image> imageList = new();
    
    private void Start()
    {
        imageTemplate.gameObject.SetActive(false);

        for (int i = 0; i < buttons.Count; i++)
        {
            int id = i + 1;
            buttons[i].onClick.AddListener(() => OnButtonClick(id));
        }
    }

    public Image GetImage()
    {
        foreach (var image in imageList)
        {
            if (!image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(true);
                return image;
            }
        }
        
        var newImage = Instantiate(imageTemplate, imageTemplate.transform.parent).GetComponent<Image>();
        imageList.Add(newImage);
        newImage.gameObject.SetActive(true);
        return newImage;
    }

    public void OnButtonClick(int id)
    {
        foreach (var image in imageList) image.gameObject.SetActive(false);
        text.text = "";
        
        // Get Table from TableCenter
        var table = tableCenter.GetTable<ExampleGroupMergeTableSO>();
        // Get Data from Table
        var data = table.GetData(id);
        
        foreach (var icon in data.Icons)
        {
            var image = GetImage();
            image.sprite = icon;
        }

        text.text = data.Text;
    }
}

public enum ExampleEnum
{
    T,
    A,
    B,
    L,
    E,
    S,
    O
}