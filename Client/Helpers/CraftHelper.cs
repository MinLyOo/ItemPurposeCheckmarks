using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System.Collections.Generic;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on MoreCheckmarks (MIT, TommySoucy) crafting recipe parsing.
    // The server returns the hideout production table. We build two indexes:
    //   - recipeId -> endProductId (for showing what an ingredient crafts into)
    //   - ingredientTemplateId -> list of recipeIds (for flagging crafting ingredients)
    internal static class CraftHelper
    {
        // recipe id -> end product template id
        public static Dictionary<string, string> ProductionEndProductById = [];

        // ingredient template id -> list of recipe ids that use it
        public static Dictionary<MongoID, List<string>> RecipesByIngredient = [];

        // ingredient template id -> end product template id (first matching recipe, for tooltip)
        public static Dictionary<MongoID, MongoID> EndProductByIngredient = [];

        public static void LoadData()
        {
            ProductionEndProductById.Clear();
            RecipesByIngredient.Clear();
            EndProductByIngredient.Clear();

            try
            {
                JObject productionData = JObject.Parse(RequestHandler.GetJson("/item-purpose-checkmarks/productions"));
                JArray? recipes = productionData["recipes"] as JArray;

                if (recipes is null)
                {
                    return;
                }

                for (int i = 0; i < recipes.Count; ++i)
                {
                    string? id = recipes[i]?["_id"]?.ToString();
                    string? endProduct = recipes[i]?["endProduct"]?.ToString();

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(endProduct))
                    {
                        ProductionEndProductById[id] = endProduct;
                    }

                    // Build the ingredient -> recipe reverse index from requiredItems.
                    // requiredItems is an array of { _tpl, count } objects.
                    JArray? requiredItems = recipes[i]?["requiredItems"] as JArray;
                    if (requiredItems is null || string.IsNullOrEmpty(endProduct))
                    {
                        continue;
                    }

                    MongoID endProductId = endProduct!;

                    foreach (JToken req in requiredItems)
                    {
                        string? tpl = req["_tpl"]?.ToString();
                        if (string.IsNullOrEmpty(tpl) || !Utils.IsValidMongoID(tpl))
                        {
                            continue;
                        }

                        MongoID ingredient = tpl!;

                        if (!RecipesByIngredient.TryGetValue(ingredient, out List<string>? recipeIds))
                        {
                            recipeIds = [];
                            RecipesByIngredient.Add(ingredient, recipeIds);
                        }

                        if (!string.IsNullOrEmpty(id))
                        {
                            recipeIds.Add(id!);
                        }

                        // Keep the first end product for the quick tooltip line.
                        if (!EndProductByIngredient.ContainsKey(ingredient))
                        {
                            EndProductByIngredient.Add(ingredient, endProductId);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Plugin.LogSource?.LogError($"Failed to parse production data: {ex.Message}. Craft checkmarks will be unavailable.");
            }
        }

        public static bool HasEndProduct(string recipeId)
        {
            return ProductionEndProductById.ContainsKey(recipeId);
        }

        public static string? GetEndProduct(string recipeId)
        {
            return ProductionEndProductById.TryGetValue(recipeId, out string? product) ? product : null;
        }

        /// <summary>
        /// Returns true if the given item template is used as an ingredient in any hideout recipe.
        /// </summary>
        public static bool IsCraftingIngredient(MongoID itemId)
        {
            return RecipesByIngredient.ContainsKey(itemId);
        }

        /// <summary>
        /// Returns the end product template id this item can be crafted into (first matching recipe),
        /// or null if the item is not a crafting ingredient.
        /// </summary>
        public static MongoID? GetCraftEndProduct(MongoID itemId)
        {
            return EndProductByIngredient.TryGetValue(itemId, out MongoID product) ? product : null;
        }
    }
}
