using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ER
{
    namespace UI
    {
        namespace InGame
        {
            using Core;
            using Culture;
            using Configs;
            using Players;

            public class MainPanelManager : MonoBehaviour
            {
                public static MainPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private TextMeshProUGUI CultureNameText;
                [SerializeField] private TextMeshProUGUI CultureIdText;
                [SerializeField] private TextMeshProUGUI CultureIdentityText;
                [SerializeField] private TextMeshProUGUI CultureDescriptionText;
                
                private CultureManager cultureManager;
                private PlayerManager playerManager;

                private MainConfig mainConfig;
                private UIConfig uiConfig;

                void Awake()
                {
                    if (transform.parent != null)
                    {
                        transform.SetParent(null);
                    }
                
                    if (Instance != null && Instance != this)
                    {
                        Destroy(gameObject); return;
                    }

                    Instance = this;

                    DontDestroyOnLoad(gameObject);

                    CoreManager.RegisterMainPanelManager(this);
                }

                void Start()
                {
                    mainConfig = CoreManager.MainConfig;
                    
                    uiConfig = CoreManager.UIConfig;

                    cultureManager = CoreManager.CultureManager;

                    playerManager = CoreManager.PlayerManager;

                    // cultureManager.OnCultureTick += UpdateUI;
                }

                void OnDestroy()
                {
                    // if (cultureManager != null) cultureManager.OnCultureTick -= UpdateUI;
                }

                void Update()
                {
                    UpdateUI(0f);
                }

                private void UpdateUI(float delta)
                {
                    var culture = cultureManager.GetCulture("CUL_0001");

                    var player = playerManager.GetPlayer("PLAYER_0001");

                    if (CultureNameText != null)
                    {
                        Color color = culture.CultureDefeated ? uiConfig.ErrorColor : uiConfig.MainTextColor;

                        string text = culture.CultureDefeated ? "Defeated nation" : $"{culture.CultureName} | {player.GetFood():F0}";

                        CultureNameText.SetText($"{text}");
                        
                        CultureNameText.color = color;
                    }

                    if (CultureIdText != null)
                    {
                        CultureIdText.SetText($"{culture.CultureId}");
                    }

                    if (CultureIdentityText != null)
                    {
                        Color color = uiConfig.SecondTextColor;

                        float identity = culture.CultureIdentity;

                        if (identity < mainConfig.WarningIdentityThreshold) color = identity < mainConfig.ErrorIdentityThreshold ? uiConfig.ErrorColor : uiConfig.WarningColor;

                        CultureIdentityText.SetText($"Identity : {identity:F1}%");
                        
                        CultureIdentityText.color = color;
                    }
                    
                    if (CultureDescriptionText != null)
                    {
                        Color color = culture.CultureDefeated ? uiConfig.ErrorColor : uiConfig.MainTextColor;

                        string text = culture.CultureDefeated ? "A ruins of defeated nation. The death of civilization. Inevitable, sad or joyful, it doesn't matter anymore." : culture.CultureDescription;

                        CultureDescriptionText.SetText($"{text} | {culture.CultureTraits.Count}");
                        
                        CultureDescriptionText.color = color;
                    }
                }
            }
        }
    }
}