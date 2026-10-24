using UnityEngine;

namespace ER.Core.UI
{
    [CreateAssetMenu(fileName = "OnyxThemeConfig", menuName = "ER/01_Core/UI/Onyx Theme Config")]
    public class OnyxThemeConfig : ScriptableObject
    {
        [Header("Background Surfaces")]
        [SerializeField] private Color surfaceAbsoluteBlack = new Color(0.039f, 0.047f, 0.063f, 0.95f);

        [SerializeField] private Color surfaceDarkSlate = new Color(0.067f, 0.078f, 0.094f, 0.90f);

        [SerializeField] private Color surfacePanelBorder = new Color(0.102f, 0.122f, 0.149f, 1.0f);


        [Header("Typography")]
        [SerializeField] private Color textPrimaryColdWhite = new Color(0.910f, 0.929f, 0.949f, 1.0f);

        [SerializeField] private Color textSecondarySteel = new Color(0.690f, 0.769f, 0.871f, 1.0f);

        [SerializeField] private Color textMutedDark = new Color(0.450f, 0.500f, 0.580f, 1.0f);


        [Header("Interactive Accents")]
        [SerializeField] private Color accentAzurePrimary = new Color(0.290f, 0.565f, 0.851f, 1.0f);

        [SerializeField] private Color accentTealVibrant = new Color(0.251f, 0.878f, 0.816f, 1.0f);

        [SerializeField] private Color accentWarningAmber = new Color(0.880f, 0.580f, 0.200f, 1.0f);

        [SerializeField] private Color accentAlertCrimson = new Color(0.850f, 0.250f, 0.250f, 1.0f);


        public Color SurfaceAbsoluteBlack => surfaceAbsoluteBlack;

        public Color SurfaceDarkSlate => surfaceDarkSlate;

        public Color SurfacePanelBorder => surfacePanelBorder;

        public Color TextPrimaryColdWhite => textPrimaryColdWhite;

        public Color TextSecondarySteel => textSecondarySteel;

        public Color TextMutedDark => textMutedDark;

        public Color AccentAzurePrimary => accentAzurePrimary;

        public Color AccentTealVibrant => accentTealVibrant;

        public Color AccentWarningAmber => accentWarningAmber;

        public Color AccentAlertCrimson => accentAlertCrimson;
    }
}