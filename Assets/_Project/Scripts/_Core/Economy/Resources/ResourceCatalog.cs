using System.Collections.Generic;

namespace ER
{
    namespace Resources
    {
        public enum ResourceCategory
        {
            FoodRaw = 0,
            FoodProcessed = 1,
            RawMaterial = 2,
            Industrial = 3,
            Currency = 4,
        }

        public class ProductionRecipe
        {
            public string OutputResource {get; private set;}
            public float OutputAmount {get; private set;}

            public string Input1 {get; private set;}
            public float Input1Amount {get; private set;}

            public string Input2 {get; private set;}
            public float Input2Amount {get; private set;}

            public ProductionRecipe(string output, float outAmount, string in1, float in1Amount, string in2 = "", float in2Amount = 0f)
            {
                OutputResource = output;
                OutputAmount = outAmount;

                Input1 = in1;
                Input1Amount = in1Amount;

                Input2 = in2;
                Input2Amount = in2Amount;
            }
        }

        public static class ResourceCatalog
        {
            public static readonly string[] AllResources = new string[]
            {
                "Wood",
                "Stone",
                "Iron",
                "Gold",
                "Coal",
                "Copper",
                "Food",
                "Grain",
                "Leather",
                "Oil",
                "Saltpeter",
                "Gunpowder",
                "Fuel",
                "Electricity",
                "Steel",
                "Meat",
                "Bread",
                "Fruits",
                "Vegetables",
                "Berries",
                "Fish",
                "Money"
            };

            public static readonly string[] FoodResources = new string[]
            {
                "Bread",
                "Food",
                "Meat",
                "Fish",
                "Fruits",
                "Vegetables",
                "Berries",
                "Grain"
            };
            
            public static readonly List<ProductionRecipe> Recipes = new List<ProductionRecipe>()
            {
                new ProductionRecipe("Bread", 10f, "Grain", 6f),
                new ProductionRecipe("Food", 12f, "Meat", 5f, "Vegetables", 4f),
                new ProductionRecipe("Steel", 5f, "Iron", 5f, "Coal", 5f),
                new ProductionRecipe("Gunpowder", 5f, "Saltpeter", 4f, "Coal", 4f),
                new ProductionRecipe("Fuel", 5f, "Oil", 6f)
            };

            public static bool IsFood(string resourceName)
            {
                for (int i = 0; i < FoodResources.Length; i++)
                {
                    if (FoodResources[i] == resourceName) return true;
                }

                return false;
            }

            public static float GetFoodNutritionMultiplier(string resourceName)
            {
                return resourceName switch
                {
                    "Bread" => 1.5f,
                    "Food" => 1.6f,
                    "Meat" => 1.3f,
                    "Fish" => 1.2f,
                    "Fruits" => 1.0f,
                    "Vegetables" => 1.0f,
                    "Berries" => 0.8f,
                    "Grain" => 0.7f,
                    _ => 0f
                };
            }
        }
    }
}