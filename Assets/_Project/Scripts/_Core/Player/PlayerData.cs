using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Players
    {
        using Politics.Internal.Government;
        using Culture;
        using Resources;

        [System.Serializable]
        public class PlayerData
        {
            public string PlayerId;
            public string PlayerName;

            public bool PlayerDefeated;

            public bool PlayerAI;

            // public GovernmentData PlayerGovernment;
            public CultureData PlayerCulture;

            public bool PlayerSpectator;

            public PlayerData(){}

            public PlayerData(string playerId, string playerName, bool isAI = false)
            {
                PlayerId = playerId;
                PlayerName = playerName;

                PlayerDefeated = false;

                PlayerAI = isAI;
            }

            public Dictionary<string, ResourceData> PlayerResources = new Dictionary<string, ResourceData>();

            public ResourceData GetOrCreateResource(string resourceId)
            {
                if (!PlayerResources.TryGetValue(resourceId, out var resource))
                {
                    resource = new ResourceData(true, 0f);

                    PlayerResources[resourceId] = resource;
                }

                return resource;
            }

            public float GetFood() => GetOrCreateResource("Fruits").Amount;

            public float GetIron() => GetOrCreateResource("Iron").Amount;
            
            public float GetMaterials() => GetIron();
            
            public float GetWood() => GetOrCreateResource("Wood").Amount;
            
            public float GetStone() => GetOrCreateResource("Stone").Amount;
            
            public float GetMoney() => GetOrCreateResource("Money").Amount;

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