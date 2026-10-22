using System;
using UnityEngine;

namespace ER
{
    namespace Politics
    {
        namespace Internal
        {
            namespace Government
            {
                public enum OrganizationType
                {
                    Tribe = 0,
                    State = 1,
                    Corporation = 2,
                    Marauders = 3,
                }

                [System.Serializable]
                public class GovernmentData
                {
                    public string CultureId;

                    public OrganizationType Type = OrganizationType.Tribe;

                    public string OrganizationName;

                    public float Stability = 100f;

                    public GovernmentData() {}

                    public GovernmentData(string cultureId, string cultureName, OrganizationType initialType = OrganizationType.Tribe)
                    {
                        CultureId = cultureId;

                        Type = initialType;

                        Stability = 100f;

                        UpdateOrganizationName(cultureName);
                    }

                    public void UpdateOrganizationName(string cultureName)
                    {
                        OrganizationName = Type switch
                        {
                            OrganizationType.State => $"State of {cultureName}",
                            OrganizationType.Corporation => $"{cultureName} Trade Syndicate",
                            OrganizationType.Marauders => $"{cultureName} Free Horde",
                            _ => $"{cultureName} Tribe"
                        };
                    }

                    public float GetInfluenceMultiplier()
                    {
                        return Type switch
                        {
                            OrganizationType.State => 1.4f,
                            OrganizationType.Corporation => 1.15f,
                            OrganizationType.Marauders => 0.6f,
                            _ => 1.0f
                        };
                    }

                    public float GetLogisticsYieldMultiplier()
                    {
                        return Type switch
                        {
                            OrganizationType.Corporation => 1.5f,
                            OrganizationType.State => 1.15f,
                            OrganizationType.Marauders => 0.75f,
                            _ => 1.0f
                        };
                    }

                    public float GetLootMultiplier()
                    {
                        return Type switch
                        {
                            OrganizationType.Marauders => 3.0f,
                            _ => 1.0f
                        };
                    }

                    public float GetMobilizationCostMultiplier()
                    {
                        return Type switch
                        {
                            OrganizationType.State => 0.7f,
                            OrganizationType.Marauders => 0.5f,
                            OrganizationType.Corporation => 1.25f,
                            _ => 1.0f
                        };
                    }
                }
            }
        }
    }
}