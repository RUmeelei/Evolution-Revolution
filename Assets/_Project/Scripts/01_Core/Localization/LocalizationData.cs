using System;
using System.Collections.Generic;

namespace ER.Core.Localization
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;

        public string value;
    }

    [Serializable]
    public class LocalizationPackage
    {
        public string languageCode;

        public string languageDisplayName;

        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }
}