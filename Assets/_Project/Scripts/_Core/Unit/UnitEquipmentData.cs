using System;
using UnityEngine;

namespace ER
{
    namespace Unit
    {
        public enum ArmorTier
        {
            None = 0,
            Leather = 1,
            Copper = 2,
            Steel = 3,
        }

        public enum WeaponType
        {
            None = 0,
            Spear = 1,
            Sword = 2,
            Longsword = 3,
            Halberd = 4,
            Bow = 5,
            Crossbow = 6,
        }

        [Serializable]
        public struct EquipmentLoadout
        {
            public string TemplateName;

            public WeaponType Weapon;

            public ArmorTier Helmet;
            public ArmorTier Chestplate;
            public ArmorTier Greaves;
            public ArmorTier Boots;

            public static EquipmentLoadout CreateUnarmed()
            {
                return new EquipmentLoadout
                {
                    TemplateName = "Civilian",
                    Weapon = WeaponType.None,
                    Helmet = ArmorTier.None,
                    Chestplate = ArmorTier.None,
                    Greaves = ArmorTier.None,
                    Boots = ArmorTier.None
                };
            }

            public static EquipmentLoadout CreatePreset(string name, WeaponType weapon, ArmorTier armorTier)
            {
                return new EquipmentLoadout
                {
                    TemplateName = name,
                    Weapon = weapon,
                    Helmet = armorTier,
                    Chestplate = armorTier,
                    Greaves = armorTier,
                    Boots = armorTier
                };
            }
            
            public float GetTotalArmor()
            {
                return GetSlotArmor(Helmet, 1.0f) +
                       GetSlotArmor(Chestplate, 2.2f) +
                       GetSlotArmor(Greaves, 1.4f) +
                       GetSlotArmor(Boots, 0.8f);
            }

            private static float GetSlotArmor(ArmorTier tier, float slotWeight)
            {
                float tierValue = tier switch
                {
                    ArmorTier.Leather => 2.0f,
                    ArmorTier.Copper => 4.5f,
                    ArmorTier.Steel => 8.0f,
                    _ => 0f
                };

                return tierValue * slotWeight;
            }
            
            public float GetSpeedMultiplier()
            {
                float totalWeight = GetSlotWeight(Helmet) + GetSlotWeight(Chestplate) * 2f + GetSlotWeight(Greaves) * 1.5f + GetSlotWeight(Boots) + GetWeaponWeight(Weapon);

                return Mathf.Clamp(1.0f - (totalWeight * 0.025f), 0.55f, 1.0f);
            }

            private static float GetSlotWeight(ArmorTier tier)
            {
                return tier switch
                {
                    ArmorTier.Leather => 0.5f,
                    ArmorTier.Copper => 1.5f,
                    ArmorTier.Steel => 2.5f,
                    _ => 0f
                };
            }

            private static float GetWeaponWeight(WeaponType weapon)
            {
                return weapon switch
                {
                    WeaponType.Spear => 1.0f,
                    WeaponType.Sword => 1.0f,
                    WeaponType.Longsword => 2.0f,
                    WeaponType.Halberd => 2.5f,
                    WeaponType.Bow => 0.8f,
                    WeaponType.Crossbow => 2.0f,
                    _ => 0f
                };
            }

            public float GetWeaponDamageMultiplier()
            {
                return Weapon switch
                {
                    WeaponType.Spear => 1.15f,
                    WeaponType.Sword => 1.35f,
                    WeaponType.Longsword => 1.75f,
                    WeaponType.Halberd => 1.90f,
                    WeaponType.Bow => 1.10f,
                    WeaponType.Crossbow => 1.85f,
                    _ => 0.5f
                };
            }
            
            public float GetArmorPenetration()
            {
                return Weapon switch
                {
                    WeaponType.Spear => 4f,
                    WeaponType.Sword => 5f,
                    WeaponType.Longsword => 10f,
                    WeaponType.Halberd => 18f,
                    WeaponType.Bow => 3f,
                    WeaponType.Crossbow => 22f,
                    _ => 0f
                };
            }
            
            public float GetEffectiveRange(float baseMeleeRange)
            {
                float melee = Mathf.Max(0.5f, baseMeleeRange);

                return Weapon switch
                {
                    WeaponType.Spear => melee * 1.8f,
                    WeaponType.Halberd => melee * 2.0f,
                    WeaponType.Longsword => melee * 1.25f,
                    WeaponType.Bow => 4.2f,
                    WeaponType.Crossbow => 4.8f,
                    _ => melee
                };
            }

            public float GetEffectiveCooldown(float baseCooldown)
            {
                float cd = Mathf.Max(0.2f, baseCooldown);

                return Weapon switch
                {
                    WeaponType.Sword => cd * 0.85f,
                    WeaponType.Longsword => cd * 1.15f,
                    WeaponType.Halberd => cd * 1.30f,
                    WeaponType.Bow => cd * 1.20f,
                    WeaponType.Crossbow => cd * 2.20f,
                    _ => cd
                };
            }
        }
    }
}