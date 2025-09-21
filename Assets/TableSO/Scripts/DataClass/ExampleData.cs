using System;
using System.Collections.Generic;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Made by TableSO CsvTableGenerator
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class ExampleData : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        [field: SerializeField] public string[] IconName { get; internal set; }

        [field: SerializeField] public string Text { get; internal set; }

        public ExampleData(int ID, string[] IconName, string Text)
        {
            this.ID = ID;
            this.IconName = IconName;
            this.Text = Text;
        }
    }
}
