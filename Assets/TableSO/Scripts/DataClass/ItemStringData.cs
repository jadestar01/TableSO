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
    public class ItemStringData : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        [field: SerializeField] public string Name { get; internal set; }

        [field: SerializeField] public string Description { get; internal set; }

        public ItemStringData(int ID, string Name, string Description)
        {
            this.ID = ID;
            this.Name = Name;
            this.Description = Description;
        }
    }
}
