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
    public class EquippableItemData : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        [field: SerializeField] public string ItemIconPath { get; internal set; }

        [field: SerializeField] public string ItemName { get; internal set; }

        [field: SerializeField] public string ItemDescription { get; internal set; }

        [field: SerializeField] public ItemType ItemType { get; internal set; }

        [field: SerializeField] public ItemQuality ItemQuality { get; internal set; }

        [field: SerializeField] public int DropRate { get; internal set; }

        [field: SerializeField] public string Price { get; internal set; }

        [field: SerializeField] public int UpgradeCount { get; internal set; }

        [field: SerializeField] public float Health { get; internal set; }

        [field: SerializeField] public float Spirit { get; internal set; }

        [field: SerializeField] public float Swift { get; internal set; }

        [field: SerializeField] public float Haste { get; internal set; }

        [field: SerializeField] public string PhysicalDamage { get; internal set; }

        [field: SerializeField] public float PhysicalCritRate { get; internal set; }

        [field: SerializeField] public float PhysicalCritDamage { get; internal set; }

        [field: SerializeField] public float PhysicalAttackSpeed { get; internal set; }

        [field: SerializeField] public float PhysicalPenetration { get; internal set; }

        [field: SerializeField] public string MagicalDamage { get; internal set; }

        [field: SerializeField] public float MagicalCritRate { get; internal set; }

        [field: SerializeField] public float MagicalCritDamage { get; internal set; }

        [field: SerializeField] public float MagicalAttackSpeed { get; internal set; }

        [field: SerializeField] public float MagicalPenetration { get; internal set; }

        [field: SerializeField] public float Armor { get; internal set; }

        [field: SerializeField] public float Resistance { get; internal set; }

        [field: SerializeField] public float Dodge { get; internal set; }

        public EquippableItemData(int ID, string ItemIconPath, string ItemName, string ItemDescription, ItemType ItemType, ItemQuality ItemQuality, int DropRate, string Price, int UpgradeCount, float Health, float Spirit, float Swift, float Haste, string PhysicalDamage, float PhysicalCritRate, float PhysicalCritDamage, float PhysicalAttackSpeed, float PhysicalPenetration, string MagicalDamage, float MagicalCritRate, float MagicalCritDamage, float MagicalAttackSpeed, float MagicalPenetration, float Armor, float Resistance, float Dodge)
        {
            this.ID = ID;
            this.ItemIconPath = ItemIconPath;
            this.ItemName = ItemName;
            this.ItemDescription = ItemDescription;
            this.ItemType = ItemType;
            this.ItemQuality = ItemQuality;
            this.DropRate = DropRate;
            this.Price = Price;
            this.UpgradeCount = UpgradeCount;
            this.Health = Health;
            this.Spirit = Spirit;
            this.Swift = Swift;
            this.Haste = Haste;
            this.PhysicalDamage = PhysicalDamage;
            this.PhysicalCritRate = PhysicalCritRate;
            this.PhysicalCritDamage = PhysicalCritDamage;
            this.PhysicalAttackSpeed = PhysicalAttackSpeed;
            this.PhysicalPenetration = PhysicalPenetration;
            this.MagicalDamage = MagicalDamage;
            this.MagicalCritRate = MagicalCritRate;
            this.MagicalCritDamage = MagicalCritDamage;
            this.MagicalAttackSpeed = MagicalAttackSpeed;
            this.MagicalPenetration = MagicalPenetration;
            this.Armor = Armor;
            this.Resistance = Resistance;
            this.Dodge = Dodge;
        }
    }
}
