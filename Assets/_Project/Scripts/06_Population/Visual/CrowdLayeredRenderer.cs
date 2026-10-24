using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Population;
using ER.World.Population.Tasks;
using ER.World.Inventory;

namespace ER.World.Population.Visual
{
    public class CrowdLayeredRenderer : MonoBehaviour
    {
        public static CrowdLayeredRenderer Instance { get; private set; }


        [Header("Sprites Setup (Paper Doll Layers)")]
        [SerializeField] private Sprite bodySprite;

        [SerializeField] private Sprite furParkaSprite;

        [SerializeField] private Sprite spearSprite;

        [SerializeField] private Sprite woodLogSprite;


        [Header("Visual & Animation Tuning")]
        [SerializeField] private float baseScale = 2.4f;

        [SerializeField] private float walkBobbingSpeed = 12.0f;

        [SerializeField] private float walkTiltAngle = 8.0f;

        [SerializeField] private float toolForwardOffset = 0.45f;

        [SerializeField] private float toolSideOffset = 0.40f;


        [Header("System References")]
        [SerializeField] private PopulationManager populationManager;

        [SerializeField] private PopTaskManager taskManager;

        [SerializeField] private InventoryManager inventoryManager;


        private readonly List<AgentVisualInstance> visualPool = new List<AgentVisualInstance>();

        private Transform poolContainer;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        private void Start()
        {
            EnsureInitialized();
        }


        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            ResolveReferences();

            EnsureFallbackSprites();

            GameObject containerObj = new GameObject("_AgentVisualPool");

            containerObj.transform.SetParent(transform);

            poolContainer = containerObj.transform;

            isInitialized = true;
        }


        private void ResolveReferences()
        {
            if (populationManager == null) populationManager = PopulationManager.Instance;

            if (populationManager == null) populationManager = FindFirstObjectByType<PopulationManager>();

            if (taskManager == null) taskManager = PopTaskManager.Instance;

            if (taskManager == null) taskManager = FindFirstObjectByType<PopTaskManager>();

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
        }


        private void EnsureFallbackSprites()
        {
            if (bodySprite == null)
            {
                bodySprite = CreateCircleSprite(32, new Color(0.95f, 0.78f, 0.62f));
            }

            if (furParkaSprite == null)
            {
                furParkaSprite = CreateCircleSprite(34, new Color(0.55f, 0.38f, 0.22f));
            }

            if (spearSprite == null)
            {
                spearSprite = CreateBarSprite(8, 36, new Color(0.75f, 0.75f, 0.75f));
            }

            if (woodLogSprite == null)
            {
                woodLogSprite = CreateBarSprite(12, 32, new Color(0.48f, 0.30f, 0.15f));
            }
        }


