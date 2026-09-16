using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Core;
        using Configs;
        using Simulation;
        using Culture;
        using Controls;

        [RequireComponent(typeof(SpriteRenderer))]
        public class UnitVisual : MonoBehaviour
        {
            [Header("Main")]
            public bool EnableLogging;
            
            private SpriteRenderer SpriteRenderer;
            private Unit Unit;
            private UnitSpriteSet Sprites;
            private UnitSpriteDirection CurrentDirection = UnitSpriteDirection.Down;

            private float BlinkTimer = 0f;
            private bool IsBlinking = false;
            private float BlinkInterval = 3f;
            private float BlinkDuration = 0.15f;

            private MainConfig mainConfig;

            private SimulationManager simulationManager;
            private UnitVisualPoolManager unitVisualPoolManager;

            void Awake()
            {
                SpriteRenderer = GetComponent<SpriteRenderer>();
            }

            void Start()
            {
                mainConfig = CoreManager.MainConfig;
                
                unitVisualPoolManager = CoreManager.UnitVisualPoolManager;
            }

            void OnDestroy()
            {
                if (Unit != null)
                {
                    Unit.OnPositionChanged -= OnUnitPositionChanged;
                    Unit.OnHealthChanged -= OnUnitHealthChanged;
                    Unit.OnDied -= OnUnitDied;
                    Unit = null;
                }

                simulationManager.OnTick -= UpdateBlink;
            }

            void Update()
            {
                if (Unit == null) return;
                
                Vector3 targetPos = new Vector3(Unit.Position.x, Unit.Position.y, 0);
                Vector3 currentPos = transform.position;

                if (Vector3.Distance(currentPos, targetPos) > 0.01f)
                {
                    Vector2 direction = (targetPos - currentPos).normalized;

                    UpdateDirection(direction);

                    transform.position = Vector3.Lerp(currentPos, targetPos, mainConfig.UnitSmoothSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = targetPos;
                }
            }

            public void Initialize(Unit unit, UnitSpriteSet sprites)
            {
                simulationManager = CoreManager.SimulationManager;

                Unit = unit;

                Sprites = sprites;

                transform.position = new Vector3(unit.Position.x, unit.Position.y, 0);

                SetDirection(UnitSpriteDirection.Down);

                BlinkTimer = Random.Range(0f, BlinkInterval);
                
                Unit.OnPositionChanged += OnUnitPositionChanged;
                Unit.OnHealthChanged += OnUnitHealthChanged;
                Unit.OnDied += OnUnitDied;
            }

            private void OnUnitPositionChanged(Vector2 position)
            {
                
            }

            private void OnUnitHealthChanged(float health)
            {
                if (health < 30) SpriteRenderer.color = Color.red;
                else SpriteRenderer.color = Color.white;
            }

            private void OnUnitDied()
            {
                StartCoroutine(DeathAnimation());
            }

            private System.Collections.IEnumerator DeathAnimation()
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;

                float timer = 0f;
                float duration = 0.5f;

                Color startColor = SpriteRenderer.color;

                while (timer < duration)
                {
                    timer += Time.deltaTime;

                    float t = timer / duration;

                    Color color = startColor;

                    color.a = Mathf.Lerp(1f, 0f, t);

                    SpriteRenderer.color = color;

                    yield return null;
                }

                if (unitVisualPoolManager != null) unitVisualPoolManager.Return(this);
            }

            public void Release()
            {
                if (Unit != null)
                {
                    Unit.OnPositionChanged -= OnUnitPositionChanged;
                    Unit.OnHealthChanged -= OnUnitHealthChanged;
                    Unit.OnDied -= OnUnitDied;
                    Unit = null;
                }

                simulationManager.OnTick -= UpdateBlink;

                SpriteRenderer.color = Color.white;
                
                gameObject.SetActive(false);
            }

            public void UpdateSprites(UnitSpriteSet newSprites)
            {
                Sprites = newSprites;

                SetDirection(CurrentDirection);
            }

            private void UpdateDirection(Vector2 direction)
            {
                float absX = Mathf.Abs(direction.x);
                float absY = Mathf.Abs(direction.y);

                UnitSpriteDirection newDir = absX > absY ? (direction.x > 0 ? UnitSpriteDirection.Right : UnitSpriteDirection.Left) : (direction.y > 0 ? UnitSpriteDirection.Up : UnitSpriteDirection.Down);

                if (newDir != CurrentDirection) SetDirection(newDir);
            }

            private void SetDirection(UnitSpriteDirection dir)
            {
                CurrentDirection = dir;

                SpriteRenderer.sprite = GetSprite(dir);
            }

            private Sprite GetSprite(UnitSpriteDirection dir)
            {
                if (IsBlinking)
                {
                    return dir switch
                    {
                        UnitSpriteDirection.Up => Sprites.UpBlink,
                        UnitSpriteDirection.Right => Sprites.RightBlink,
                        UnitSpriteDirection.Down => Sprites.DownBlink,
                        UnitSpriteDirection.Left => Sprites.LeftBlink,
                        _ => Sprites.Down
                    };
                }
                else
                {
                    return dir switch
                    {
                        UnitSpriteDirection.Up => Sprites.Up,
                        UnitSpriteDirection.Right => Sprites.Right,
                        UnitSpriteDirection.Down => Sprites.Down,
                        UnitSpriteDirection.Left => Sprites.Left,
                        _ => Sprites.Down
                    };
                }
            }

            private void UpdateBlink(float delta)
            {
                BlinkTimer += delta;

                if (!IsBlinking)
                {
                    if (BlinkTimer >= BlinkInterval)
                    {
                        IsBlinking = true;

                        BlinkTimer = 0f;

                        SetDirection(CurrentDirection);
                    }
                }
                else
                {
                    if (BlinkTimer >= BlinkDuration)
                    {
                        IsBlinking = false;

                        BlinkTimer = 0f;

                        SetDirection(CurrentDirection);
                    }
                }
            }
        }
    }
}