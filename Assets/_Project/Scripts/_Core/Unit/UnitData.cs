using UnityEngine;

namespace ER
{
    namespace Unit
    {
        public abstract class Unit
        {
            public string UnitId {get; protected set;}
            public string UnitName {get; protected set;}

            public Vector2 Position {get; set;}

            public float Health {get; set;}
            public float MaxHealth {get; protected set;}

            public float Speed {get; set;}
            public float MaxSpeed {get; protected set;}

            public bool IsAlive => Health > 0;

            public string CultureId {get; set;}

            protected Unit(string unitId, string unitName, float maxHealth, float maxSpeed)
            {
                UnitId = unitId;
                UnitName = unitName;

                MaxHealth = maxHealth;
                Health = maxHealth;

                MaxSpeed = maxSpeed;
                Speed = 0;
            }

            public virtual void ChangeHealth(float health)
            {
                Health = Mathf.Clamp(Health + health, 0, MaxHealth);
            }

            public virtual void Move(Vector2 target)
            {
                Position = target;
            }
        }
    }
}