        private Sprite CreateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            float r = size * 0.46f;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist <= r)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32.0f);
        }


        private Sprite CreateBarSprite(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32.0f);
        }


        private void LateUpdate()
        {
            if (populationManager == null || !populationManager.IsInitialized)
            {
                ResolveReferences();

                if (populationManager == null || !populationManager.IsInitialized)
                {
                    return;
                }
            }

            NativeArray<PopData> pops = populationManager.ActivePops;

            int popCount = populationManager.TotalPopulationCount;

            if (!pops.IsCreated || popCount == 0)
            {
                HideUnusedVisuals(0);

                return;
            }

            EnsurePoolCapacity(popCount);

            float time = Time.time;

            for (int i = 0; i < popCount; i++)
            {
                PopData pop = pops[i];

                AgentVisualInstance visual = visualPool[i];

                if (!pop.IsAlive)
                {
                    visual.Root.SetActive(false);

                    continue;
                }

                visual.Root.SetActive(true);

                visual.Selectable.PopId = pop.PopId;

                visual.Selectable.PopIndex = i;

                float scaleModifier = 1.0f;

                if (pop.LifeStage == PopLifeStage.Infant) scaleModifier = 0.45f;
                else if (pop.LifeStage == PopLifeStage.Child) scaleModifier = 0.65f;
                else if (pop.LifeStage == PopLifeStage.Youth) scaleModifier = 0.85f;

                float finalScale = baseScale * scaleModifier;

                visual.Root.transform.localScale = new Vector3(finalScale, finalScale, 1.0f);

                visual.Root.transform.position = new Vector3(pop.WorldPositionX, pop.WorldPositionY, -3.0f);

                float speedSqr = pop.VelocityX * pop.VelocityX + pop.VelocityY * pop.VelocityY;

                bool isMoving = speedSqr > 0.05f;

                float baseAngle = 0.0f;

                if (isMoving)
                {
                    baseAngle = Mathf.Atan2(pop.VelocityY, pop.VelocityX) * Mathf.Rad2Deg - 90.0f;
                }

                float wobbleTilt = 0.0f;

                if (isMoving)
                {
                    wobbleTilt = Mathf.Sin(time * walkBobbingSpeed + (pop.PopId * 1.35f)) * walkTiltAngle;
                }

                visual.Root.transform.rotation = Quaternion.Euler(0.0f, 0.0f, baseAngle + wobbleTilt);

                int sortingBase = 15000 + Mathf.Clamp(10000 - ((int)(pop.WorldPositionY * 4.0f) % 8000), 100, 12000);

                visual.BodyRenderer.sortingOrder = sortingBase;

                visual.ParkaRenderer.sortingOrder = sortingBase + 1;

                visual.ToolRenderer.sortingOrder = sortingBase + 2;

                bool hasParka = pop.WarmthInsulation >= 150;

                visual.ParkaRenderer.enabled = hasParka;

                if (hasParka && visual.ParkaRenderer.sprite == null)
                {
                    visual.ParkaRenderer.sprite = furParkaSprite;
                }

                bool holdsWood = false;

                if (inventoryManager != null && inventoryManager.IsInitialized)
                {
                    holdsWood = inventoryManager.GetItemCount(pop.PersonalInventoryId, 1) > 0;
                }

                if (holdsWood)
                {
                    visual.ToolRenderer.enabled = true;

                    visual.ToolRenderer.sprite = woodLogSprite;

                    visual.ToolRenderer.transform.localPosition = new Vector3(toolSideOffset, 0.1f, -0.05f);

                    visual.ToolRenderer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 25.0f);
                }
                else if (pop.Gender == Gender.Male && pop.LifeStage >= PopLifeStage.Youth)
                {
                    visual.ToolRenderer.enabled = true;

                    visual.ToolRenderer.sprite = spearSprite;

                    visual.ToolRenderer.transform.localPosition = new Vector3(toolSideOffset, toolForwardOffset, -0.05f);

                    visual.ToolRenderer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, -15.0f);
                }
                else
                {
                    visual.ToolRenderer.enabled = false;
                }
            }

            HideUnusedVisuals(popCount);
        }


        private void EnsurePoolCapacity(int requiredCount)
        {
            while (visualPool.Count < requiredCount)
            {
                int index = visualPool.Count;

                GameObject agentObj = new GameObject($"AgentView_{index}");

                agentObj.transform.SetParent(poolContainer);

                GameObject bodyObj = new GameObject("BodyLayer");

                bodyObj.transform.SetParent(agentObj.transform);

                bodyObj.transform.localPosition = Vector3.zero;

                SpriteRenderer bodyRend = bodyObj.AddComponent<SpriteRenderer>();

                bodyRend.sprite = bodySprite;

                bodyRend.color = Color.white;

                GameObject parkaObj = new GameObject("ParkaLayer");

                parkaObj.transform.SetParent(agentObj.transform);

                parkaObj.transform.localPosition = new Vector3(0.0f, 0.0f, -0.01f);

                SpriteRenderer parkaRend = parkaObj.AddComponent<SpriteRenderer>();

                parkaRend.sprite = furParkaSprite;

                parkaRend.color = Color.white;

                parkaRend.enabled = false;

                GameObject toolObj = new GameObject("ToolLayer");

                toolObj.transform.SetParent(agentObj.transform);

                toolObj.transform.localPosition = new Vector3(toolSideOffset, toolForwardOffset, -0.02f);

                SpriteRenderer toolRend = toolObj.AddComponent<SpriteRenderer>();

                toolRend.color = Color.white;

                toolRend.enabled = false;

                BoxCollider2D collider = agentObj.AddComponent<BoxCollider2D>();

                collider.size = new Vector2(0.8f, 0.8f);

                AgentSelectable selectable = agentObj.AddComponent<AgentSelectable>();

                visualPool.Add(new AgentVisualInstance
                {
                    Root = agentObj,
                    BodyRenderer = bodyRend,
                    ParkaRenderer = parkaRend,
                    ToolRenderer = toolRend,
                    Collider = collider,
                    Selectable = selectable
                });
            }
        }


        private void HideUnusedVisuals(int activeCount)
        {
            for (int i = activeCount; i < visualPool.Count; i++)
            {
                if (visualPool[i].Root.activeSelf)
                {
                    visualPool[i].Root.SetActive(false);
                }
            }
        }


        private class AgentVisualInstance
        {
            public GameObject Root;

            public SpriteRenderer BodyRenderer;

            public SpriteRenderer ParkaRenderer;

            public SpriteRenderer ToolRenderer;

            public BoxCollider2D Collider;

            public AgentSelectable Selectable;
        }
    }
}