using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ER.Core.Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }


        [Header("Active Language")]
        [SerializeField] private string currentLanguageCode = "ru_RU";

        [SerializeField] private string fallbackLanguageCode = "en_US";


        private readonly Dictionary<string, string> activeLanguageDictionary = new Dictionary<string, string>();

        private readonly Dictionary<string, string> fallbackLanguageDictionary = new Dictionary<string, string>();

        private readonly List<string> availableLanguages = new List<string>();

        private bool isInitialized = false;


        public event Action OnLanguageChanged;


        public string CurrentLanguageCode => currentLanguageCode;

        public IReadOnlyList<string> AvailableLanguages => availableLanguages;

        public bool IsInitialized => isInitialized;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            InitializeLocalizationSystem();
        }


        public void InitializeLocalizationSystem()
        {
            ScanAvailableLanguages();

            LoadFallbackDictionary();

            LoadActiveDictionary(currentLanguageCode);

            isInitialized = true;
        }


        private string GetLocalizationDirectoryPath()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Localization");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }


        private void ScanAvailableLanguages()
        {
            availableLanguages.Clear();

            string dirPath = GetLocalizationDirectoryPath();

            string[] files = Directory.GetFiles(dirPath, "*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string languageCode = Path.GetFileNameWithoutExtension(files[i]);

                availableLanguages.Add(languageCode);
            }

            if (!availableLanguages.Contains("en_US"))
            {
                CreateDefaultEnglishPackage();

                availableLanguages.Add("en_US");
            }

            if (!availableLanguages.Contains("ru_RU"))
            {
                CreateDefaultRussianPackage();

                availableLanguages.Add("ru_RU");
            }
        }


        private void LoadFallbackDictionary()
        {
            fallbackLanguageDictionary.Clear();

            string filePath = Path.Combine(GetLocalizationDirectoryPath(), $"{fallbackLanguageCode}.json");

            if (!File.Exists(filePath))
            {
                return;
            }

            string jsonContent = File.ReadAllText(filePath);

            LocalizationPackage package = JsonUtility.FromJson<LocalizationPackage>(jsonContent);

            if (package != null && package.entries != null)
            {
                for (int i = 0; i < package.entries.Count; i++)
                {
                    LocalizationEntry entry = package.entries[i];

                    fallbackLanguageDictionary[entry.key] = entry.value;
                }
            }
        }


        private void LoadActiveDictionary(string languageCode)
        {
            activeLanguageDictionary.Clear();

            string filePath = Path.Combine(GetLocalizationDirectoryPath(), $"{languageCode}.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);

                LocalizationPackage package = JsonUtility.FromJson<LocalizationPackage>(jsonContent);

                if (package != null && package.entries != null)
                {
                    for (int i = 0; i < package.entries.Count; i++)
                    {
                        LocalizationEntry entry = package.entries[i];

                        activeLanguageDictionary[entry.key] = entry.value;
                    }
                }

                currentLanguageCode = languageCode;
            }
            else
            {
                currentLanguageCode = fallbackLanguageCode;
            }

            OnLanguageChanged?.Invoke();
        }


        public void SetLanguage(string languageCode)
        {
            if (currentLanguageCode == languageCode && activeLanguageDictionary.Count > 0)
            {
                return;
            }

            LoadActiveDictionary(languageCode);
        }


        public string GetText(string key)
        {
            if (activeLanguageDictionary.TryGetValue(key, out string localizedValue))
            {
                return localizedValue;
            }

            if (fallbackLanguageDictionary.TryGetValue(key, out string fallbackValue))
            {
                return fallbackValue;
            }

            return $"MISSING_{key}";
        }


        public string GetTextFormat(string key, params object[] args)
        {
            string rawPattern = GetText(key);

            try
            {
                return string.Format(rawPattern, args);
            }
            catch
            {
                return rawPattern;
            }
        }


        private void CreateDefaultEnglishPackage()
        {
            LocalizationPackage package = new LocalizationPackage
            {
                languageCode = "en_US",
                languageDisplayName = "English"
            };

            package.entries.Add(new LocalizationEntry { key = "season.spring", value = "Spring" });
            package.entries.Add(new LocalizationEntry { key = "season.summer", value = "Summer" });
            package.entries.Add(new LocalizationEntry { key = "season.autumn", value = "Autumn" });
            package.entries.Add(new LocalizationEntry { key = "season.winter", value = "Winter" });

            package.entries.Add(new LocalizationEntry { key = "terrain.plains", value = "Plains" });
            package.entries.Add(new LocalizationEntry { key = "terrain.hills", value = "Hills" });
            package.entries.Add(new LocalizationEntry { key = "terrain.mountains", value = "Mountains" });
            package.entries.Add(new LocalizationEntry { key = "terrain.water_shallow", value = "Shallow Water" });
            package.entries.Add(new LocalizationEntry { key = "terrain.water_deep", value = "Deep Ocean" });
            package.entries.Add(new LocalizationEntry { key = "terrain.swamp", value = "Swamp" });

            package.entries.Add(new LocalizationEntry { key = "ui.date_format", value = "Year {0}, {1}, Day {2:00}, {3:00}:00" });
            package.entries.Add(new LocalizationEntry { key = "ui.pause", value = "PAUSED" });

            SavePackageToFile("en_US", package);
        }


        private void CreateDefaultRussianPackage()
        {
            LocalizationPackage package = new LocalizationPackage
            {
                languageCode = "ru_RU",
                languageDisplayName = "Русский"
            };

            package.entries.Add(new LocalizationEntry { key = "season.spring", value = "Весна" });
            package.entries.Add(new LocalizationEntry { key = "season.summer", value = "Лето" });
            package.entries.Add(new LocalizationEntry { key = "season.autumn", value = "Осень" });
            package.entries.Add(new LocalizationEntry { key = "season.winter", value = "Зима" });

            package.entries.Add(new LocalizationEntry { key = "terrain.plains", value = "Равнины" });
            package.entries.Add(new LocalizationEntry { key = "terrain.hills", value = "Холмы" });
            package.entries.Add(new LocalizationEntry { key = "terrain.mountains", value = "Горы" });
            package.entries.Add(new LocalizationEntry { key = "terrain.water_shallow", value = "Мелководье / Река" });
            package.entries.Add(new LocalizationEntry { key = "terrain.water_deep", value = "Глубокие Воды" });
            package.entries.Add(new LocalizationEntry { key = "terrain.swamp", value = "Болото" });

            package.entries.Add(new LocalizationEntry { key = "ui.date_format", value = "Год {0}, {1}, День {2:00}, {3:00}:00" });
            package.entries.Add(new LocalizationEntry { key = "ui.pause", value = "ПАУЗА" });

            SavePackageToFile("ru_RU", package);
        }


        private void SavePackageToFile(string languageCode, LocalizationPackage package)
        {
            string filePath = Path.Combine(GetLocalizationDirectoryPath(), $"{languageCode}.json");

            string json = JsonUtility.ToJson(package, true);

            File.WriteAllText(filePath, json);
        }
    }
}