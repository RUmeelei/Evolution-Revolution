using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        public class Human : Living
        {
            public string FirstName {get; private set;}
            public string LastName {get; private set;}

            public int Age {get; private set;}

            public List<string> Inventory {get; private set;}

            public Human(string id, string firstName, string lastName, int age, Vector2 position, string cultureId = "CUL_NONE") : base(id, $"{firstName} {lastName}", 100f, 2f, 50f, cultureId)
            {
                FirstName = firstName;
                LastName = lastName;

                Age = age;

                Inventory = new List<string>();

                base.Move(position);
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