using TMPro;
using UnityEngine;
using ER.Core.Localization;

namespace ER.Core.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization Key")]
        [SerializeField] private string localizationKey;


        private TextMeshProUGUI textComponent;


        public string LocalizationKey => localizationKey;


        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }


        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += RefreshText;
            }

            RefreshText();
        }


        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshText;
            }
        }


        public void SetKey(string newKey)
        {
            localizationKey = newKey;

            RefreshText();
        }


        public void RefreshText()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TextMeshProUGUI>();
            }

            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(localizationKey))
            {
                textComponent.text = LocalizationManager.Instance.GetText(localizationKey);
            }
        }
    }
}