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

            protected Living(string id, string name, float maxHealth, float maxSpeed, float stamina, string cultureId) : base(id, name, maxHealth, maxSpeed, cultureId)
            {
                MaxStamina = stamina;
                Stamina = stamina;

                Hunger = 0;
                
                MaxMorale = 100;
                Morale = MaxMorale;
            }

            public override float GetStamina() => Stamina;
            public override float GetCurrentStaminaPercent() => MaxStamina > 0 ? Stamina / MaxStamina : 0f;
            public override void SetStamina(float amount) => Stamina = Mathf.Clamp(amount, 0, MaxStamina);
            public override void ChangeStamina(float amount) => Stamina = Mathf.Clamp(Stamina + amount, 0, MaxStamina);
    
            public override float GetMorale() => Morale;
            public override float GetCurrentMoralePercent() => MaxMorale > 0 ? Morale / MaxMorale : 0f;
            public override void SetMorale(float amount) => Morale = Mathf.Clamp(amount, 0, MaxMorale);
            public override void ChangeMorale(float amount) => Morale = Mathf.Clamp(Morale + amount, 0, MaxMorale);
    
            public override float GetHunger() => Hunger;
            public override void SetHunger(float amount) => Hunger = Mathf.Clamp(amount, 0, 100);
            public override void ChangeHunger(float amount) => Hunger = Mathf.Clamp(Hunger + amount, 0, 100);
        }
    }
}