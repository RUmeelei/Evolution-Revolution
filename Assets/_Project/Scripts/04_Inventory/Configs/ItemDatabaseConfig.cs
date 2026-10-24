using System.Collections.Generic;
using UnityEngine;

namespace ER.World.Inventory
{
    [CreateAssetMenu(fileName = "ItemDatabaseConfig", menuName = "ER/04_Inventory/Item Database Config")]
    public class ItemDatabaseConfig : ScriptableObject
    {
        [Header("Item Definitions")]
        [SerializeField] private List<ItemTypeConfig> itemTypes = new List<ItemTypeConfig>();


        private readonly Dictionary<ushort, ItemTypeConfig> lookup = new Dictionary<ushort, ItemTypeConfig>();

        private bool isLookupBuilt = false;


        public IReadOnlyList<ItemTypeConfig> ItemTypes => itemTypes;


        public void BuildLookup()
        {
            lookup.Clear();

            for (int i = 0; i < itemTypes.Count; i++)
            {
                ItemTypeConfig item = itemTypes[i];

                lookup[item.ItemTypeId] = item;
            }

            isLookupBuilt = true;
        }


        public bool TryGetItemConfig(ushort itemTypeId, out ItemTypeConfig config)
        {
            if (!isLookupBuilt)
            {
                BuildLookup();
            }

            return lookup.TryGetValue(itemTypeId, out config);
        }
    }
}