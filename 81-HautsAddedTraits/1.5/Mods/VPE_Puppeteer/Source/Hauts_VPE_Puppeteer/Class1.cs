using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HautsFramework;
using HautsTraitsRoyalty;
using VPEPuppeteer;
using RimWorld;
using Verse;
using System.Reflection;
using RimWorld.Planet;

namespace Hauts_VPE_Puppeteer
{
    [StaticConstructorOnStartup]
    public class Hauts_VPEP
    {
        private static readonly Type patchType = typeof(Hauts_VPEP);
        static Hauts_VPEP()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsvpepcompatibility.main");
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HVT_VPEP_GainTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Ability_Puppet), nameof(Ability_Puppet.ValidateTarget)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPEP_ValidateTargetPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Ability_Puppet), nameof(Ability_Puppet.Cast)),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPEP_CastPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Ability_Puppet), nameof(Ability_Puppet.Cast)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPEP_CastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MindJump), nameof(MindJump.TransferMind)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPEP_TransferMindPostfix)));
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HVT_VPEP_GainTraitPostfix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (PsychicAwakeningUtility.IsAwakenedTrait(trait.def) || PsychicAwakeningUtility.IsTranscendentTrait(trait.def))
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(VPEP_DefOf.VPEP_Puppet, false);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
        public static void HVT_VPEP_CastPrefix(out List<Trait> __state, Ability_Puppet __instance, GlobalTargetInfo[] targets)
        {
            Pawn pawn = targets[0].Thing as Pawn;
            if (pawn != null && pawn.story != null)
            {
                __state = new List<Trait>();
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        __state.Add(t);
                    }
                }
            } else {
                __state = null;
            }
        }
        public static void HVT_VPEP_CastPostfix(List<Trait> __state, Ability_Puppet __instance, GlobalTargetInfo[] targets)
        {
            Pawn pawn = targets[0].Thing as Pawn;
            if (pawn != null && pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits.ToList<Trait>())
                {
                    if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        pawn.story.traits.RemoveTrait(t, false);
                    }
                }
                if (__state != null && __state.Count > 0)
                {
                    foreach (Trait t in __state)
                    {
                        pawn.story.traits.GainTrait(t, true);
                    }
                }
            }
        }
        public static void HVT_VPEP_ValidateTargetPostfix(ref bool __result, LocalTargetInfo target, bool showMessages)
        {
            if (target.Pawn != null && target.Pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(target.Pawn))
            {
                if (showMessages)
                {
                    Messages.Message("HVT_WontTargetAwakened".Translate(), MessageTypeDefOf.CautionInput, true);
                }
                __result = false;
            }
        }
        public static void HVT_VPEP_TransferMindPostfix(Pawn puppetToMaster, Pawn masterToPuppet)
        {
            if (puppetToMaster.story != null && masterToPuppet.story != null)
            {
                List<Trait> psychicTraits = new List<Trait>();
                List<Trait> transes = new List<Trait>();
                foreach (Trait t in masterToPuppet.story.traits.allTraits)
                {
                    if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic || PsychicAwakeningUtility.IsAwakenedTrait(t.def))
                    {
                        psychicTraits.Add(t);
                    }
                    else if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                    {
                        transes.Add(t);
                    }
                }
                foreach (Trait t in transes)
                {
                    masterToPuppet.story.traits.RemoveTrait(t);
                }
                foreach (Trait t in psychicTraits)
                {
                    masterToPuppet.story.traits.RemoveTrait(t);
                }
                foreach (Trait t in psychicTraits)
                {
                    puppetToMaster.story.traits.GainTrait(t);
                }
                foreach (Trait t in transes)
                {
                    puppetToMaster.story.traits.GainTrait(t);
                }
            }
        }
    }
}
