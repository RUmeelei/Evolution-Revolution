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

            public Human(string id, string firstName, string lastName, int age) : base(id, $"{firstName} {lastName}", 100f, 5f, 50f)
            {
                FirstName = firstName;
                LastName = lastName;

                Age = age;

                Inventory = new List<string>();
            }

            public void AddItem(string item)
            {
                Inventory.Add(item);
            }

            public bool RemoveItem(string item)
            {
                return Inventory.Remove(item);
            }
        }
    }
}