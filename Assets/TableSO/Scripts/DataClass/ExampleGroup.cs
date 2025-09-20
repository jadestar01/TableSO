using System;
using System.Collections.Generic;
using UnityEngine;
using TableSO.Scripts;

/// <summary>
/// Merge Data Class - Made by TableSO MergeTableGenerator
/// Key Type: int
/// </summary>

namespace TableData
{
    [System.Serializable]
    public class ExampleGroup : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        public List<Sprite> Icons = new();
        public ExampleEnum exampleEnum;
        public string Text;

        public ExampleGroup(int ID, List<Sprite> Icons, ExampleEnum exampleEnum, string Text)
        {
            this.ID = ID;
            this.Icons = Icons;
            this.exampleEnum = exampleEnum;
            this.Text = Text;
        }
    }
}
