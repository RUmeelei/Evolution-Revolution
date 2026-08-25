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

            public class MainPanelManager : MonoBehaviour
            {
                public static MainPanelManager Instance {get; private set;}

                [Header("Main")]
                [SerializeField] private TextMeshProUGUI CultureNameText;
                [SerializeField] private TextMeshProUGUI CultureIdText;
                [SerializeField] private TextMeshProUGUI CultureIdentityText;
                [SerializeField] private TextMeshProUGUI CultureDescriptionText;
                
                private CultureManager cultureManager;

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
                    cultureManager = CoreManager.CultureManager;

                    uiConfig = CoreManager.UIConfig;
                }

                void Update()
                {
                    if (CultureNameText != null)
                    {
                        CultureNameText.SetText($"{cultureManager.GetCulture("CUL_0001").CultureName}");
                    }

                    if (CultureIdText != null)
                    {
                        CultureIdText.SetText($"{cultureManager.GetCulture("CUL_0001").CultureId}");
                    }

                    if (CultureIdentityText != null)
                    {
                        CultureIdentityText.SetText($"Identity : {Mathf.Round(cultureManager.GetCulture("CUL_0001").CultureIdentity)}%");
                    }
                    
                    if (CultureDescriptionText != null)
                    {
                        CultureDescriptionText.SetText($"{cultureManager.GetCulture("CUL_0001").CultureDescription}");
                    }
                }
            }
        }
    }
}