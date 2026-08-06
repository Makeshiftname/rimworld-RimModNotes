using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using HarmonyLib;
using CONN;
using HautsTraits;
using HautsFramework;

namespace Hauts_COaNN
{
    [StaticConstructorOnStartup]
    public static class Hauts_COaNN
    {
        private static readonly Type patchType = typeof(Hauts_COaNN);
        static Hauts_COaNN()
        {
			Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsconncompatibility.main");
            harmony.Patch(AccessTools.Method(typeof(HautsF_COaNN.HautsCOaNNUtility), nameof(HautsF_COaNN.HautsCOaNNUtility.AddRandomTraitExempt)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_AddRandomTraitExemptPostfix)));
			harmony.Patch(AccessTools.Method(typeof(HautsF_COaNN.HautsCOaNNUtility), nameof(HautsF_COaNN.HautsCOaNNUtility.TraitResetExempt)),
							postfix: new HarmonyMethod(patchType, nameof(HVT_AddRandomTraitExemptPostfix)));
		}
		public static void HVT_AddRandomTraitExemptPostfix(ref bool __result, TraitDef t)
        {
			if (TraitSerumWindow.isOtherDisallowedTrait(t))
            {
				__result = true;
            }
		}
    }
}
