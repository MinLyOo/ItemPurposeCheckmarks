using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using ItemPurposeCheckmarks.Helpers;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ItemPurposeCheckmarks.Patches
{
    // Based on MoreCheckmarks (MIT, TommySoucy) "Take" action coloring.
    // Adapted to SPT 4.1: the loose-loot action menu goes through
    // InteractionContextHelper.GetAvailableInteractionState, which is sealed off.
    // We patch InteractionContextHelper.GetAvailableActions(GamePlayerOwner, LootItem)
    // and tint the "Take" label in the resulting AvailableInteractionState.Actions.
    internal class TakeActionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(InteractionContextHelper),
                nameof(InteractionContextHelper.GetAvailableActions),
                new[] { typeof(GamePlayerOwner), typeof(LootItem) }
            );
        }

        [PatchPostfix]
        static void Postfix(GamePlayerOwner owner, LootItem lootItem, ref AvailableInteractionState __result)
        {
            try
            {
                if (!Settings.ColorizeTakeAction!.Value || __result?.Actions is not List<InteractionAction> actions)
                {
                    return;
                }

                if (lootItem?.Item is not Item item)
                {
                    return;
                }

                Color? color = TryGetCheckmarkColor(item);
                if (color is null)
                {
                    return;
                }

                for (int i = 0; i < actions.Count; ++i)
                {
                    InteractionAction action = actions[i];
                    if (action is not null && action.Name == "Take")
                    {
                        // No <font> tag: that tag surfaces as a literal <FONT='...'> in the
                        // in-raid action menu. A plain color tag is enough to tint the label.
                        action.Name = "<color=#" + ColorUtility.ToHtmlStringRGB(color.Value) + ">Take</color>";
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource?.LogError($"Failed to process available actions for loose item: {ex.Message}");
            }
        }

        private static Color? TryGetCheckmarkColor(Item item)
        {
            // Reconstruct the same status arbitration used by the tooltip so the
            // "Take" label matches the item's quest / purpose checkmark.
            bool neededForActive = false;
            bool hideoutNeeded = false;
            bool hideoutFulfilled = false;
            bool wishlist = false;
            bool barter = false;
            bool craft = false;

            Profile? profile = GetProfile();

            if (profile != null &&
                QuestsHelper.GetActiveQuestsWithItem(profile, item, out Dictionary<MongoID, QuestsHelper.CurrentQuest> activeQuests,
                    out Dictionary<MongoID, QuestsHelper.CurrentQuest> _))
            {
                neededForActive = activeQuests.Count > 0;
            }

            if (Settings.ShowHideout!.Value)
            {
                HideoutHelper.HideoutNeed need = HideoutHelper.GetNeeded(item.TemplateId);
                hideoutNeeded = need.FoundNeeded;
                hideoutFulfilled = need.FoundFulfilled && !need.FoundNeeded;
            }

            if (Settings.ShowWishlist!.Value && profile?.WishlistManager != null)
            {
                wishlist = profile.WishlistManager.IsInWishlist(item.TemplateId, false, out _);
            }

            if (Settings.ShowBarter!.Value)
            {
                barter = BarterHelper.HasAnyBarter(item.TemplateId);
            }

            if (Settings.ShowCraft!.Value)
            {
                craft = CraftHelper.IsCraftingIngredient(item.TemplateId);
            }

            QuestsHelper.ECheckmarkStatus status = QuestsHelper.GetCheckmarkStatus(
                active: neededForActive,
                future: false,
                squad: false,
                fir: item.MarkedAsSpawnedInSession,
                enough: false,
                collector: false,
                hideoutNeeded: hideoutNeeded,
                hideoutFulfilled: hideoutFulfilled,
                wishlist: wishlist,
                barter: barter,
                craft: craft
            );

            if (status == QuestsHelper.ECheckmarkStatus.None)
            {
                return null;
            }

            return QuestsHelper.GetCheckmarkColor(status, item.MarkedAsSpawnedInSession);
        }

        private static Profile? GetProfile()
        {
            try
            {
                return SPT.Reflection.Utils.ClientAppUtils.GetClientApp()?.GetClientBackEndSession()?.Profile;
            }
            catch
            {
                return null;
            }
        }
    }
}
