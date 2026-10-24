using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ER.World.Population;
using ER.World.Population.Visual;
using ER.World.Construction;
using ER.World.Inventory;
using ER.World.Polity;

namespace ER.World.Command.Interaction
{
    public class StrategicInteractionManager : MonoBehaviour
    {
        public static StrategicInteractionManager Instance { get; private set; }


        [Header("Selection Visualizer")]
        [SerializeField] private Color selectionRingColor = new Color(0.2f, 0.9f, 1.0f, 0.9f);

        [SerializeField] private float ringRadius = 1.35f;


        [Header("System References")]
        [SerializeField] private PopulationManager populationManager;

        [SerializeField] private ConstructionManager constructionManager;

        [SerializeField] private InventoryManager inventoryManager;

        [SerializeField] private PolityRegionManager polityManager;


        private Camera mainCamera;

        private int selectedPopId = -1;

        private int selectedPopIndex = -1;

        private bool isBuildMode = false;

        private BuildingType activeBuildType = BuildingType.DugoutShelter;

        private bool isInitialized = false;


        public bool IsInitialized => isInitialized;

        public int SelectedPopId => selectedPopId;


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

            mainCamera = Camera.main;

            if (populationManager == null) populationManager = PopulationManager.Instance;

            if (constructionManager == null) constructionManager = ConstructionManager.Instance;

            if (inventoryManager == null) inventoryManager = InventoryManager.Instance;

            if (polityManager == null) polityManager = PolityRegionManager.Instance;

            isInitialized = true;
        }


        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;

