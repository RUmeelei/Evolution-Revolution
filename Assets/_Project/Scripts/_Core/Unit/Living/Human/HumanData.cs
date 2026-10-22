using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        public enum PopulationGroup
        {
            Producers = 0,
            Military = 1,
            Merchants = 2,
            Ideologists = 3,
            Elite = 4,
        }

        public class Human : Living
        {
            public string FirstName {get; private set;}
            public string LastName {get; private set;}

            public int Age {get; private set;}

            public PopulationGroup Group {get; set;}

            public EquipmentLoadout Equipment {get; private set;}

            public List<string> Inventory {get; private set;}

            public Human(string id, string firstName, string lastName, int age, Vector2 position, string cultureId = "CUL_NONE", PopulationGroup group = PopulationGroup.Producers) : base(id, $"{firstName} {lastName}", 100f, Core.CoreManager.MainConfig.UnitBaseMaxSpeed, 50f, cultureId)
            {
                FirstName = firstName;
                LastName = lastName;

                Age = age;

                Group = group;

                Equipment = EquipmentLoadout.CreateUnarmed();

                Inventory = new List<string>();

                base.Move(position);
            }

            public void ApplyEquipmentLoadout(EquipmentLoadout loadout, float baseDamage, float baseRange, float baseCooldown, float traitDamageMultiplier = 1f)
            {
                Equipment = loadout;

                float finalDamage = baseDamage * loadout.GetWeaponDamageMultiplier() * traitDamageMultiplier;
                float finalRange = loadout.GetEffectiveRange(baseRange);
                float finalCooldown = loadout.GetEffectiveCooldown(baseCooldown);

                InitializeCombat(finalDamage, finalRange, finalCooldown);
                SetEquipmentStats(loadout.GetTotalArmor(), loadout.GetArmorPenetration(), loadout.GetSpeedMultiplier());
            }

            public void Rename(string firstName, string lastName)
            {
                if (!string.IsNullOrEmpty(firstName)) FirstName = firstName;
                if (!string.IsNullOrEmpty(lastName)) LastName = lastName;
            }

            public override int GetAge() => Age;
            public override void SetAge(int amount) => Age = Mathf.Max(0, amount);
            public override void ChangeAge(int amount) => Age = Mathf.Max(0, Age + amount);
    
            public override List<string> GetInventory() => Inventory;
            public override void AddItem(string item) => Inventory.Add(item);
            public override void RemoveItem(string item) => Inventory.Remove(item);
        }
    }
}