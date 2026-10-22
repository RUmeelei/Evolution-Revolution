using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        public abstract class Unit
        {
            public event System.Action<Vector2> OnPositionChanged;
            public event System.Action<float> OnHealthChanged;
            public event System.Action OnDied;

            public string UnitId {get; protected set;}
            public string UnitName {get; protected set;}

            private Vector2 _position;
            public Vector2 Position
            {
                get => _position;

                set
                {
                    if (_position != value)
                    {
                        _position = value;

                        OnPositionChanged?.Invoke(_position);
                    }
                }
            }

            private Vector2 _targetPosition;
            public Vector2 TargetPosition
            {
                get => _targetPosition;

                set => _targetPosition = value;
            }
            public bool HasTarget => TargetPosition != Position;
            private List<Vector2> waypoints = new List<Vector2>();
            private Vector2 lastPathTarget = new Vector2(-999f, -999f);

            public UnitVisual UnitVisual {get; set;}

            public bool IsAlive => Health > 0;
            private float _health;
            public float Health
            {
                get => _health;

                set
                {
                    float oldHealth = _health;
                    _health = Mathf.Clamp(value, 0, MaxHealth);

                    if (oldHealth != _health)
                    {
                        OnHealthChanged?.Invoke(_health);

                        if (_health <= 0 && oldHealth > 0) OnDied?.Invoke();
                    }
                }
            }
            public float MaxHealth {get; protected set;}

            public float Speed {get; set;}
            public float MaxSpeed {get; protected set;}

            public string CultureId {get; set;}

            internal int GridCellIndex = -1;

            public bool IsSelected {get; set;}

            public float DecisionTimer = 0f;

            public bool IsCapturing = false;
            
            public enum PlayerOrderType
            {
                None,
                MoveTo,
                AttackUnit,
            }
            
            public PlayerOrderType OrderType {get; set;} = PlayerOrderType.None;
            public Vector2 OrderTargetPosition {get; set;}
            public Unit OrderTargetUnit {get; set;}
            public bool HasPlayerOrder => OrderType != PlayerOrderType.None;

            public bool IsAutonomous {get; set;} = true;

            public float Armor {get; protected set;}
            public float ArmorPenetration {get; protected set;}
            public float EquipmentSpeedMultiplier {get; protected set;} = 1f;
            
            public void SetMoveOrder(Vector2 target)
            {
                OrderType = PlayerOrderType.MoveTo;

                OrderTargetPosition = target;

                OrderTargetUnit = null;

                SetTarget(target);
            }
            
            public void SetAttackOrder(Unit target)
            {
                OrderType = PlayerOrderType.AttackUnit;

                OrderTargetUnit = target;

                OrderTargetPosition = target.Position;

                AttackTarget = target;

                SetTarget(target.Position);
            }
            
            public void ClearPlayerOrder()
            {
                OrderType = PlayerOrderType.None;

                OrderTargetPosition = Vector2.zero;
                
                OrderTargetUnit = null;
            }

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
                Vector2 activeWaypoint = waypoints.Count > 0 ? waypoints[0] : TargetPosition;
            
                Vector2 direction = (activeWaypoint - Position).normalized;
                float distance = Vector2.Distance(Position, activeWaypoint);
                float moveDistance = Speed * delta;
            
                if (distance <= moveDistance)
                {
                    Position = activeWaypoint;
            
                    if (waypoints.Count > 0)
                    {
                        waypoints.RemoveAt(0);
                    }
            
                    if (waypoints.Count == 0)
                    {
                        Position = TargetPosition;
                        Speed = 0f;
                    }
                }
                else
                {
                    Position += direction * moveDistance;
                }
            }

            public void ApplySeparation(Vector2 pushForce, float maxWorldX = float.MaxValue, float maxWorldY = float.MaxValue)
            {
                Vector2 nextPos = Position + pushForce;

                nextPos.x = Mathf.Clamp(nextPos.x, 0.25f, maxWorldX - 0.25f);
                nextPos.y = Mathf.Clamp(nextPos.y, 0.25f, maxWorldY - 0.25f);
                
                Position = nextPos;
            }
            
            public virtual void SetTarget(Vector2 target)
            {
                var tileManager = Core.CoreManager.TileManager;
            
                if (tileManager != null && tileManager.WorldToTile(target).Type == World.Tiles.TileType.Water)
                {
                    return;
                }
            
                Vector2 currentDir = HasTarget ? (TargetPosition - Position).normalized : Vector2.zero;
                Vector2 newDir = (target - Position).sqrMagnitude > 0.001f ? (target - Position).normalized : Vector2.zero;
            
                TargetPosition = target;
            
                if (tileManager != null && Vector2.Distance(target, lastPathTarget) > 0.4f)
                {
                    lastPathTarget = target;
                    World.Tiles.PathfindingSystem.TryFindPath(Position, target, tileManager, waypoints);
                }
                
                if (currentDir != Vector2.zero && newDir != Vector2.zero)
                {
                    float dot = Vector2.Dot(currentDir, newDir);
            
                    if (dot < 0f)
                    {
                        Speed *= -dot;
                    }
                }
            }
            
            public virtual void Stop()
            {
                waypoints.Clear();
                
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

            public virtual float GetStamina() => 0f;
            public virtual float GetCurrentStaminaPercent() => 0f;
            public virtual void SetStamina(float amount) {}
            public virtual void ChangeStamina(float amount) {}

            public virtual float GetMorale() => 0f;
            public virtual float GetCurrentMoralePercent() => 0f;
            public virtual void SetMorale(float amount) {}
            public virtual void ChangeMorale(float amount) {}
            
            public virtual float GetHunger() => 0f;
            public virtual void SetHunger(float amount) {}
            public virtual void ChangeHunger(float amount) {}
            
            public virtual int GetAge() => 0;
            public virtual void SetAge(int age) {}
            public virtual void ChangeAge(int age) {}

            public virtual List<string> GetInventory() => null;
            public virtual void AddItem(string item) {}
            public virtual void RemoveItem(string item) {}

            public virtual void SetAutonomous(bool autonomy)
            {
                IsAutonomous = autonomy;
            }

            public void SetEquipmentStats(float armor, float armorPenetration, float speedMultiplier)
            {
                Armor = Mathf.Max(0f, armor);
                ArmorPenetration = Mathf.Max(0f, armorPenetration);
                EquipmentSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.4f, 1.5f);
            }

            // AI

            public UnitAIState AIState {get; set;} = UnitAIState.Idle;

            // Combat

            public event System.Action<Unit> OnAttacked;

            public virtual bool CanAttack => true;

            private float baseAttackDamage;
            private float baseAttackRange;
            private float baseAttackCooldown;
            
            public float AttackDamage {get; protected set;}
            public float AttackRange {get; protected set;}
            public float AttackCooldown {get; protected set;}
            
            public Unit AttackTarget {get; set;}
            
            private float AttackCooldownTimer = 0f;
            
            public bool IsAttackReady => AttackCooldownTimer <= 0;

            public void UpdateCombatCooldown(float delta)
            {
                if (AttackCooldownTimer > 0) AttackCooldownTimer -= delta;
                
                if (DecisionTimer > 0) DecisionTimer -= delta;
            }

            public void MarkAttackPerformed(Unit target)
            {
                AttackCooldownTimer = AttackCooldown;

                OnAttacked?.Invoke(target);
            }

            public void ClearAttackTarget()
            {
                AttackTarget = null;
            }

            public void InitializeCombat(float damage, float range, float cooldown)
            {
                baseAttackDamage = damage;
                baseAttackRange = range;
                baseAttackCooldown = cooldown;

                RecalculateCombatStats();
            }

            public virtual void RecalculateCombatStats()
            {
                float damage = baseAttackDamage;
                float range = baseAttackRange;
                float cooldown = baseAttackCooldown;

                AttackDamage = damage;
                AttackRange = range;
                AttackCooldown = cooldown;
            }
        }
    }
}