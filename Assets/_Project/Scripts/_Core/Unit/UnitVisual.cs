using UnityEngine;

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

            private static Material sharedLineMaterial;

            private SpriteRenderer SpriteRenderer;
            private Unit Unit;
            private UnitSpriteSet Sprites;
            private UnitSpriteDirection CurrentDirection = UnitSpriteDirection.Down;
            private Color CultureColor = Color.white;

            private bool isDying = false;

            private float BlinkTimer = 0f;
            private bool IsBlinking = false;
            private float BlinkInterval = 3f;
            private float BlinkDuration = 0.15f;

            private bool IsAttackFlashing = false;
            private Coroutine attackFlashCoroutine;

            private MainConfig mainConfig;
            private SimulationManager simulationManager;
            private UnitVisualPoolManager unitVisualPoolManager;
            private SelectionManager selectionManager;
            private CultureManager cultureManager;

            private LineRenderer selectionCircle;

            void Awake()
            {
                SpriteRenderer = GetComponent<SpriteRenderer>();
                SetupSelectionCircle();
            }

            void Start()
            {
                if (mainConfig == null) mainConfig = CoreManager.MainConfig;
                if (unitVisualPoolManager == null) unitVisualPoolManager = CoreManager.UnitVisualPoolManager;
            }

            void OnDestroy()
            {
                UnsubscribeUnitEvents();
                if (simulationManager != null) simulationManager.OnTick -= UpdateBlink;
            }

            void Update()
            {
                if (Unit == null || isDying) return;

                if (selectionCircle != null && selectionCircle.enabled != Unit.IsSelected)
                {
                    selectionCircle.enabled = Unit.IsSelected;
                }

                Vector3 targetPos = new Vector3(Unit.Position.x, Unit.Position.y, 0f);
                Vector3 currentPos = transform.position;
                Vector3 diff = targetPos - currentPos;

                if (diff.sqrMagnitude > 0.0001f)
                {
                    UpdateDirection(new Vector2(diff.x, diff.y).normalized);
                    float smooth = mainConfig != null ? mainConfig.UnitSmoothSpeed : 12f;
                    transform.position = Vector3.Lerp(currentPos, targetPos, smooth * Time.deltaTime);
                }
                else
                {
                    transform.position = targetPos;
                }

                if (!IsAttackFlashing && SpriteRenderer.color != CultureColor)
                {
                    SpriteRenderer.color = CultureColor;
                }
            }

            public void Initialize(Unit unit, UnitSpriteSet sprites)
            {
                mainConfig = CoreManager.MainConfig;
                unitVisualPoolManager = CoreManager.UnitVisualPoolManager;
                simulationManager = CoreManager.SimulationManager;
                selectionManager = CoreManager.SelectionManager;
                cultureManager = CoreManager.CultureManager;

                UnsubscribeUnitEvents();

                Unit = unit;
                Sprites = sprites;
                isDying = false;
                IsAttackFlashing = false;
                
                float scale = mainConfig != null && mainConfig.UnitVisualScale > 0.02f ? mainConfig.UnitVisualScale : 0.22f;

                transform.position = new Vector3(unit.Position.x, unit.Position.y, 0f);
                transform.localScale = new Vector3(scale, scale, 1f);

                SetDirection(UnitSpriteDirection.Down);

                BlinkTimer = Random.Range(0f, BlinkInterval);

                var culture = cultureManager != null ? cultureManager.GetCulture(unit.CultureId) : null;
                CultureColor = (culture != null) ? culture.CultureColor : Color.white;
                SpriteRenderer.color = CultureColor;

                Unit.OnPositionChanged += OnUnitPositionChanged;
                Unit.OnHealthChanged += OnUnitHealthChanged;
                Unit.OnDied += OnUnitDied;
                Unit.OnAttacked += OnUnitAttacked;

                if (simulationManager != null)
                {
                    simulationManager.OnTick -= UpdateBlink;
                    simulationManager.OnTick += UpdateBlink;
                }
            }

            private void UnsubscribeUnitEvents()
            {
                if (Unit != null)
                {
                    Unit.OnPositionChanged -= OnUnitPositionChanged;
                    Unit.OnHealthChanged -= OnUnitHealthChanged;
                    Unit.OnDied -= OnUnitDied;
                    Unit.OnAttacked -= OnUnitAttacked;
                    Unit = null;
                }
            }

            private void SetupSelectionCircle()
            {
                GameObject circleObj = new GameObject("SelectionCircle");
                circleObj.transform.SetParent(transform);
                circleObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);

                selectionCircle = circleObj.AddComponent<LineRenderer>();
                selectionCircle.useWorldSpace = false;
                selectionCircle.loop = true;
                selectionCircle.startWidth = 0.06f;
                selectionCircle.endWidth = 0.06f;
                selectionCircle.positionCount = 24;

                if (sharedLineMaterial == null)
                {
                    sharedLineMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                selectionCircle.sharedMaterial = sharedLineMaterial;

                selectionCircle.startColor = Color.green;
                selectionCircle.endColor = Color.green;
                selectionCircle.sortingLayerName = SpriteRenderer.sortingLayerName;
                selectionCircle.sortingOrder = 0;

                float radius = 0.15f;
                
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 24f;
                    selectionCircle.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.6f, -0.01f));
                }

                selectionCircle.enabled = false;
            }

            private void OnUnitPositionChanged(Vector2 position) { }
            private void OnUnitHealthChanged(float health) { }

            private void OnUnitDied()
            {
                if (Unit != null)
                {
                    Unit.OnDied -= OnUnitDied;
                }

                if (!gameObject.activeInHierarchy) return;
                StartCoroutine(DeathAnimation());
            }

            private void OnUnitAttacked(Unit target)
            {
                if (target?.UnitVisual == null || !target.UnitVisual.gameObject.activeInHierarchy) return;

                target.UnitVisual.TriggerHitFlash();
            }

            private System.Collections.IEnumerator DeathAnimation()
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;

                isDying = true;

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

                isDying = false;
                if (unitVisualPoolManager != null) unitVisualPoolManager.Return(this);
            }

            public void TriggerHitFlash()
            {
                if (!gameObject.activeInHierarchy) return;
            
                if (attackFlashCoroutine != null)
                {
                    StopCoroutine(attackFlashCoroutine);
                }
            
                attackFlashCoroutine = StartCoroutine(HitFlashRoutine());
            }
            
            private System.Collections.IEnumerator HitFlashRoutine()
            {
                IsAttackFlashing = true;
                SpriteRenderer.color = Color.yellow;
            
                yield return new WaitForSeconds(0.15f);
            
                IsAttackFlashing = false;
                attackFlashCoroutine = null;
            
                if (Unit != null && Unit.IsAlive)
                {
                    SpriteRenderer.color = CultureColor;
                }
            }

            public void Release()
            {
                if (attackFlashCoroutine != null)
                {
                    StopCoroutine(attackFlashCoroutine);

                    attackFlashCoroutine = null;
                }

                UnsubscribeUnitEvents();

                if (simulationManager != null) simulationManager.OnTick -= UpdateBlink;

                isDying = false;

                IsAttackFlashing = false;

                if (selectionCircle != null) selectionCircle.enabled = false;

                SpriteRenderer.color = Color.white;
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