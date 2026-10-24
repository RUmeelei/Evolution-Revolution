using System;
using UnityEngine;

namespace ER.World.Inventory
{
    [Serializable]
    public class ItemTypeConfig
    {
        [SerializeField] private ushort itemTypeId;

        [SerializeField] private string localizationKey;

        [SerializeField] private float massPerUnitKg = 1.0f;

        [SerializeField] private float volumePerUnitLiters = 1.0f;

        [SerializeField] private uint maxStackSize = 100;

        [SerializeField] private ulong shelfLifeTicks = 0;

        [SerializeField] private float moistureAbsorbRate = 0.05f;

        [SerializeField] private byte baseFlammability = 10;


        public ushort ItemTypeId => itemTypeId;

        public string LocalizationKey => localizationKey;

        public float MassPerUnitKg => massPerUnitKg;

        public float VolumePerUnitLiters => volumePerUnitLiters;

        public uint MaxStackSize => maxStackSize;

        public ulong ShelfLifeTicks => shelfLifeTicks;

        public float MoistureAbsorbRate => moistureAbsorbRate;

        public byte BaseFlammability => baseFlammability;

        public bool IsPerishable => shelfLifeTicks > 0;
    }
}