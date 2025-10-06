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
    public class DamageExpressionData : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        [field: SerializeField] public int[] Effects { get; internal set; }

        [field: SerializeField] public float[] Rates { get; internal set; }

        [field: SerializeField] public bool UseHitStop { get; internal set; }

        [field: SerializeField] public bool isDodgable { get; internal set; }

        [field: SerializeField] public float KnockbackCoef { get; internal set; }

        [field: SerializeField] public float DamageCoef { get; internal set; }

        [field: SerializeField] public float PhysicalCoef { get; internal set; }

        [field: SerializeField] public int BasePhysicalDamage { get; internal set; }

        [field: SerializeField] public int AddtionalPhysicalCritRawRate { get; internal set; }

        [field: SerializeField] public int AdditionalPhysicalCritDamage { get; internal set; }

        [field: SerializeField] public int AdditionalPhysicalPenetration { get; internal set; }

        [field: SerializeField] public StatType[] PhysicalStatTypes { get; internal set; }

        [field: SerializeField] public float[] PhysicalStatValues { get; internal set; }

        [field: SerializeField] public float MagicalCoef { get; internal set; }

        [field: SerializeField] public float BaseMagicalDamage { get; internal set; }

        [field: SerializeField] public int AddtionalMagicalCritRawRate { get; internal set; }

        [field: SerializeField] public int AdditionalMagicalCritDamage { get; internal set; }

        [field: SerializeField] public int AdditionalMagicalPenetration { get; internal set; }

        [field: SerializeField] public StatType[] MagicalStatTypes { get; internal set; }

        [field: SerializeField] public float[] MagicalStatValues { get; internal set; }

        public DamageExpressionData(int ID, int[] Effects, float[] Rates, bool UseHitStop, bool isDodgable, float KnockbackCoef, float DamageCoef, float PhysicalCoef, int BasePhysicalDamage, int AddtionalPhysicalCritRawRate, int AdditionalPhysicalCritDamage, int AdditionalPhysicalPenetration, StatType[] PhysicalStatTypes, float[] PhysicalStatValues, float MagicalCoef, float BaseMagicalDamage, int AddtionalMagicalCritRawRate, int AdditionalMagicalCritDamage, int AdditionalMagicalPenetration, StatType[] MagicalStatTypes, float[] MagicalStatValues)
        {
            this.ID = ID;
            this.Effects = Effects;
            this.Rates = Rates;
            this.UseHitStop = UseHitStop;
            this.isDodgable = isDodgable;
            this.KnockbackCoef = KnockbackCoef;
            this.DamageCoef = DamageCoef;
            this.PhysicalCoef = PhysicalCoef;
            this.BasePhysicalDamage = BasePhysicalDamage;
            this.AddtionalPhysicalCritRawRate = AddtionalPhysicalCritRawRate;
            this.AdditionalPhysicalCritDamage = AdditionalPhysicalCritDamage;
            this.AdditionalPhysicalPenetration = AdditionalPhysicalPenetration;
            this.PhysicalStatTypes = PhysicalStatTypes;
            this.PhysicalStatValues = PhysicalStatValues;
            this.MagicalCoef = MagicalCoef;
            this.BaseMagicalDamage = BaseMagicalDamage;
            this.AddtionalMagicalCritRawRate = AddtionalMagicalCritRawRate;
            this.AdditionalMagicalCritDamage = AdditionalMagicalCritDamage;
            this.AdditionalMagicalPenetration = AdditionalMagicalPenetration;
            this.MagicalStatTypes = MagicalStatTypes;
            this.MagicalStatValues = MagicalStatValues;
        }
    }
}
