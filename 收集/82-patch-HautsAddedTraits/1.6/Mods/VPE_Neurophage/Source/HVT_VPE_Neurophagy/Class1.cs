using HarmonyLib;
using HautsTraitsRoyalty;
using System;
using Verse;
using VPE_Neurophage;

namespace HVT_VPE_Neurophagy
{
    [StaticConstructorOnStartup]
    public class HVT_VPE_Neurophagy
    {
        private static readonly Type patchType = typeof(HVT_VPE_Neurophagy);
        static HVT_VPE_Neurophagy()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.HVT_VPEneurophage");
            harmony.Patch(AccessTools.Method(typeof(Ability_PsychicTheft), nameof(Ability_PsychicTheft.Linkophagy)),
                          prefix: new HarmonyMethod(patchType, nameof(HVT_VPEN_LinkophagyPrefix)));
        }
        //while the single-target Neurophage psycasts can just be slapped with the AbilityExtension that forbids targeting woke pawns, AoE neurophagies need a more invasive method to immunize awoken
        public static bool HVT_VPEN_LinkophagyPrefix(Pawn pawn)
        {
            if (pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
            {
                return false;
            }
            return true;
        }
    }
}
