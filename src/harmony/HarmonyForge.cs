using AncientTools.BlockEntityBehaviors;
using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AncientTools
{
    // Removed the problematic [HarmonyPatch] class-level attribute.
    // Harmony will look inside this class and run TargetMethod() to hook it.
    public class HarmonyForgeIgnite
    {
        // Explicitly searches the compiled assembly for the method to bypass reflection mismatching
        public static MethodBase TargetMethod()
        {
            return typeof(BlockEntityForge).GetMethod("OnCommonTick", 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        static bool Prefix(BlockEntityForge __instance, float dt, ref bool ___burning, ref double ___lastTickTotalHours, ref float ___fuelLevel)
        {
            try
            {
                BlockEntityBehaviorFireproofFuel fireproofFuelBehavior = __instance.GetBehavior<BlockEntityBehaviorFireproofFuel>();
                
                if (fireproofFuelBehavior != null)
                {
                    if (fireproofFuelBehavior.GetFedFireproofFuel() == true)
                    {
                        if (___burning)
                        {
                            double hoursPassed = __instance.Api.World.Calendar.TotalHours - ___lastTickTotalHours;
                            
                            if (___fuelLevel > 0) 
                                ___fuelLevel = Math.Max(0, ___fuelLevel - (float)(2.5 / 24 * hoursPassed));
                            
                            if (___fuelLevel <= 0)
                            {
                                ___burning = false;
                            }

                            // Access slot 0 (contents slot) from the inventory object safely
                            ItemSlot contentsSlot = __instance.Inventory?[0];

                            if (contentsSlot != null && !contentsSlot.Empty)
                            {
                                ItemStack contents = contentsSlot.Itemstack;
                                float temp = contents.Collectible.GetTemperature(__instance.Api.World, contents);

                                if (temp < 1100)
                                {
                                    float tempGain = (float)(hoursPassed * 1500);
                                    contents.Collectible.SetTemperature(__instance.Api.World, contents, Math.Min(1100, temp + tempGain));
                                }
                            }
                        }
                        ___lastTickTotalHours = __instance.Api.World.Calendar.TotalHours;
                        return false; 
                    }
                }
            }
            catch { }
            return true;
        }
    }

    [HarmonyPatch(typeof(BlockEntityForge), "OnPlayerInteract", MethodType.Normal)]
    public class HarmonyForgeInteract
    {
        static void Prefix(BlockEntityForge __instance, IPlayer byPlayer)
        {
            try
            {
                BlockEntityBehaviorFireproofFuel fireproofFuelBehavior = __instance.GetBehavior<BlockEntityBehaviorFireproofFuel>();
                ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
                
                if (byPlayer.Entity.Controls.Sneak)
                {
                    if (fireproofFuelBehavior != null && slot.Itemstack != null)
                    {
                        CombustibleProperties combustProps = slot.Itemstack.Collectible.CombustibleProps;
                        if (combustProps != null && combustProps.BurnTemperature > 1000)
                        {
                            if (slot.Itemstack.Collectible.Attributes != null && slot.Itemstack.Collectible.Attributes["waterproofFuel"].Exists)
                            {
                                if (slot.Itemstack.Collectible.Attributes["waterproofFuel"].AsBool() == true) 
                                    fireproofFuelBehavior.SetFedFireproofFuel(true);
                            }
                            else
                            {
                                fireproofFuelBehavior.SetFedFireproofFuel(false);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
