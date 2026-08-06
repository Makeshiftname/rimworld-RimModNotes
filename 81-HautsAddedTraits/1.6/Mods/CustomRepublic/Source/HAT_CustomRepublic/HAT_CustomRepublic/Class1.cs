using CustomRepublic;
using HarmonyLib;
using HautsTraits;
using RimWorld;
using System;
using Verse;

namespace HAT_CustomRepublic
{
    [StaticConstructorOnStartup]
    public class HAT_CustomRepublic
    {
        private static readonly Type patchType = typeof(HAT_CustomRepublic);
        static HAT_CustomRepublic()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautstraitsroyalty.customrepublic");
            harmony.Patch(AccessTools.Method(typeof(Thought_Situational_RelationsWithEmpire), nameof(Thought_Situational_RelationsWithEmpire.ShouldHateFaction)),
                          postfix: new HarmonyMethod(patchType, nameof(HVT_ShouldHateFactionPostfix)));
        }
        public static void HVT_ShouldHateFactionPostfix(ref bool __result, Faction f)
        {
            if (GameComponent_Republic.Instance != null && GameComponent_Republic.Instance.state.HasFaction(f.def))
            {
                __result = true;
            }
        }
    }
}