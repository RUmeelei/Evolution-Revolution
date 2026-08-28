using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Players
    {
        // using Government;
        using Culture;
        using Resources;

        [System.Serializable]
        public class PlayerData
        {
            public string PlayerId;
            public string PlayerName;

            // public GovernmentData PlayerGovernment;
            public CultureData PlayerCulture;

            public PlayerData(){}

            public PlayerData(string playerId, string playerName)
            {
                PlayerId = playerId;

                PlayerName = playerName;
            }

            public Dictionary<string, ResourceData> PlayerResources = new Dictionary<string, ResourceData>();

            private ResourceData GetOrCreateResource(string resourceId)
            {
                if (!PlayerResources.TryGetValue(resourceId, out var resource))
                {
                    resource = new ResourceData(true, 0f);

                    PlayerResources[resourceId] = resource;
                }

                return resource;
            }

            public float GetFood()
            {
                var resource = GetOrCreateResource("Fruits");

                return resource.Amount;
            }

            public void GatherResource(string resourceId, float amount)
            {
                var resource = GetOrCreateResource(resourceId);

                resource.Amount += amount;
            }

            public void SpendResource(string resourceId, float amount)
            {
                var resource = GetOrCreateResource(resourceId);

                resource.Amount = Mathf.Max(0f, resource.Amount - amount);
            }
        }
    }
}