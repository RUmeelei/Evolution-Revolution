using UnityEngine;

namespace ER
{
    namespace Unit
    {
        public abstract class Living : Unit
        {
            public float Stamina {get; set;}
            public float MaxStamina {get; protected set;}

            public float Hunger {get; set;}
            
            public float Morale {get; set;}
            public float MaxMorale {get; protected set;}

            protected Living(string id, string name, float maxHealth, float maxSpeed, float stamina) : base(id, name, maxHealth, maxSpeed)
            {
                MaxStamina = stamina;
                Stamina = stamina;

                Hunger = 0;
                
                MaxMorale = 100;
                Morale = MaxMorale;
            }

            public virtual void ChangeHunger(float food)
            {
                Hunger = Mathf.Clamp(Hunger + food, 0, 100);

                if (Hunger > 0)
                {
                    base.ChangeHealth(food);

                    ChangeMorale(food);
                }
                else
                {
                    base.ChangeHealth(-food);

                    ChangeMorale(-food);
                }
            }

            public virtual void ChangeMorale(float morale)
            {
                Morale = Mathf.Clamp(Morale + morale, 0, MaxMorale);
            }
        }
    }
}