                if (mainCamera == null) return;
            }

            HandleInputHotkeys();

            HandleMouseClicks();
        }


        private void HandleInputHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                isBuildMode = !isBuildMode || activeBuildType != BuildingType.DugoutShelter;

                activeBuildType = BuildingType.DugoutShelter;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                isBuildMode = !isBuildMode || activeBuildType != BuildingType.DryingRack;

                activeBuildType = BuildingType.DryingRack;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isBuildMode = false;

                selectedPopId = -1;

                selectedPopIndex = -1;
            }
        }


        private void HandleMouseClicks()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);

                Vector2 clickPoint = new Vector2(mouseWorld.x, mouseWorld.y);

                if (isBuildMode)
                {
                    PlaceCurrentBlueprint(clickPoint);

                    return;
                }

                Collider2D hit = Physics2D.OverlapPoint(clickPoint);

                if (hit != null && hit.TryGetComponent<AgentSelectable>(out AgentSelectable selectable))
                {
                    selectedPopId = selectable.PopId;

                    selectedPopIndex = selectable.PopIndex;
                }
                else
                {
                    selectedPopId = -1;

                    selectedPopIndex = -1;
                }
            }

            if (Input.GetMouseButtonDown(1) && isBuildMode)
            {
                isBuildMode = false;
            }
        }


        private void PlaceCurrentBlueprint(Vector2 worldPos)
        {
            if (constructionManager == null || polityManager == null || polityManager.Anchors.Count == 0)
            {
                return;
            }

            var playerAnchor = polityManager.Anchors[0];

            constructionManager.PlaceBlueprint(activeBuildType, worldPos.x, worldPos.y, playerAnchor.OwnerPolityId, playerAnchor.AnchorId);

            if (!Input.GetKey(KeyCode.LeftShift))
            {
                isBuildMode = false;
            }
        }


        private void OnDrawGizmos()
        {
            if (selectedPopId > 0 && populationManager != null && populationManager.IsInitialized)
            {
                NativeArray<PopData> pops = populationManager.ActivePops;

                if (pops.IsCreated && selectedPopIndex >= 0 && selectedPopIndex < pops.Length)
                {
                    PopData pop = pops[selectedPopIndex];

                    if (pop.IsAlive && pop.PopId == selectedPopId)
                    {
                        Vector3 pos = new Vector3(pop.WorldPositionX, pop.WorldPositionY, -3.15f);

                        Gizmos.color = selectionRingColor;

                        Gizmos.DrawWireSphere(pos, ringRadius);
                    }
                }
            }

            if (isBuildMode && mainCamera != null)
            {
                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);

                Vector3 pos = new Vector3(mouseWorld.x, mouseWorld.y, -2.5f);

                Gizmos.color = new Color(0.2f, 1.0f, 0.4f, 0.8f);

                Gizmos.DrawWireCube(pos, new Vector3(2.8f, 2.8f, 1.6f));
            }
        }


        private void OnGUI()
        {
            DrawControlsHint();

            DrawSettlementStorageBar();

            if (selectedPopId > 0 && populationManager != null && populationManager.IsInitialized)
            {
                DrawSelectedPopCard();
            }
        }


        private void DrawSettlementStorageBar()
        {
            if (polityManager == null || polityManager.Anchors.Count == 0 || inventoryManager == null)
            {
                return;
            }

            int storageId = polityManager.Anchors[0].StorageInventoryId;

            if (storageId <= 0)
            {
                return;
            }

            uint wood = inventoryManager.GetItemCount(storageId, 1);

            uint flint = inventoryManager.GetItemCount(storageId, 2);

            uint berries = inventoryManager.GetItemCount(storageId, 3);

            uint meat = inventoryManager.GetItemCount(storageId, 4);

            uint hides = inventoryManager.GetItemCount(storageId, 5);

            uint fur = inventoryManager.GetItemCount(storageId, 6);

            uint parkas = inventoryManager.GetItemCount(storageId, 7);

            uint smoked = inventoryManager.GetItemCount(storageId, 8);

            int barWidth = 640;

            int barHeight = 34;

            Rect rect = new Rect((Screen.width - barWidth) / 2, 10, barWidth, barHeight);

            GUI.Box(rect, "");

            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12));

            GUILayout.BeginHorizontal();

            GUILayout.Label($"Дрова: <b>{wood}</b>");

            GUILayout.Label($"Кремень: <b>{flint}</b>");

            GUILayout.Label($"Ягоды: <b>{berries}</b>");

            string meatColor = meat > 0 ? "#FF8888" : "#AAAAAA";

            GUILayout.Label($"Сырое: <color={meatColor}><b>{meat}</b></color>");

            string smokedColor = smoked > 0 ? "#FFCC66" : "#AAAAAA";

            GUILayout.Label($"Копчёное: <color={smokedColor}><b>{smoked}</b></color>");

            GUILayout.Label($"Парки: <b>{parkas}</b>");

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }


        private void DrawControlsHint()
        {
            float morale = 70.0f;

            int deadCount = 0;

            if (ER.World.Culture.PolityMoraleManager.Instance != null && ER.World.Culture.PolityMoraleManager.Instance.IsInitialized)
            {
                morale = ER.World.Culture.PolityMoraleManager.Instance.GetPolityMorale(1);

                deadCount = ER.World.Culture.PolityMoraleManager.Instance.UnburiedCorpseCount;
            }

            GUI.Box(new Rect(10, 10, 310, isBuildMode ? 105 : 90), "");

            GUILayout.BeginArea(new Rect(15, 12, 300, 100));

            string moraleColor = morale < 40.0f ? "#FF5555" : (morale > 75.0f ? "#55FF55" : "#FFFF55");

            GUILayout.Label($"<b>УПРАВЛЕНИЕ ПОЛИТИЕЙ</b> | Мораль: <color={moraleColor}><b>{morale:F0}%</b></color>");

            if (deadCount > 0)
            {
                GUILayout.Label($"<color=#FF7777>Непогребённых усопших: {deadCount}</color>");
            }

            GUILayout.Label(isBuildMode ? $"<color=#55FF55>СТРОЙКА ({activeBuildType}): [КЛИК] Поставить</color>" : "[B] - Полуземлянка | [R] - Коптильня");

            GUILayout.Label("[ЛКМ по человеку] - Карточка жителя");

            int totalBeds = 0;

            int occupiedBeds = 0;

            if (ER.World.Housing.HouseholdManager.Instance != null && ER.World.Housing.HouseholdManager.Instance.IsInitialized)
            {
                ER.World.Housing.HouseholdManager.Instance.GetHousingStats(1, out _, out totalBeds, out occupiedBeds);
            }

            GUILayout.Label($"Жильё: <b>{occupiedBeds}/{totalBeds}</b> мест занято");

            GUILayout.EndArea();
        }


        private void DrawSelectedPopCard()
        {
            NativeArray<PopData> pops = populationManager.ActivePops;

            if (!pops.IsCreated || selectedPopIndex < 0 || selectedPopIndex >= pops.Length)
            {
                return;
            }

            PopData pop = pops[selectedPopIndex];

            if (!pop.IsAlive || pop.PopId != selectedPopId)
            {
                return;
            }

            int panelWidth = 320;

            int panelHeight = 240;

            Rect cardRect = new Rect(10, Screen.height - panelHeight - 10, panelWidth, panelHeight);

            GUI.Box(cardRect, "");

            GUILayout.BeginArea(new Rect(cardRect.x + 12, cardRect.y + 10, cardRect.width - 24, cardRect.height - 20));

            string genderStr = pop.Gender == Gender.Male ? "Мужчина" : "Женщина";

            int ageYears = pop.AgeInDays / 16;

            GUILayout.Label($"<b>Соплеменник #{pop.PopId}</b> ({genderStr}, {ageYears} лет)", GUILayout.Height(20));

            GUILayout.Label($"Стадия жизни: <b>{pop.LifeStage}</b> | Состояние: <b>{pop.ActivityState}</b>");

            float healthPercent = (pop.Health / 255.0f) * 100.0f;

            GUILayout.Label($"Здоровье: <b>{healthPercent:F0}%</b> ({pop.Health}/255)");

            GUILayout.Label($"Калории: <b>{pop.CurrentCalories:F0} / {pop.MaxCalories:F0} ккал</b>");

            string tempColor = pop.BodyTemperature < 33.5f ? "#55FFFF" : "#FFFFFF";

            GUILayout.Label($"Температура ядра: <color={tempColor}><b>{pop.BodyTemperature:F1}°C</b></color>");

            string clothes = pop.WarmthInsulation >= 150 ? "Меховая парка (+180)" : "Набедренная повязка (+20)";

            GUILayout.Label($"Одежда: <b>{clothes}</b>");

            GUILayout.Space(6);

            GUILayout.Label("<b>Личный инвентарь:</b>");

            if (inventoryManager != null && inventoryManager.IsInitialized)
            {
                uint wood = inventoryManager.GetItemCount(pop.PersonalInventoryId, 1);

                uint flint = inventoryManager.GetItemCount(pop.PersonalInventoryId, 2);

                uint meat = inventoryManager.GetItemCount(pop.PersonalInventoryId, 4);

                uint smoked = inventoryManager.GetItemCount(pop.PersonalInventoryId, 8);

                uint hides = inventoryManager.GetItemCount(pop.PersonalInventoryId, 5);

                GUILayout.Label($"Дрова: {wood} | Кремень: {flint} | Мясо: {meat} | Копчёное: {smoked} | Шкуры: {hides}");
            }

            string homeStr = pop.DwellingId > 0 ? $"Полуземлянка #{pop.DwellingId}" : "<color=#FF7777>Бездомный (у костра)</color>";

            GUILayout.Label($"Дом: <b>{homeStr}</b>");

            GUILayout.EndArea();
        }
    }
}