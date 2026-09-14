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
            public Vector2 TargetPosition {get; set;}
            public bool HasTarget => TargetPosition != Position;

            public UnitVisual UnitVisual {get; set;}

            public float Health {get; set;}
            public float MaxHealth {get; protected set;}

            public float Speed {get; set;}
            public float MaxSpeed {get; protected set;}

            public string CultureId {get; set;}

            public bool IsAlive => Health > 0;

            protected Unit(string unitId, string unitName, float maxHealth, float maxSpeed, string cultureId)
            {
                UnitId = unitId;
                UnitName = unitName;

                MaxHealth = maxHealth;
                Health = maxHealth;

                MaxSpeed = maxSpeed;
                Speed = 0f;

                CultureId = cultureId;
            }

            public virtual void Update(float delta)
            {
            }

            public virtual void MoveTowardsTarget(float delta)
            {
                Vector2 direction = (TargetPosition - Position).normalized;

                float distance = Vector2.Distance(Position, TargetPosition);

                float moveDistance = Speed * delta;
    
                if (distance <= moveDistance)
                {
                    Position = TargetPosition;

                    Speed = 0f;
                }
                else
                {
                    Position += direction * moveDistance;
                }
            }

            public virtual void SetTarget(Vector2 target)
            {
                TargetPosition = target;

                Speed *= 0.2f;
            }

            public virtual void Stop()
            {
                TargetPosition = Position;
            }

            public virtual void Move(Vector2 target)
            {
                Position = target;

                TargetPosition = target;

                Speed = 0f;
            }

            public virtual void SetSpeed(float speed)
            {
                Speed = Mathf.Clamp(speed, 0f, MaxSpeed);
            }

            public virtual float GetCurrentSpeedPercent()
            {
                return MaxSpeed > 0 ? Speed / MaxSpeed : 0f;
            }

            public virtual void ChangeHealth(float health)
            {
                Health = Mathf.Clamp(Health + health, 0, MaxHealth);
            }

            public virtual float GetCurrentHealthPercent()
            {
                return MaxHealth > 0 ? Health / MaxHealth : 0f;
            }

            public virtual void TakeDamage(float damage)
            {
                ChangeHealth(-damage);
            }

            public virtual void Heal(float health)
            {
                ChangeHealth(health);
            }
        }
    }
}