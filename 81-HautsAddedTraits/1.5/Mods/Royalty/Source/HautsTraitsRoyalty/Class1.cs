using HarmonyLib;
using HautsFramework;
using HautsTraits;
using MVCF.Comps;
using MVCF.VerbComps;
using RimWorld;
using RimWorld.Planet;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;
using VFECore;
using VFECore.Shields;
using static RimWorld.IdeoFoundation_Deity;
using static RimWorld.PsychicRitualRoleDef;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.GraphicsBuffer;

namespace HautsTraitsRoyalty
{
    [StaticConstructorOnStartup]
    public static class HautsTraitsRoyalty
    {
        private static readonly Type patchType = typeof(HautsTraitsRoyalty);
        static HautsTraitsRoyalty()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautstraitsroyalty.main");
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.ShouldNotGrantTraitStuff)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyalty_ShouldNotGrantTraitStuffPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyGainTraitPrefix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyGainTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.RemoveTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyRemoveTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.CharacterEditorCompat)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyCharEditorCompatibility)));
            harmony.Patch(AccessTools.Method(typeof(MentalState), nameof(MentalState.RecoverFromState)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyRecoverFromStatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsNotify_PawnKilledPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.Notify_PawnDied)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsNotify_PawnDiedPostifx)));
            harmony.Patch(AccessTools.Method(typeof(Hediff_Level), nameof(Hediff_Level.ChangeLevel)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPMastery_ChangeLevelPostfix)));
            harmony.Patch(AccessTools.Method(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(Pawn), typeof(SkillDef) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPMastery_GenerateQualityCreatedByPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPMisery_TryStartMentalStatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(InspirationHandler), nameof(InspirationHandler.TryStartInspiration)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPMisery_TryStartInspirationPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLove_AddDirectRelationPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.ConfigureGrowthLetter)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLove_ConfigureGrowthLetterPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLoss_SetDeadPrefix)));
            harmony.Patch(AccessTools.Method(typeof(PawnBanishUtility), nameof(PawnBanishUtility.Banish)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLoss_BanishPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Hediff_LaborPushing), nameof(Hediff_LaborPushing.PostRemoved)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLife_LaborPushing_PostRemovedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.TryChildGrowthMoment)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPLife_TryChildGrowthMomentPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_PsychicEntropyTracker), nameof(Pawn_PsychicEntropyTracker.Notify_PawnDied)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsLPDeath_PsychicEntropy_Notify_PawnDiedPrefix)));
            if (ModsConfig.AnomalyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(VoidAwakeningUtility), nameof(VoidAwakeningUtility.EmbraceTheVoid)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPAny_EmbraceTheVoidPostfix)));
                harmony.Patch(AccessTools.Method(typeof(VoidAwakeningUtility), nameof(VoidAwakeningUtility.DisruptTheLink)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPAny_DisruptTheLinkPostfix)));
                harmony.Patch(AccessTools.Method(typeof(BloodRainUtility), nameof(BloodRainUtility.ExposedToBloodRain)),
                              prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_ExposedToBloodRainPrefix)));
            }
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_GainTraitPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.AddGene), new[] { typeof(GeneDef), typeof(bool) }),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_AddGenePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.AddGene), new[] { typeof(GeneDef), typeof(bool) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_AddGenePostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.RemoveGene)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_RemoveGenePostfix)));
            harmony.Patch(AccessTools.Method(typeof(MeditationFocusTypeAvailabilityCache), nameof(MeditationFocusTypeAvailabilityCache.PawnCanUse)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_PawnCanUsePostfix)));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_GeneratePawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompGiveThoughtToAllMapPawnsOnDestroy), nameof(CompGiveThoughtToAllMapPawnsOnDestroy.PostDestroy)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTransPostDestroyPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTransDeath_KillPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTransDeath_KillPostfix)));
            /*harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTranscend_SetXenotypePostfix)));*/
            harmony.Patch(AccessTools.Method(typeof(GeneUtility), nameof(GeneUtility.ImplantXenogermItem)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_ImplantXenogermItemPostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_Neuroquake), nameof(CompAbilityEffect_Neuroquake.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Neuroquake_ApplyPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_IngestedPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_IngestedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.TotalPsyfocusRefund)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_TotalPsyfocusRefundPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Psycast_ActivatePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Psycast_ActivatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(GlobalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Psycast_ActivatePostfix2)));
            harmony.Patch(AccessTools.Method(typeof(FleshbeastUtility), nameof(FleshbeastUtility.SpawnFleshbeastFromPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_SpawnFleshbeastFromPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_TryExecutePrefix)));
            harmony.Patch(AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.ChanceFactor)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_ChanceFactorPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AgeInjuryUtility), nameof(AgeInjuryUtility.GenerateRandomOldAgeInjuries)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GenerateRandomOldAgeInjuriesPrefix)));
            harmony.Patch(AccessTools.Method(typeof(SurgeryOutcomeEffectDef), nameof(SurgeryOutcomeEffectDef.GetQuality)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GetQualityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TendUtility), nameof(TendUtility.DoTend)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_DoTendPostfix)));
            harmony.Patch(AccessTools.Method(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(Pawn), typeof(SkillDef) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GenerateQualityCreatedByPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedMeleeDamageAmount), new[] { typeof(Tool), typeof(Pawn), typeof(Thing), typeof(HediffComp_VerbGiver) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_AdjustedMeleeDamageAmountPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Quest), nameof(Quest.End)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Quest_EndPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Plant), nameof(Plant.PlantCollected)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_PlantCollectedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(CompHasGatherableBodyResource), nameof(CompHasGatherableBodyResource.Gathered)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GatheredPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Mineable), nameof(Mineable.DestroyMined)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_DestroyMinedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_MakeRecipeProductsPostfix)));
            harmony.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedRange)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_AdjustedRangePostfix)));
            harmony.Patch(AccessTools.Method(typeof(VFECore.Abilities.Ability), nameof(VFECore.Abilities.Ability.GetRangeForPawn)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GetRangeForPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_TryInteractWithPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.CanApplyPsycastTo)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_CanApplyPsycastToPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSerumWindow), nameof(TraitSerumWindow.isOtherDisallowedTrait)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsIsOtherDisallowedTraitPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Hediff_Psylink), nameof(Hediff_Psylink.PostAdd)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_PostAddPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSerumWindow), nameof(TraitSerumWindow.isBadTraitCombo)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsIsBadTraitComboPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.COaNN_TraitReset_ShouldDoBonusEffect)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsCOaNNIsLatentPsychicPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.COaNN_TraitReset_BonusEffects)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsCOaNNAwakenPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.IsAwakenedPsychic)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsIsAwakenedPsychicPostfix)));
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (t.HasModExtension<SuperPsychicTrait>())
                {
                    if (t.GetModExtension<SuperPsychicTrait>().category == "awakening")
                    {
                        PsychicAwakeningUtility.AddAwakeningTrait(t);
                    } else if (t.GetModExtension<SuperPsychicTrait>().category == "transcendence") {
                        PsychicAwakeningUtility.AddTranscendentTrait(t);
                    } else if (t.GetModExtension<SuperPsychicTrait>().category == "mythic") {
                        PsychicAwakeningUtility.AddMythicTranscendentTrait(t);
                    }
                    HautsUtility.AddExciseTraitExemption(t);
                }
            }
            foreach (GeneDef g in DefDatabase<GeneDef>.AllDefs)
            {
                if (g.HasModExtension<SuperPsychicGene>() && g.GetModExtension<SuperPsychicGene>().category == "awakening")
                {
                    PsychicAwakeningUtility.AddWokeGene(g);
                }
            }
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HautsTraitsRoyalty_ShouldNotGrantTraitStuffPostfix(ref bool __result, Pawn pawn, Trait trait)
        {
            if (PsychicAwakeningUtility.IsAntipsychicTrait(trait.def, trait.Degree) && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
            {
                __result = true;
            }
        }
        public static bool HautsTraitsRoyaltyGainTraitPrefix(TraitSet __instance, Trait trait, bool suppressConflicts)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (PsychicAwakeningUtility.IsAwakenedTrait(trait.def))
            {
                if (PsychicAwakeningUtility.PsychicDeafMutantDeafInteraction(pawn, false))
                {
                    Log.Error("HVT_NoWokeMutants".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    return false;
                }
                if (ModsConfig.BiotechActive && pawn.genes != null)
                {
                    List<Gene> genesToRemove = new List<Gene>();
                    foreach (Gene g in pawn.genes.GenesListForReading)
                    {
                        if (PsychicAwakeningUtility.IsAntipsychicGene(g.def))
                        {
                            genesToRemove.Add(g);
                        }
                    }
                    foreach (Gene g in genesToRemove)
                    {
                        pawn.genes.RemoveGene(g);
                    }
                }
                List<Trait> traitsToRemove = new List<Trait>();
                foreach (Trait t in __instance.allTraits)
                {
                    if (t.def == HVTRoyaltyDefOf.HVT_LocustClone)
                    {
                        PsychicAwakeningUtility.LocustVanish(pawn);
                        return false;
                    }
                    if (PsychicAwakeningUtility.IsAntipsychicTrait(t.def, t.Degree) || t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    __instance.RemoveTrait(t);
                }
            } else if (trait.def == HVTRoyaltyDefOf.HVT_LatentPsychic) {
                if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    return false;
                }
            } else if (PsychicAwakeningUtility.IsTranscendentTrait(trait.def)) {
                if (PsychicAwakeningUtility.PsychicDeafMutantDeafInteraction(pawn, false))
                {
                    Log.Error("HVT_NoTransMutants".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    return false;
                }
                if (!PsychicAwakeningUtility.IsAwakenedPsychic(pawn) && !PsychicAwakeningUtility.CanTranscendAnyways(pawn))
                {
                    Log.Error("HVT_CantGrantTrans".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    PsychicAwakeningUtility.AwakenPsychicTalent(pawn, false, "HVT_WokeningDefault".Translate(), "HVT_WokeningDefaultFantasy".Translate());
                    return false;
                }
            }
            return true;
        }
        public static void HautsTraitsRoyaltyGainTraitPostfix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (trait.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
            {
                bool removeLP = false;
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                    {
                        removeLP = true;
                        break;
                    }
                }
                if (removeLP)
                {
                    pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic));
                }
            } else if (PsychicAwakeningUtility.IsAwakenedTrait(trait.def)) {
                Hediff tracker = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_LatentPsyTerminus, pawn, null);
                pawn.health.AddHediff(tracker, pawn.health.hediffSet.GetBrain(), null, null);
                tracker = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker, pawn, null);
                pawn.health.AddHediff(tracker, pawn.health.hediffSet.GetBrain(), null, null);
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier))
                {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                } else {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                }
                if (trait.def == HVTRoyaltyDefOf.HVT_AwakenedErudite)
                {
                    PsychicAwakeningUtility.GrantEruditeEffects(pawn, 10);
                }
            } else if (PsychicAwakeningUtility.IsTranscendentTrait(trait.def)) {
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff))
                {
                    Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff, pawn, null);
                    pawn.health.AddHediff(hediff, null, null, null);
                }
                if (PawnGenerator.IsBeingGenerated(pawn))
                {
                    if (trait.def == HVTRoyaltyDefOf.HVT_TTraitSphinx)
                    {
                        PsychicAwakeningUtility.GrantEruditeEffects(pawn, 2);
                    }
                    if (trait.def == HVTRoyaltyDefOf.HVT_TTraitThunderbird)
                    {
                        PsychicAwakeningUtility.AwakenPsychicTalent(pawn, false, "", "", true);
                    }
                }
            }
            if (trait.def.HasModExtension<GrantWordPsycast>() && pawn.abilities != null)
            {
                int maxLevel = pawn.GetPsylinkLevel();
                List<AbilityDef> wordcasts = new List<AbilityDef>();
                foreach (AbilityDef ab in DefDatabase<AbilityDef>.AllDefsListForReading)
                {
                    if (ab.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("WordOf") && ab.IsPsycast && pawn.abilities.GetAbility(ab) == null && ab.level <= maxLevel)
                    {
                        wordcasts.Add(ab);
                    }
                }
                if (wordcasts.Count > 0)
                {
                    pawn.abilities.GainAbility(wordcasts.RandomElement());
                }
            }
            if (trait.def.HasModExtension<GrantSkipPsycast>() && pawn.abilities != null)
            {
                int maxLevel = pawn.GetPsylinkLevel();
                List<AbilityDef> wordcasts = new List<AbilityDef>();
                foreach (AbilityDef ab in DefDatabase<AbilityDef>.AllDefsListForReading)
                {
                    if (ab.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("Skip") && ab.IsPsycast && pawn.abilities.GetAbility(ab) == null && ab.level <= maxLevel)
                    {
                        wordcasts.Add(ab);
                    }
                }
                if (wordcasts.Count > 0)
                {
                    pawn.abilities.GainAbility(wordcasts.RandomElement());
                }
            }
        }
        public static void HautsTraitsRoyaltyRemoveTraitPostfix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (trait != null && (trait.def == HVTRoyaltyDefOf.HVT_LocustClone || trait.def == HVTRoyaltyDefOf.HVT_LovebugDoppel)) {
                PsychicAwakeningUtility.LocustVanish(pawn);
            }
        }
        public static void HautsTraitsRoyaltyCharEditorCompatibility(Pawn p)
        {
            if (p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                foreach (Trait t in p.story.traits.allTraits)
                {
                    if (PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                    {
                        p.story.traits.RemoveTrait(p.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic));
                        break;
                    }
                }
            } else {
                if (PsychicAwakeningUtility.IsAwakenedPsychic(p) && !p.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker))
                {
                    Hediff tracker = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker, p, null);
                    p.health.AddHediff(tracker, p.health.hediffSet.GetBrain(), null, null);
                    if (p.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier))
                    {
                        PawnUtility.ChangePsylinkLevel(p, 1, false);
                    } else {
                        PawnUtility.ChangePsylinkLevel(p, 1, false);
                        PawnUtility.ChangePsylinkLevel(p, 1, false);
                    }
                    if (p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_AwakenedErudite))
                    {
                        PsychicAwakeningUtility.GrantEruditeEffects(p, 10);
                    }
                    if (ModsConfig.BiotechActive && p.genes != null)
                    {
                        List<Gene> genesToRemove = new List<Gene>();
                        foreach (Gene g in p.genes.GenesListForReading)
                        {
                            if (PsychicAwakeningUtility.IsAntipsychicGene(g.def))
                            {
                                genesToRemove.Add(g);
                            }
                        }
                        foreach (Gene g in genesToRemove)
                        {
                            p.genes.RemoveGene(g);
                        }
                    }
                    List<Trait> traitsToRemove = new List<Trait>();
                    foreach (Trait t in p.story.traits.allTraits)
                    {
                        if (PsychicAwakeningUtility.IsAntipsychicTrait(t.def, t.Degree))
                        {
                            traitsToRemove.Add(t);
                        }
                    }
                    foreach (Trait t in traitsToRemove)
                    {
                        p.story.traits.RemoveTrait(t);
                    }
                }
                if (PsychicAwakeningUtility.IsTranscendent(p))
                {
                    if (!PsychicAwakeningUtility.IsAwakenedPsychic(p))
                    {
                        Log.Error("HVT_CantGrantTrans".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve());
                        List<Trait> transesToRemove = new List<Trait>();
                        foreach (Trait t in p.story.traits.allTraits)
                        {
                            if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                            {
                                transesToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in transesToRemove)
                        {
                            p.story.traits.RemoveTrait(t);
                        }
                    }
                    if (!p.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff))
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff, p, null);
                        p.health.AddHediff(hediff, null, null, null);
                    }
                    foreach (Trait trait in p.story.traits.allTraits)
                    {
                        if (trait.def.HasModExtension<GrantWordPsycast>() && p.abilities != null)
                        {
                            int maxLevel = p.GetPsylinkLevel();
                            List<AbilityDef> wordcasts = new List<AbilityDef>();
                            foreach (AbilityDef ab in DefDatabase<AbilityDef>.AllDefsListForReading)
                            {
                                if (ab.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("WordOf") && p.abilities.GetAbility(ab) == null && ab.level <= maxLevel)
                                {
                                    wordcasts.Add(ab);
                                }
                            }
                            if (wordcasts.Count > 0)
                            {
                                p.abilities.GainAbility(wordcasts.RandomElement());
                            }
                        }
                    }
                }
            }
        }
        public static void HautsTraitsRoyaltyRecoverFromStatePostfix(MentalState __instance)
        {
            if (__instance.pawn.story != null)
            {
                if (__instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRaven) && __instance.def != MentalStateDefOf.PanicFlee && __instance.def != MentalStateDefOf.SocialFighting && !__instance.causedByPsycast)
                {
                    for (int i = 0; i < Math.Max(1f,(int)Math.Floor(__instance.pawn.GetStatValue(StatDefOf.PsychicSensitivity))); i++)
                    {
                        PsychicAwakeningUtility.MakeGoodEvent(__instance.pawn);
                    }
                }
            }
        }
        public static void HautsTraitsNotify_PawnKilledPostfix(Pawn killed, Pawn killer)
        {
            if (killer.story != null)
            {
                if (killed.RaceProps.Humanlike && killer.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitShrike) && Rand.Chance(Math.Max(0.01f * killer.GetStatValue(StatDefOf.PsychicSensitivity),0.15f)))
                {
                    PsychicAwakeningUtility.MakeGoodEvent(killer);
                }
            }
        }
        public static void HautsTraitsNotify_PawnDiedPostifx(HediffSet __instance, DamageInfo? dinfo)
        {
            Thing instigator = dinfo.GetValueOrDefault().Instigator;
            if (instigator != null && instigator is Pawn p && p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDulotic) && __instance.pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
            {
                if (!__instance.pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_DulosisTimedLife) && __instance.pawn.PositionHeld != null)
                {
                    if (p.Faction != null && (__instance.pawn.Faction == null || __instance.pawn.Faction != p.Faction))
                    {
                        __instance.pawn.SetFaction(p.Faction, p);
                    }
                    if (ModsConfig.IdeologyActive && p.Ideo != null && __instance.pawn.Ideo != null && __instance.pawn.Ideo != p.Ideo)
                    {
                        __instance.pawn.ideo.SetIdeo(p.Ideo);
                    }
                    HVTRoyaltyDefOf.HVT_Zomburst.SpawnMaintained(__instance.pawn.PositionHeld, __instance.pawn.MapHeld, 1f);
                    HautsUtility.StartDelayedResurrection(__instance.pawn, new IntRange(1, 1), "", false, false, true, HVTRoyaltyDefOf.HVT_DulosisTimedLife, 1f);
                }
            }
        }
        public static void HautsTraitsLPMastery_ChangeLevelPostfix(Hediff_Level __instance, int levelOffset)
        {
            if ((__instance.def == HediffDefOf.PsychicAmplifier || __instance.def == DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant")))
            {
                Pawn pawn = __instance.pawn;
                if (levelOffset > 0)
                {
                    int hediffCount = pawn.health.hediffSet.hediffs.Count;
                    for (int i = hediffCount - 1; i >= 0; i--)
                    {
                        if (pawn.health.hediffSet.hediffs[i] is Hediff_Censure hc)
                        {
                            pawn.health.RemoveHediff(hc);
                        }
                    }
                    if (pawn.story != null)
                    {
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                        {
                            if (__instance.level == pawn.GetMaxPsylinkLevel())
                            {
                                PsychicAwakeningUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeMaxPsyLevel".Translate(), "HVT_WokeMaxPsyLevelFantasy".Translate(), false);
                            }
                        } else if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn) && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_LatentPsyTerminus) && Rand.Value <= 0.01f) {
                            PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_TransHighPsyLevel".Translate(), "HVT_TransHighPsyLevelFantasy".Translate(), 0.01f);
                        }
                    }
                }
            }
        }
        public static void HautsTraitsLPMastery_GenerateQualityCreatedByPawnPostfix(ref QualityCategory __result, Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (__result == QualityCategory.Legendary)
                {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 1, true, "HVT_WokeLegendaryWork".Translate(), "HVT_WokeLegendaryWorkFantasy".Translate());
                }
            }
        }
        public static void HautsTraitsLPMisery_TryStartMentalStatePostfix(MentalStateHandler __instance, MentalStateDef stateDef, bool causedByMood, bool causedByPsycast)
        {
            Pawn pawn = GetInstanceField(typeof(MentalStateHandler), __instance, "pawn") as Pawn;
            if (!causedByPsycast && pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && (stateDef.IsExtreme && Rand.Value <= 0.5f) || Rand.Value <= 0.02f)
                {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 2, true, "HVT_WokeMentalBreak".Translate(), "HVT_WokeMentalBreakFantasy".Translate());
                    return;
                }
                if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn) && causedByMood && Rand.Value <= 0.04f) {
                    if (pawn.MapHeld != null)
                    {
                        if (pawn.MapHeld.gameConditionManager.GetHighestPsychicDroneLevelFor(pawn.gender) >= PsychicDroneLevel.BadLow)
                        {
                            PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_TransBreak".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_TransBreakFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), 0.2f, false);
                        }
                    } else if (pawn.IsCaravanMember()) {
                        PsychicDroneLevel psychicDroneLevel = PsychicDroneLevel.None;
                        foreach (Site site in Find.World.worldObjects.Sites)
                        {
                            foreach (SitePart sitePart in site.parts)
                            {
                                if (!sitePart.conditionCauser.DestroyedOrNull() && sitePart.def.Worker is SitePartWorker_ConditionCauser_PsychicDroner)
                                {
                                    CompCauseGameCondition_PsychicEmanation compCauseGameCondition_PsychicEmanation = sitePart.conditionCauser.TryGetComp<CompCauseGameCondition_PsychicEmanation>();
                                    if (compCauseGameCondition_PsychicEmanation.ConditionDef.conditionClass == typeof(GameCondition_PsychicEmanation) && compCauseGameCondition_PsychicEmanation.InAoE(pawn.GetCaravan().Tile) && compCauseGameCondition_PsychicEmanation.gender == pawn.gender && compCauseGameCondition_PsychicEmanation.Level > psychicDroneLevel)
                                    {
                                        psychicDroneLevel = compCauseGameCondition_PsychicEmanation.Level;
                                    }
                                }
                            }
                        }
                        foreach (Map map in Find.Maps)
                        {
                            foreach (GameCondition gameCondition in map.gameConditionManager.ActiveConditions)
                            {
                                CompCauseGameCondition_PsychicEmanation compCauseGameCondition_PsychicEmanation2 = gameCondition.conditionCauser.TryGetComp<CompCauseGameCondition_PsychicEmanation>();
                                if (compCauseGameCondition_PsychicEmanation2 != null && compCauseGameCondition_PsychicEmanation2.InAoE(pawn.GetCaravan().Tile) && compCauseGameCondition_PsychicEmanation2.gender == pawn.gender && compCauseGameCondition_PsychicEmanation2.Level > psychicDroneLevel)
                                {
                                    psychicDroneLevel = compCauseGameCondition_PsychicEmanation2.Level;
                                }
                            }
                        }
                        if (psychicDroneLevel >= PsychicDroneLevel.BadLow)
                        {
                            PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_TransBreak".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_TransBreakFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), 0.2f, false);
                        }
                    }
                }
                if (pawn.Map != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitNoctule))
                {
                    IncidentParms parms = new IncidentParms
                    {
                        target = pawn.Map
                    };
                    RimWorld.IncidentDefOf.Eclipse.Worker.TryExecute(parms);
                }
            }
        }
        public static void HautsTraitsLPMisery_TryStartInspirationPostfix(ref bool __result, InspirationHandler __instance)
        {
            if (__result && __instance.pawn.story != null)
            {
                if (__instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && __instance.pawn.needs != null && __instance.pawn.needs.mood != null && __instance.pawn.needs.mood.thoughts != null && __instance.pawn.needs.mood.thoughts.memories != null)
                {
                    for (int i = 0; i < __instance.pawn.needs.mood.thoughts.memories.Memories.Count; i++)
                    {
                        if (__instance.pawn.needs.mood.thoughts.memories.Memories[i].def == ThoughtDefOf.Catharsis)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(__instance.pawn, 2, true, "HVT_WokeCatharsisInspiration".Translate(), "HVT_WokeCatharsisInspirationFantasy".Translate());
                            break;
                        }
                    }
                }
                if (__instance.pawn.Map != null && __instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitNoctule))
                {
                    IncidentParms parms = new IncidentParms
                    {
                        target = __instance.pawn.Map
                    };
                    RimWorld.IncidentDefOf.Eclipse.Worker.TryExecute(parms);
                }
            }
        }
        public static void HautsTraitsLPLove_AddDirectRelationPostfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_RelationsTracker), __instance, "pawn") as Pawn;
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                PsychicAwakeningUtility.LPLoveCheckRelations(def,pawn,otherPawn);
            }
        }
        public static void HautsTraitsLPLove_ConfigureGrowthLetterPostfix(ChoiceLetter_GrowthMoment __instance, Pawn pawn)
        {
            if (ModsConfig.BiotechActive && __instance.def == LetterDefOf.ChildToAdult)
            {
                if (pawn.Map != null)
                {
                    List<Pawn> recipients = pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(pawn.Faction);
                    for (int i = 0; i < recipients.Count; i++)
                    {
                        Pawn recipient = recipients[i];
                        if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && recipient.relations.OpinionOf(pawn) >= 60)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 3, true, "HVT_WokeChildGrewUp".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")), "HVT_WokeChildGrewUpFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")));
                        }
                    }
                } else if (pawn.IsCaravanMember()) {
                    Caravan caravan = pawn.GetCaravan();
                    for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
                    {
                        Pawn recipient = caravan.PawnsListForReading[i];
                        if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && recipient.relations.OpinionOf(pawn) >= 60)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 3, true, "HVT_WokeChildGrewUp".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")), "HVT_WokeChildGrewUpFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")));
                        }
                    }
                }
            }
        }
        public static void HautsTraitsLPLoss_SetDeadPrefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_HealthTracker), __instance, "pawn") as Pawn;
            if (pawn.relations != null && pawn.relations.DirectRelations != null)
            {
                for (int i = 0; i < pawn.relations.DirectRelations.Count; i++)
                {
                    Pawn recipient = pawn.relations.DirectRelations[i].otherPawn;
                    if (recipient != pawn && recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                    {
                        if ((Rand.Value <= 0.5f && (!pawn.RaceProps.Humanlike || recipient.relations.OpinionOf(pawn) >= 60 || recipient.relations.OpinionOf(pawn) <= -60)) || recipient.relations.OpinionOf(pawn) > 100)
                        {
                            string triggerEvent,triggerEventFantasy;
                            if (Current.ProgramState == ProgramState.Playing)
                            {
                                if (pawn.Name != null && pawn.Name.ToStringFull != null)
                                {
                                    triggerEvent = "HVT_WokeNamedDeath".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                                    triggerEventFantasy = "HVT_WokeNamedDeathFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                                } else {
                                    triggerEvent = "HVT_WokeNamelessDeath".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                                    triggerEventFantasy = "HVT_WokeNamelessDeathFantasy".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                                }
                                PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 4, true, triggerEvent, triggerEventFantasy, false, 0.5f, true);
                            }
                        }
                    }
                }
            }
            if (pawn.Map != null)
            {
                List<Pawn> recipients = pawn.Map.mapPawns.AllHumanlikeSpawned;
                for (int i = 0; i < recipients.Count; i++)
                {
                    Pawn recipient = recipients[i];
                    if (recipient != pawn && recipient.story != null)
                    {
                        if (Current.ProgramState == ProgramState.Playing && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && (pawn.RaceProps.Humanlike ? (recipient.relations.OpinionOf(pawn) >= 100 || (recipient.relations.OpinionOf(pawn) >= 60 && Rand.Chance(0.5f))) : (Rand.Chance(0.5f) && recipient.relations.DirectRelationExists(PawnRelationDefOf.Bond,pawn))))
                        {
                            string triggerEvent, triggerEventFantasy;
                            if (pawn.Name != null && pawn.Name.ToStringFull != null)
                            {
                                triggerEvent = "HVT_WokeNamedDeath".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                                triggerEventFantasy = "HVT_WokeNamedDeathFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                            } else {
                                triggerEvent = "HVT_WokeNamelessDeath".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                                triggerEventFantasy = "HVT_WokeNamelessDeathFantasy".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                            }
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 4, true, triggerEvent, triggerEventFantasy, false, 0.5f, true);
                        }
                        if (recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon))
                        {
                            Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_DragonsHoard, recipient);
                            float victimsPsylinks = 66.67f;
                            float psyEnergy = 1f;
                            if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                            {
                                Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                                if (psylink != null)
                                {
                                    victimsPsylinks *= psylink.level;
                                }
                            } else {
                                victimsPsylinks *= pawn.GetPsylinkLevel();
                            }
                            if (pawn.story != null)
                            {
                                for (int j = 0; j < pawn.story.traits.allTraits.Count; j++)
                                {
                                    if (PsychicAwakeningUtility.IsAwakenedTrait(pawn.story.traits.allTraits[j].def))
                                    {
                                        psyEnergy += 1f;
                                    }
                                    if (PsychicAwakeningUtility.IsTranscendentTrait(pawn.story.traits.allTraits[j].def))
                                    {
                                        psyEnergy += 3f;
                                    }
                                }
                                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                                {
                                    psyEnergy += 0.5f;
                                }
                            }
                            if (ModsConfig.BiotechActive && pawn.genes != null)
                            {
                                for (int j = 0; j < pawn.genes.GenesListForReading.Count; j++)
                                {
                                    if (PsychicAwakeningUtility.IsAwakenedPsychicGene(pawn.genes.GenesListForReading[j].def))
                                    {
                                        psyEnergy += 1f;
                                    }
                                }
                            }
                            hediff.Severity += victimsPsylinks * psyEnergy*10f;
                            if (hediff.Severity > 0f)
                            {
                                recipient.health.AddHediff(hediff, recipient.health.hediffSet.GetBrain());
                            }
                        }
                    }
                }
            } else if (pawn.IsCaravanMember()) {
                Caravan caravan = pawn.GetCaravan();
                for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
                {
                    Pawn recipient = caravan.PawnsListForReading[i];
                    if (recipient != pawn && recipient.RaceProps.Humanlike && recipient.story != null)
                    {
                        if (Current.ProgramState == ProgramState.Playing && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && (pawn.RaceProps.Humanlike ? (recipient.relations.OpinionOf(pawn) >= 100 || (recipient.relations.OpinionOf(pawn) >= 60 && Rand.Chance(0.5f))) : Rand.Chance(0.5f)))
                        {
                            string triggerEvent, triggerEventFantasy;
                            if (pawn.Name != null && pawn.Name.ToStringFull != null)
                            {
                                triggerEvent = "HVT_WokeNamedDeath".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                                triggerEventFantasy = "HVT_WokeNamedDeathFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve();
                            } else {
                                triggerEvent = "HVT_WokeNamelessDeath".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                                triggerEventFantasy = "HVT_WokeNamelessDeathFantasy".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                            }
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 4, true, triggerEvent,triggerEventFantasy,false,0.5f,true);
                        }
                        if (recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon))
                        {
                            Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_DragonsHoard, recipient);
                            if (pawn.story != null)
                            {
                                for (int j = 0; j < pawn.story.traits.allTraits.Count; j++)
                                {
                                    if (PsychicAwakeningUtility.IsAwakenedTrait(pawn.story.traits.allTraits[j].def))
                                    {
                                        hediff.Severity += 66.67f;
                                    }
                                }
                                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                                {
                                    hediff.Severity += 33.33f;
                                }
                            }
                            if (ModsConfig.BiotechActive && pawn.genes != null)
                            {
                                for (int j = 0; j < pawn.genes.GenesListForReading.Count; j++)
                                {
                                    if (PsychicAwakeningUtility.IsAwakenedPsychicGene(pawn.genes.GenesListForReading[j].def))
                                    {
                                        hediff.Severity += 53.33f;
                                    }
                                }
                            }
                            int victimsPsylinks = 0;
                            if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                            {
                                Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                                if (psylink != null)
                                {
                                    victimsPsylinks = psylink.level;
                                }
                            } else {
                                victimsPsylinks = pawn.GetPsylinkLevel();
                            }
                            hediff.Severity += victimsPsylinks * 33.33f;
                            if (hediff.Severity > 0f)
                            {
                                recipient.health.AddHediff(hediff, recipient.health.hediffSet.GetBrain());
                            }
                        }
                    }
                }
            }
        }
        public static void HautsTraitsLPLoss_BanishPostfix(Pawn pawn)
        {
            if (ModsConfig.RoyaltyActive)
            {
                for (int i = 0; i < pawn.relations.DirectRelations.Count; i++)
                {
                    Pawn recipient = pawn.relations.DirectRelations[i].otherPawn;
                    if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                    {
                        if (Rand.Value <= 0.02f && recipient.relations.OpinionOf(pawn) >= 60)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(recipient, 4, true, "HVT_WokeExile".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve(), "HVT_WokeExileFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve());
                        }
                    }
                }
            }
        }
        public static void HautsTraitsLPLife_LaborPushing_PostRemovedPostfix(Hediff_LaborPushing __instance)
        {
            if (Rand.Value <= 0.33f && !__instance.pawn.Dead && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                PsychicAwakeningUtility.AwakenPsychicTalentCheck(__instance.pawn, 5, true, "HVT_WokeBirth".Translate().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve(), "HVT_WokeBirthFantasy".Translate().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve());
            }
        }
        public static void HautsTraitsLPLife_TryChildGrowthMomentPostfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_AgeTracker), __instance, "pawn") as Pawn;
            if (Rand.Value <= 0.01f && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 5, true, "HVT_WokeBirthday".Translate(pawn.ageTracker.AgeBiologicalYears, pawn.Name.ToStringShort, pawn.gender.GetPossessive()).Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeBirthdayFantasy".Translate(pawn.ageTracker.AgeBiologicalYears, pawn.Name.ToStringShort, pawn.gender.GetPossessive()).Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
        }
        public static bool HautsTraitsLPDeath_PsychicEntropy_Notify_PawnDiedPrefix(Pawn_PsychicEntropyTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_PsychicEntropyTracker), __instance, "pawn") as Pawn;
            if (pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakeningAfterglow) || pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker))
            {
                __instance.RechargePsyfocus();
                __instance.RemoveAllEntropy();
                return false;
            }
            return true;
        }
        public static void HautsTraitsLPAny_EmbraceTheVoidPostfix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                {
                    PsychicAwakeningUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeVoidEmbrace".Translate(), "HVT_WokeVoidEmbraceFantasy".Translate());
                }
                if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_WokeVoidEmbrace2".Translate(), "HVT_WokeVoidEmbraceFantasy2".Translate(), 1f);
                }
            }
        }
        public static void HautsTraitsLPAny_DisruptTheLinkPostfix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                {
                    PsychicAwakeningUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeVoidClosure".Translate(), "HVT_WokeVoidClosureFantasy".Translate());
                }
                if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_WokeVoidClosure2".Translate(), "HVT_WokeVoidClosureFantasy2".Translate(), 1f);
                }
            }
        }
        public static bool HautsTraitsAA_GainTraitPrefix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn) && PsychicAwakeningUtility.IsAntipsychicTrait(trait.def, trait.Degree))
            {
                return false;
            }
            return true;
        }
        public static bool HautsTraitsAA_AddGenePrefix(Pawn_GeneTracker __instance, GeneDef geneDef)
        {
            if (__instance.pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(__instance.pawn) && PsychicAwakeningUtility.IsAntipsychicGene(geneDef))
            {
                return false;
            }
            return true;
        }
        public static void HautsTraitsAA_AddGenePostfix(Pawn_GeneTracker __instance, GeneDef geneDef)
        {
            Pawn pawn = __instance.pawn;
            if (PsychicAwakeningUtility.IsAwakenedPsychicGene(geneDef))
            {
                List<Gene> genesToRemove = new List<Gene>();
                if (pawn.story != null)
                {
                    List<Trait> traitsToRemove = new List<Trait>();
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (PsychicAwakeningUtility.IsAntipsychicTrait(t.def, t.Degree))
                        {
                            traitsToRemove.Add(t);
                        }
                    }
                    foreach (Trait t in traitsToRemove)
                    {
                        pawn.story.traits.RemoveTrait(t);
                    }
                }
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier))
                {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                } else {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                }
                if (geneDef == HVTRoyaltyDefOf.HVT_AEruditeGene)
                {
                    PsychicAwakeningUtility.GrantEruditeEffects(pawn, 10);
                }
                foreach (Gene g in pawn.genes.GenesListForReading)
                {
                    if (PsychicAwakeningUtility.IsAntipsychicGene(g.def))
                    {
                        genesToRemove.Add(g);
                    }
                }
                foreach (Gene g in genesToRemove)
                {
                    pawn.genes.RemoveGene(g);
                }
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker))
                {
                    Hediff tracker = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker,pawn);
                    pawn.health.AddHediff(tracker, pawn.health.hediffSet.GetBrain(), null, null);
                }
            }
        }
        public static void HautsTraitsAA_RemoveGenePostfix(Pawn_GeneTracker __instance, Gene gene)
        {
            GeneDef geneDef = gene.def;
            if (geneDef == HVTRoyaltyDefOf.HVT_AEruditeGene)
            {
                PawnUtility.ChangePsylinkLevel(__instance.pawn, -1, false);
                PawnUtility.ChangePsylinkLevel(__instance.pawn, -1, false);
            }
        }
        public static void HautsTraitsAA_PawnCanUsePostfix(ref bool __result, Pawn p, MeditationFocusDef type)
        {
            if (p.story != null)
            {
                if (p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_AwakenedChanshi) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_ChanshiGene))
                {
                    __result = true;
                    return;
                } else {
                    bool hasAnyMFD = false;
                    foreach (Trait t in p.story.traits.allTraits)
                    {
                        if (t.CurrentData.allowedMeditationFocusTypes != null && t.CurrentData.allowedMeditationFocusTypes.Count > 0)
                        {
                            hasAnyMFD = true;
                            if (t.CurrentData.allowedMeditationFocusTypes.Contains(type))
                            {
                                __result = true;
                                return;
                            }
                        }
                    }
                    if (p.story.Childhood.spawnCategories.Contains("Tribal") || p.story.Childhood.spawnCategories.Contains("ChildTribal"))
                    {
                        return;
                    }
                    if (!hasAnyMFD && type == DefDatabase<MeditationFocusDef>.GetNamed("Artistic"))
                    {
                        __result = true;
                    }
                }
            }
        }
        public static void HautsTraitsAA_GeneratePawnPostfix(ref Pawn __result, PawnGenerationRequest request)
        {
            if (__result.story != null && __result.Faction != null && !__result.Dead && __result.DevelopmentalStage.Adult())
            {
                float awakenChance = 0f, transcendChance = 0.025f;
                if (!PsychicAwakeningUtility.IsAwakenedPsychic(__result))
                {
                    if (__result.royalty != null && __result.royalty.AllTitlesForReading.Count > 0)
                    {
                        foreach (RoyalTitle rt in __result.royalty.AllTitlesForReading)
                        {
                            if (rt.def.maxPsylinkLevel > 0 && (rt.def.seniority / 1000f) > awakenChance)
                            {
                                awakenChance = rt.def.seniority / 1000f;
                            }
                        }
                    }
                    if (__result.Faction.leader == __result)
                    {
                        awakenChance += 0.1f;
                    }
                    if (Rand.Value <= awakenChance)
                    {
                        PsychicAwakeningUtility.AwakenPsychicTalent(__result, false, "", "", true);
                    }
                    transcendChance = Math.Max(awakenChance - 0.6f, 0.025f);
                }
                if (PsychicAwakeningUtility.IsAwakenedPsychic(__result) && !PsychicAwakeningUtility.IsTranscendent(__result))
                {
                    if (Rand.Value <= transcendChance)
                    {
                        PsychicAwakeningUtility.AchieveTranscendence(__result, "", "", 0f, true);
                    }
                }
                if (request.Context != PawnGenerationContext.PlayerStarter && !__result.IsMutant)
                {
                    Trait t = __result.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic);
                    if (t != null)
                    {
                        RemovedOnAwakening roa = t.def.GetModExtension<RemovedOnAwakening>();
                        if (roa != null && Rand.Chance(roa.awakenChance))
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalent(__result,false,"","",true);
                        }
                    }
                }
            }
        }
        public static void HautsTraitsTransPostDestroyPostfix(CompGiveThoughtToAllMapPawnsOnDestroy __instance, Map previousMap)
        {
            if (previousMap != null)
            {
                CompProperties_GiveThoughtToAllMapPawnsOnDestroy props = (CompProperties_GiveThoughtToAllMapPawnsOnDestroy)__instance.props;
                if (props.thought == DefDatabase<ThoughtDef>.GetNamed("AnimaScream"))
                {
                    foreach (Pawn p in previousMap.mapPawns.AllPawnsSpawned)
                    {
                        if (Rand.Value <= 0.04f && p.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(p))
                        {
                            PsychicAwakeningUtility.AchieveTranscendence(p, "HVT_TransAnimaScream".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVT_TransAnimaScreamFantasy".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), 0.1f);
                        }
                    }
                }
            }
        }
        public static bool HautsTraitsTransDeath_KillPrefix(Pawn __instance)
        {
            if (__instance.story != null)
            {
                if (__instance.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LocustClone) || __instance.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LovebugDoppel))
                {
                    PsychicAwakeningUtility.LocustVanish(__instance);
                    return false;
                }
                Hediff wraith = __instance.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
                if (wraith != null && wraith.Severity >= 24f)
                {
                    PsychicAwakeningUtility.WraithTransfer(__instance);
                }
            }
            return true;
        }
        public static void HautsTraitsTransDeath_KillPostfix(Pawn __instance)
        {
            if (__instance.Corpse != null)
            {
                if (__instance.Corpse.InnerPawn.abilities != null)
                {
                    Pawn pawn = __instance.Corpse.InnerPawn;
                    Ability ability = pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_PhoenixAbility);
                    if (ability != null && ability.CooldownTicksRemaining <= 0 && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_PhoenixPostResurrection))
                    {
                        LocalTargetInfo self = new LocalTargetInfo(__instance.Corpse);
                        ability.Activate(self, self);
                        ability.StartCooldown(ability.def.cooldownTicksRange.max);
                    }
                }
            }
        }
        /*public static void HautsTraitsTranscend_SetXenotypePostfix(Pawn_GeneTracker __instance, XenotypeDef xenotype)
        {
            if (PsychicAwakeningUtility.IsAwakenedPsychic(__instance.pawn))
            {
                PsychicAwakeningUtility.InduceArchiteTranscendenceDelay(__instance.pawn, xenotype.AllGenes);
            }
        }*/
        public static void HautsTraitsTrans_ImplantXenogermItemPostfix(Pawn pawn, Xenogerm xenogerm)
        {
            if (xenogerm.GeneSet != null && pawn.genes != null)
            {
                PsychicAwakeningUtility.InduceArchiteTranscendenceDelay(pawn, xenogerm.GeneSet.GenesListForReading);
            }
        }
        public static void HautsTraitsTrans_Neuroquake_ApplyPostfix(CompAbilityEffect_Neuroquake __instance)
        {
            foreach (Pawn pawn in __instance.parent.pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!pawn.Dead && !pawn.Suspended && pawn.GetStatValue(StatDefOf.PsychicSensitivity, true, -1) > float.Epsilon && !pawn.Fogged() && pawn.Spawned && pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    if (Rand.Value <= 0.3f && !pawn.Position.InHorDistOf(__instance.parent.pawn.Position, __instance.parent.def.EffectRadius) && pawn.Position.InHorDistOf(__instance.parent.pawn.Position, __instance.Props.mentalStateRadius))
                    {
                        PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_TransNeuroquake".Translate(), "HVT_TransNeuroquakeFantasy".Translate(), 0.01f);
                    }
                }
            }
        }
        public static void HautsTraitsTrans_IngestedPrefix(Thing __instance, Pawn ingester)
        {
            if (__instance is Corpse corpse && corpse.InnerPawn != null && ingester.story != null)
            {
                Pawn pawn = corpse.InnerPawn;
                if (ingester.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_HarpysHunger))
                {
                    List<TraitDef> couldGrant = new List<TraitDef>();
                    if (ingester.needs != null && ingester.needs.food != null && pawn.HasPsylink)
                    {
                        ingester.needs.food.CurLevelPercentage = 1f;
                        if (ingester.HasPsylink && pawn.GetPsylinkLevel() > ingester.GetPsylinkLevel())
                        {
                            ingester.GetMainPsylinkSource().ChangeLevel(1, true);
                        }
                    }
                    if (pawn.story != null)
                    {
                        foreach (Trait t in pawn.story.traits.allTraits)
                        {
                            if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                            {
                                couldGrant.Add(t.def);
                            } else if (PsychicAwakeningUtility.IsAwakenedTrait(t.def)) {
                                ingester.story.traits.GainTrait(new Trait(t.def, t.Degree));
                            }
                        }
                    }
                    if (couldGrant.Count > 0)
                    {
                        ingester.story.traits.GainTrait(new Trait(couldGrant.RandomElement()));
                    }
                    if (ModsConfig.BiotechActive && pawn.genes != null && ingester.genes != null)
                    {
                        foreach (Gene g in pawn.genes.GenesListForReading)
                        {
                            if (g.def.biostatArc > 0)
                            {
                                ingester.genes.AddGene(g.def,true);
                            }
                        }
                    }
                    return;
                }
                if (ingester.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitHarbinger))
                {
                    if (pawn.Faction != null)
                    {
                        HautsUtility.LearnLanguage(ingester,pawn, 0.4f * (pawn.GetStatValue(StatDefOf.PsychicSensitivity) + ingester.GetStatValue(StatDefOf.PsychicSensitivity)));
                    }
                    if (pawn.skills != null && ingester.skills != null)
                    {
                        SkillDef sd = (from sdef in DefDatabase<SkillDef>.AllDefsListForReading
                                             where !ingester.skills.GetSkill(sdef).TotallyDisabled && !pawn.skills.GetSkill(sdef).TotallyDisabled
                                       select sdef).RandomElementByWeight((SkillDef sde) => pawn.skills.GetSkill(sde).Level);
                        if (sd != null)
                        {
                            ingester.skills.Learn(sd,500f*(pawn.GetStatValue(StatDefOf.PsychicSensitivity)+ingester.GetStatValue(StatDefOf.PsychicSensitivity))*pawn.skills.GetSkill(sd).Level,true);
                        }
                    }
                    float bodySize = pawn.health.hediffSet.GetCoverageOfNotMissingNaturalParts(pawn.RaceProps.body.corePart)*pawn.BodySize;
                    while (bodySize > 0f)
                    {
                        if (Rand.Chance(bodySize/2f))
                        {
                            HealthUtility.FixWorstHealthCondition(ingester);
                        }
                        bodySize -= 2f;
                    }
                }
            }
        }
        public static void HautsTraitsTrans_IngestedPostfix(float __result, Thing __instance, Pawn ingester)
        {
            if (!__instance.Destroyed)
            {
                if (ingester.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_HarpysHunger))
                {
                    __instance.Destroy();
                } else if (ingester.story != null && ingester.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitHarbinger))
                {
                    __instance.TakeDamage(new DamageInfo(DamageDefOf.Deterioration,25f));
                }
            }
        }
        public static void HautsTraitsTrans_TotalPsyfocusRefundPostfix(ref float __result, Pawn pawn, float psyfocusCost, bool isWord, bool isSkip)
        {
            if (isWord && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitNightingale))
            {
                __result = Math.Max(__result,Math.Max(0f,psyfocusCost - 0.1f));
            }
        }
        public static void HautsTraitsTrans_Psycast_ActivatePrefix(Psycast __instance, LocalTargetInfo target, LocalTargetInfo dest, out List<Thing> __state)
        {
            Pawn pawn = __instance.pawn;
            __state = new List<Thing>();
            if (target != null && pawn.story != null && target.Cell != null && target.Cell.InBounds(pawn.Map))
            {
                if (__instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("Skip") && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitGlowworm))
                {
                    List<IntVec3> iv3s = new List<IntVec3>();
                    if (dest.Cell != null && dest.Cell.InBounds(pawn.Map))
                    {
                        if (target.Cell != dest.Cell)
                        {
                            foreach (IntVec3 bres in GenSight.BresenhamCellsBetween(target.Cell, dest.Cell))
                            {
                                foreach (IntVec3 bres3 in GenRadial.RadialCellsAround(bres, 1.42f, true))
                                {
                                    if (!iv3s.Contains(bres3) && bres3.InBounds(pawn.Map))
                                    {
                                        iv3s.Add(bres3);
                                    }
                                }
                            }
                        } else {
                            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(target.Cell, Math.Min(6f,pawn.GetPsylinkLevel()), true))
                            {
                                if (!iv3s.Contains(iv3) && iv3.InBounds(pawn.Map))
                                {
                                    iv3s.Add(iv3);
                                }
                            }
                        }
                    } else {
                        foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(target.Cell, 1.42f, true))
                        {
                            if (!iv3s.Contains(iv3) && iv3.InBounds(pawn.Map))
                            {
                                iv3s.Add(iv3);
                            }
                        }
                    }
                    List<Thing> things = new List<Thing>();
                    foreach (IntVec3 toHit in iv3s)
                    {
                        foreach (Thing thing in toHit.GetThingList(pawn.Map))
                        {
                            things.Add(thing);
                        }
                    }
                    __state = things;
                }
            }
        }
        public static void HautsTraitsTrans_Psycast_ActivatePostfix(Psycast __instance, LocalTargetInfo target, LocalTargetInfo dest, List<Thing> __state)
        {
            PsychicAwakeningUtility.PsycastActivationRiderEffects(__instance);
            Pawn pawn = __instance.pawn;
            for (int i = __state.Count - 1; i >= 0; i--)
            {
                Thing t = __state[i];
                if ((target.Thing == null || t != target.Thing) && (t.def.useHitPoints || t is Pawn) && (pawn.HostileTo(t) || t.Faction == null))
                {
                    Vector3 vfxOffset = new Vector3((Rand.Value - 0.5f), (Rand.Value - 0.5f), (Rand.Value - 0.5f));
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(t.Position.ToVector3Shifted() + vfxOffset, pawn.Map, FleckDefOf.PsycastSkipInnerExit, 0.3f);
                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    pawn.Map.flecks.CreateFleck(dataStatic);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(t.Position.ToVector3Shifted() + vfxOffset, pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 0.3f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    pawn.Map.flecks.CreateFleck(dataStatic2);
                    t.TakeDamage(new DamageInfo(HautsDefOf.Hauts_SkipFrag, 5f, 999f, -1, pawn, t is Pawn p ? p.health.hediffSet.GetRandomNotMissingPart(HautsDefOf.Hauts_SkipFrag) : null));
                }
            }
            if (target != null && pawn.story != null)
            {
                if (__instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("WordOf"))
                {
                    bool didCanary = false;
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird))
                    {
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 4f * Math.Max(2f, pawn.GetPsylinkLevel()));
                        bool canary = pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary);
                        foreach (Pawn p in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 4f * Math.Max(2f, pawn.GetPsylinkLevel()), true).OfType<Pawn>().Distinct<Pawn>())
                        {
                            if (p != pawn && __instance.verb.targetParams.CanTarget(p))
                            {
                                bool hitEm = true;
                                LocalTargetInfo lti = new LocalTargetInfo(p);
                                for (int i = 0; i < __instance.EffectComps.Count; i++)
                                {
                                    if (!__instance.EffectComps[i].Valid(lti, false))
                                    {
                                        hitEm = false;
                                    }
                                }
                                if (hitEm)
                                {
                                    MethodInfo methodInfo = typeof(Psycast).GetMethod("ApplyEffects", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(IEnumerable<CompAbilityEffect>), typeof(LocalTargetInfo), typeof(LocalTargetInfo) }, null);
                                    methodInfo.Invoke(__instance, new object[] { __instance.EffectComps, lti, dest });
                                }
                            }
                            if (canary)
                            {
                                didCanary = true;
                                PsychicAwakeningUtility.DoCanaryEffects(pawn, p);
                            }
                        }
                    }
                    if (!didCanary && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary))
                    {
                        if (target.Thing != null && target.Thing is Pawn p)
                        {
                            PsychicAwakeningUtility.DoCanaryEffects(pawn,p);
                        }
                    }
                }
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBouldermit) && __instance.def.level >= 6 && Rand.Chance(0.1f))
                {
                    IncidentParms parms = new IncidentParms
                    {
                        target = pawn.Map
                    };
                    DefDatabase<IncidentDef>.GetNamedSilentFail("MeteoriteImpact").Worker.TryExecute(parms);
                }
                if (target.Cell != null && target.Cell.InBounds(pawn.Map))
                {
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDiabolus))
                    {
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.def.level));
                        foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(target.Cell, __instance.def.level, true))
                        {
                            if (iv3.IsValid && GenSight.LineOfSight(target.Cell, iv3, pawn.MapHeld, true, null, 0, 0) && FilthMaker.TryMakeFilth(iv3, pawn.MapHeld, ThingDefOf.Filth_Fuel, 1, FilthSourceFlags.None, true))
                            {
                                continue;
                            }
                        }
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBouldermit))
                    {
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.def.level));
                        ThingDef chunkDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                           where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.StoneChunks) select tdef).RandomElement();
                        if (chunkDef != null)
                        {
                            GenSpawn.Spawn(chunkDef,target.Cell,pawn.Map);
                            if (Rand.Chance(__instance.FinalPsyfocusCost(target)))
                            {
                                ThingDef metalDef = (from td in DefDatabase<ThingDef>.AllDefsListForReading
                                                     where td.stuffProps != null && td.stuffProps.categories != null && td.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic) select td).RandomElement();
                                if (metalDef != null)
                                {
                                    Thing metal = ThingMaker.MakeThing(metalDef);
                                    metal.stackCount = (int)(Rand.Value*metalDef.stackLimit);
                                    GenSpawn.Spawn(metal,target.Cell,pawn.Map);
                                }
                            }
                        }
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitFirefly) && target.Cell.WalkableByAny(pawn.Map))
                    {
                        ThingWithComps firefly = (ThingWithComps)ThingMaker.MakeThing(HVTRoyaltyDefOf.HVT_FireflyLight);
                        firefly.GetComp<CompAuraEmitter>().creator = pawn;
                        GenSpawn.Spawn(firefly, target.Cell, pawn.Map, WipeMode.Vanish);
                        CompAbilityEffect_Teleport.SendSkipUsedSignal(target, pawn);
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(target.Cell, pawn.Map, false));
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOilbird))
                    {
                        foreach (Hediff h in pawn.health.hediffSet.hediffs)
                        {
                            if (h is Hediff_Oilbird ho)
                            {
                                if (ho.activeAura != null && !ho.activeAura.Destroyed)
                                {
                                    ho.activeAura.Destroy();
                                }
                                ho.MakeNewAura(target.Cell);
                                break;
                            }
                        }
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOrca))
                    {
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.def.level));
                        foreach (IntVec3 c in GenRadial.RadialCellsAround(target.Cell, (float)Math.Pow(1.7d, __instance.def.level), true))
                        {
                            if (c.InBounds(pawn.Map))
                            {
                                List<Thing> thingList = c.GetThingList(pawn.Map);
                                for (int i = thingList.Count - 1; i >= 0; i--)
                                {
                                    Pawn pawn2;
                                    if (thingList[i] is Fire)
                                    {
                                        thingList[i].Destroy(DestroyMode.Vanish);
                                    } else if ((pawn2 = (thingList[i] as Pawn)) != null) {
                                        HediffComp_Invisibility invisibilityComp = pawn2.GetInvisibilityComp();
                                        if (invisibilityComp != null)
                                        {
                                            invisibilityComp.DisruptInvisibility();
                                        }
                                    }
                                }
                                if (!c.Filled(pawn.Map))
                                {
                                    FilthMaker.TryMakeFilth(c, pawn.Map, ThingDefOf.Filth_Water, 1, FilthSourceFlags.None, true);
                                }
                                FleckCreationData dataStatic = FleckMaker.GetDataStatic(c.ToVector3Shifted(), pawn.Map, FleckDefOf.WaterskipSplashParticles, 1f);
                                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                pawn.Map.flecks.CreateFleck(dataStatic);
                            }
                        }
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitTermite))
                    {
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f* (float)Math.Pow(1.7d, __instance.def.level));
                        foreach (Thing thing in GenRadial.RadialDistinctThingsAround(target.Cell, pawn.MapHeld, (float)Math.Pow(1.7d, __instance.def.level), true))
                        {
                            if (pawn.HostileTo(thing))
                            {
                                if (thing is Building)
                                {
                                    SoundInfo info = SoundInfo.InMap(new TargetInfo(thing.Position, thing.Map, false), MaintenanceType.None);
                                    SoundDefOf.Building_Deconstructed.PlayOneShot(info);
                                    thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, 25f * pawn.GetStatValue(StatDefOf.PsychicSensitivity), 0f, -1, pawn));
                                } else if (thing is Pawn p) {
                                    p.stances.stagger.StaggerFor((int)Math.Ceiling(60f*Math.Min((pawn.GetStatValue(StatDefOf.PsychicSensitivity)+p.GetStatValue(StatDefOf.PsychicSensitivity))/2f,2f)));
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void HautsTraitsTrans_Psycast_ActivatePostfix2(Psycast __instance)
        {
            PsychicAwakeningUtility.PsycastActivationRiderEffects(__instance);
        }
        public static bool HautsTraitsTrans_SpawnFleshbeastFromPawnPrefix(Pawn pawn)
        {
            if (PsychicAwakeningUtility.IsTranscendent(pawn))
            {
                Messages.Message("HVT_ImmuneToBiomutation".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
        public static bool HautsTraitsTrans_TryExecutePrefix(IncidentWorker __instance, IncidentParms parms)
        {
            if (parms.quest == null && ((__instance.def.letterDef != null && (__instance.def.letterDef == LetterDefOf.NegativeEvent || __instance.def.letterDef == LetterDefOf.ThreatBig)) || HautsUtility.badEventPool.Contains(__instance.def)))
            {
                foreach (Pawn p in Find.World.PlayerPawnsForStoryteller)
                {
                    if (p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDeimatic) && Rand.Chance(0.5f))
                    {
                        TaggedString letterLabel = "HVT_Deimos".Translate();
                        TaggedString letterText = "HVT_Deimos2".Translate(p.Name.ToStringShort, __instance.def.label);
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        letterLabel, letterText, LetterDefOf.PositiveEvent, null, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                        return false;
                    }
                }
            }
            return true;
        }
        public static void HautsTraitsTrans_ChanceFactorPostfix(ref float __result, HediffGiver __instance, Pawn pawn)
        {
            if (__instance.hediff == HediffDefOf.Carcinoma && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitEvergreen))
            {
                __result = 0f;
            }
        }
        public static bool HautsTraitsTrans_GenerateRandomOldAgeInjuriesPrefix(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitEvergreen))
            {
                return false;
            }
            return true;
        }
        public static void HautsTraitsTrans_GetQualityPostfix(ref float __result, Pawn surgeon, Pawn patient)
        {
            if (surgeon.story != null && surgeon.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitXerigium))
            {
                __result = 1f;
                PsychicAwakeningUtility.XerigiumHeal(surgeon, patient);
            }
            if (patient.story != null && patient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitFossil))
            {
                __result = 1f;
            }
        }
        public static void HautsTraitsTrans_DoTendPostfix(Pawn doctor, Pawn patient)
        {
            if (doctor != null && doctor.story != null && doctor.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitXerigium))
            {
                PsychicAwakeningUtility.XerigiumHeal(doctor, patient);
            }
        }
        public static void HautsTraitsTrans_GenerateQualityCreatedByPawnPostfix(ref QualityCategory __result, Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWarbler) && (__result == QualityCategory.Awful || Rand.Value <= 0.5f))
                {
                    __result = (QualityCategory)Mathf.Min((int)(__result + (byte)1), 6);//half of inspired creativity w/ a half chance to proc.
                }
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWeaverbird))
                {
                    __result = (QualityCategory)Mathf.Min((int)(__result + (byte)1), 6);
                }
            }
        }
        public static void HautsTraitsTrans_AdjustedMeleeDamageAmountPostfix(ref float __result, Tool tool, Pawn attacker, Thing equipment)
        {
            if (tool != null && equipment != null && equipment.def.comps != null && attacker != null && attacker.story != null && attacker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCassowary))
            {
                for (int i = 0; i < equipment.def.comps.Count; i++)
                {
                    if (equipment.def.comps[i].compClass == typeof(CompBladelinkWeapon))
                    {
                        __result *= 2.2f * attacker.GetStatValue(StatDefOf.PsychicSensitivity);
                        break;
                    }
                }
            }
        }
        public static void HautsTraitsTrans_Quest_EndPostfix(QuestEndOutcome outcome)
        {
            if (outcome == QuestEndOutcome.Success)
            {
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    for (int j = 0; j < Find.Maps[i].mapPawns.AllPawnsSpawned.Count; j++)
                    {
                        if (Find.Maps[i].mapPawns.AllPawnsSpawned[j].story != null && Find.Maps[i].mapPawns.AllPawnsSpawned[j].story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitMagpie))
                        {
                            Pawn pawn = Find.Maps[i].mapPawns.AllPawnsSpawned[j];
                            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastSkipInnerExit, 3f);
                            SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(pawn);
                            List<Thing> list = new List<Thing>();
                            int tries = 10;
                            while (list.Count < pawn.GetStatValue(StatDefOf.PsychicSensitivity)*(1f+Rand.Value) && tries > 0)
                            {
                                ThingSetMakerDef thingSetMakerDef = ThingSetMakerDefOf.Reward_ItemsStandard;
                                int treasure = (int)Math.Ceiling(Rand.Value * 3);
                                switch (treasure)
                                {
                                    case 1:
                                        thingSetMakerDef = ThingSetMakerDefOf.MapGen_AncientTempleContents;
                                        break;
                                    case 2:
                                        thingSetMakerDef = ThingSetMakerDefOf.DebugCaravanInventory;
                                        break;
                                    case 3:
                                        break;
                                    default:
                                        break;
                                }
                                list = thingSetMakerDef.root.Generate(default(ThingSetMakerParams));
                                tries--;
                            }
                            if (list.Count > 0)
                            {
                                foreach (Thing t in list)
                                {
                                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 6, null);
                                    GenPlace.TryPlaceThing(t, loc, pawn.Map, ThingPlaceMode.Near, null, null, default);
                                    FleckMaker.AttachedOverlay(t, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                                    FleckMaker.AttachedOverlay(t, FleckDefOf.PsycastSkipOuterRingExit, Vector3.zero, 1f, -1f);
                                }
                                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                            }
                        }
                    }
                }
            }
        }
        public static void HautsTraitsTrans_PlantCollectedPostfix(Plant __instance, Pawn by)
        {
            if (by.story != null && by.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook) && __instance.def.plant.harvestedThingDef != null)
            {
                int num = GenMath.RoundRandom((float)(__instance.YieldNow() * (by.GetStatValue(StatDefOf.PsychicSensitivity)/3f)));
                if (num > 0)
                {
                    Thing thing = ThingMaker.MakeThing(__instance.def.plant.harvestedThingDef, null);
                    thing.stackCount = num;
                    if (by.Faction != Faction.OfPlayerSilentFail)
                    {
                        thing.SetForbidden(true, true);
                    }
                    GenPlace.TryPlaceThing(thing, by.Position, by.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                }
            }
        }
        public static void HautsTraitsTrans_GatheredPostfix(CompHasGatherableBodyResource __instance, Pawn doer)
        {
            if (doer.story != null && doer.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook))
            {
                typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, Math.Min(Rand.Value*0.8f, doer.GetStatValue(StatDefOf.PsychicSensitivity) / 10f));
            }
        }
        public static void HautsTraitsTrans_DestroyMinedPostfix(Mineable __instance, Pawn pawn)
        {
            if (pawn != null && pawn.Spawned && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook))
            {
                Thing thing = ThingMaker.MakeThing(__instance.def.building.mineableThing, null);
                thing.stackCount = (int)Math.Ceiling(Math.Max(1f, __instance.def.building.EffectiveMineableYield * Math.Min(1f, pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1f) / 3f));
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default);

            }
        }
        public static void HautsTraitsTrans_MakeRecipeProductsPostfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept, ThingStyleDef style, int? overrideGraphicIndex)
        {
            if (worker.story != null)
            {
                if (worker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWeaverbird) && Rand.Chance(0.5f))
                {
                    foreach (Thing product in __result)
                    {
                        CompQuality compQualityO = product.TryGetComp<CompQuality>();
                        if (compQualityO != null)
                        {
                            Thing thing = ThingMaker.MakeThing(product.def, product.Stuff ?? null);
                            CompQuality compQualityN = thing.TryGetComp<CompQuality>();
                            if (worker.Ideo != null)
                            {
                                thing.StyleDef = worker.Ideo.GetStyleFor(thing.def);
                            }
                            if (precept != null)
                            {
                                thing.StyleSourcePrecept = precept;
                            } else if (style != null) {
                                thing.StyleDef = style;
                            } else if (!thing.def.randomStyle.NullOrEmpty<ThingStyleChance>() && Rand.Chance(thing.def.randomStyleChance)) {
                                thing.SetStyleDef(thing.def.randomStyle.RandomElementByWeight((ThingStyleChance x) => x.Chance).StyleDef);
                            }
                            thing.overrideGraphicIndex = overrideGraphicIndex;
                            if (thing.def.Minifiable)
                            {
                                thing = thing.MakeMinified();
                            }
                            if (worker.SpawnedOrAnyParentSpawned)
                            {
                                GenSpawn.Spawn(thing, product.SpawnedOrAnyParentSpawned ? product.PositionHeld : worker.PositionHeld, worker.MapHeld);
                            } else if (worker.inventory != null) {
                                worker.inventory.innerContainer.TryAdd(thing,true);
                            }
                            if (compQualityN != null)
                            {
                                compQualityN.SetQuality(compQualityO.Quality, new ArtGenerationContext?(ArtGenerationContext.Colony));
                            }
                            CompArt compArt = thing.TryGetComp<CompArt>();
                            if (compArt != null)
                            {
                                compArt.JustCreatedBy(worker);
                                if (compQualityN != null && compQualityN.Quality >= QualityCategory.Excellent)
                                {
                                    TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[] { worker, product });
                                }
                            }
                            CompColorable ccN = product.TryGetComp<CompColorable>();
                            CompColorable ccO = thing.TryGetComp<CompColorable>();
                            if (ccN != null && ccO != null)
                            {
                                ccO.SetColor(ccN.Color);
                            }
                        }
                    }
                }
                if (recipeDef.specialProducts != null && worker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook))
                {
                    int num;
                    for (int i = 0; i < recipeDef.specialProducts.Count; i = num + 1)
                    {
                        for (int j = 0; j < ingredients.Count; j = num + 1)
                        {
                            Thing thing2 = ingredients[j];
                            SpecialProductType specialProductType = recipeDef.specialProducts[i];
                            if (specialProductType == SpecialProductType.Butchery)
                            {
                                foreach (Thing product3 in thing2.ButcherProducts(worker, Math.Min(1f, worker.GetStatValue(StatDefOf.PsychicSensitivity) - 1f) / 3f))
                                {
                                    CompQuality compQuality = product3.TryGetComp<CompQuality>();
                                    if (compQuality != null)
                                    {
                                        if (recipeDef.workSkill == null)
                                        {
                                            Log.Error(recipeDef + " needs workSkill because it creates a product with a quality.");
                                        }
                                        QualityCategory q = QualityUtility.GenerateQualityCreatedByPawn(worker, recipeDef.workSkill);
                                        compQuality.SetQuality(q, ArtGenerationContext.Colony);
                                        QualityUtility.SendCraftNotification(product3, worker);
                                    }
                                    CompArt compArt = product3.TryGetComp<CompArt>();
                                    if (compArt != null)
                                    {
                                        compArt.JustCreatedBy(worker);
                                        if (compQuality != null && compQuality.Quality >= QualityCategory.Excellent)
                                        {
                                            TaleRecorder.RecordTale(TaleDefOf.CraftedArt, new object[]
                                            {
                                                worker,
                                                product3
                                            });
                                        }
                                    }
                                    if (worker.Ideo != null)
                                    {
                                        product3.StyleDef = worker.Ideo.GetStyleFor(product3.def);
                                    }
                                    if (precept != null)
                                    {
                                        product3.StyleSourcePrecept = precept;
                                    }
                                    else if (style != null)
                                    {
                                        product3.StyleDef = style;
                                    }
                                    product3.overrideGraphicIndex = overrideGraphicIndex;
                                    if (product3.def.Minifiable)
                                    {
                                        Thing product4 = product3.MakeMinified();
                                        GenPlace.TryPlaceThing(product4, worker.Position, worker.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                                    }
                                    else
                                    {
                                        GenPlace.TryPlaceThing(product3, worker.Position, worker.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                                    }
                                }
                            }
                            num = j;
                        }
                        num = i;
                    }
                }
            }
        }
        public static void HautsTraitsTrans_AdjustedRangePostfix(ref float __result, Verb ownerVerb, Pawn attacker)
        {
            if (attacker != null && ownerVerb is Verb_CastAbility && __result > 0 && attacker.story != null && attacker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPelican))
            {
                __result *= 1.5f;
            }
        }
        public static void HautsTraitsTrans_GetRangeForPawnPostfix(ref float __result, VFECore.Abilities.Ability __instance)
        {
            if (__instance.pawn != null && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPelican) && __result > 0f)
            {
                __result *= 1.5f;
            }
        }
        public static void HautsTraitsTrans_TryInteractWithPostfix(Pawn_InteractionsTracker __instance, Pawn recipient)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (recipient.abilities != null && pawn.abilities != null)
            {
                PsychicAwakeningUtility.MynahAbilityCopy(pawn, recipient);
                PsychicAwakeningUtility.MynahAbilityCopy(recipient, pawn);
            }
        }
        public static bool HautsTraitsTrans_ExposedToBloodRainPrefix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    if (t.def.HasModExtension<BloodRainImmune>())
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static void HautsTraitsTrans_CanApplyPsycastToPostfix(ref bool __result, LocalTargetInfo target, Psycast __instance)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon) && p.HostileTo(__instance.pawn))
            {
                __result = false;
            }
        }
        public static void HautsTraitsIsOtherDisallowedTraitPostfix(ref bool __result, TraitDef t)
        {
            if (PsychicAwakeningUtility.IsAwakenedTrait(t) || PsychicAwakeningUtility.IsTranscendentTrait(t))
            {
                __result = true;
            }
        }
        public static void HautsTraitsTrans_PostAddPostfix(Hediff_Psylink __instance)
        {
            int hediffCount = __instance.pawn.health.hediffSet.hediffs.Count;
            for (int i = hediffCount - 1; i >= 0; i--)
            {
                if (__instance.pawn.health.hediffSet.hediffs[i] is Hediff_Censure hc)
                {
                    __instance.pawn.health.RemoveHediff(hc);
                }
            }
        }
        public static void HautsTraitsIsBadTraitComboPostfix(ref bool __result, TraitDef t, Pawn pawn)
        {
            if (t == HVTRoyaltyDefOf.HVT_LatentPsychic && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
            {
                __result = true;
            }
        }
        public static void HautsTraitsCOaNNIsLatentPsychicPostfix(ref bool __result, TraitDef def)
        {
            if (def == HVTRoyaltyDefOf.HVT_LatentPsychic)
            {
                __result = true;
            }
        }
        public static void HautsTraitsCOaNNAwakenPostfix(Pawn user, List<TraitDef> defs)
        {
            if (defs.Contains(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                PsychicAwakeningUtility.AwakenPsychicTalent(user, true, "HVT_WokeBrainwash".Translate().Formatted(user.Named("PAWN")).AdjustedFor(user, "PAWN", true).Resolve(), "HVT_WokeBrainwashFantasy".Translate().Formatted(user.Named("PAWN")).AdjustedFor(user, "PAWN", true).Resolve());
            }
        }
        public static void HautsTraitsIsAwakenedPsychicPostfix(Pawn pawn, ref bool __result)
        {
            if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
            {
                __result = true;
            }
        }
    }
    [DefOf]
    public static class HVTRoyaltyDefOf
    {
        static HVTRoyaltyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVTRoyaltyDefOf));
        }
        public static TraitDef HVT_Anarchist;
        public static TraitDef HVT_Servile;
        public static TraitDef HVT_LatentPsychic;
        public static TraitDef HVT_AwakenedAugur;
        public static TraitDef HVT_AwakenedChanshi;
        [MayRequireBiotech]
        public static TraitDef HVT_ChanshiGene;
        public static TraitDef HVT_AwakenedErudite;
        public static TraitDef HVT_TTraitAptenodytes;
        public static TraitDef HVT_TTraitBellbird;
        public static TraitDef HVT_TTraitDiabolus;
        public static TraitDef HVT_TTraitBouldermit;
        public static TraitDef HVT_TTraitCanary;
        public static TraitDef HVT_TTraitCassowary;
        public static TraitDef HVT_TTraitCuckoo;
        public static TraitDef HVT_TTraitDeimatic;
        public static TraitDef HVT_TTraitDulotic;
        public static TraitDef HVT_TTraitElectrophorus;
        public static TraitDef HVT_TTraitEvergreen;
        public static TraitDef HVT_TTraitFirefly;
        public static TraitDef HVT_TTraitFossil;
        public static TraitDef HVT_TTraitGlowworm;
        public static TraitDef HVT_TTraitHarbinger;
        public static TraitDef HVT_TTraitLovebug;
        public static TraitDef HVT_LovebugDoppel;
        public static TraitDef HVT_LocustClone;
        public static TraitDef HVT_TTraitMagpie;
        public static TraitDef HVT_TTraitMynah;
        public static TraitDef HVT_TTraitNightingale;
        public static TraitDef HVT_TTraitNoctule;
        public static TraitDef HVT_TTraitOilbird;
        public static TraitDef HVT_TTraitOrbWeaver;
        public static TraitDef HVT_TTraitOrca;
        public static TraitDef HVT_TTraitPelican;
        public static TraitDef HVT_TTraitRaven;
        public static TraitDef HVT_TTraitRook;
        public static TraitDef HVT_TTraitShrike;
        public static TraitDef HVT_TTraitTermite;
        public static TraitDef HVT_TTraitWarbler;
        public static TraitDef HVT_TTraitWeaverbird;
        public static TraitDef HVT_TTraitXerigium;
        public static TraitDef HVT_TTraitDragon;
        public static TraitDef HVT_TTraitHarpy;
        public static TraitDef HVT_TTraitLeviathan;
        public static TraitDef HVT_TTraitPhoenix;
        public static TraitDef HVT_TTraitSeraph;
        public static TraitDef HVT_TTraitSphinx;
        public static TraitDef HVT_TTraitThunderbird;
        public static TraitDef HVT_TTraitWraith;
        public static TraitDef HVT_TTraitZiz;

        public static ThoughtDef HVT_CanarySong;
        public static ThoughtDef HVT_GoldfinchThought;

        public static AbilityDef HVT_ArchicSyzygy;
        public static AbilityDef HVT_ArchicSyzygy2;
        public static AbilityDef HVT_PhoenixAbility;
        public static AbilityDef HVT_ZizAbility;

        public static HediffDef HVT_LatentPsyTerminus;
        public static HediffDef HVT_AwakeningAfterglow;
        public static HediffDef HVT_AwakenedDeathTracker;
        public static HediffDef HVT_LuminaryGeneAura;
        public static HediffDef HVT_PsychicIncarnate;
        public static HediffDef HVT_SyzygyBuff;
        public static HediffDef HVT_TitanpowerGene;
        public static HediffDef HVT_CountdownToTranscendence;
        public static HediffDef HVT_TranscendentHediff;
        public static HediffDef HVT_THediffAnimalume;
        public static HediffDef HVT_ApocritonControl;
        public static HediffDef HVT_THediffBat;
        public static HediffDef HVT_DulosisTimedLife;
        public static HediffDef HVT_THediffFossilPart;
        public static HediffDef HVT_THediffHummingbird;
        public static HediffDef HVT_THediffHummingbird0;
        public static HediffDef HVT_THediffLocust;
        public static HediffDef HVT_LocustTimedLife;
        public static HediffDef HVT_THediffMagpie;
        public static HediffDef HVT_THediffScarlet;
        public static HediffDef HVT_THediffStork;
        public static HediffDef HVT_SwanBuff;
        public static HediffDef HVT_WQBond;
        public static HediffDef HVT_ErinysCensure;
        public static HediffDef HVT_DragonsHoard;
        public static HediffDef HVT_HarpysHunger;
        public static HediffDef HVT_PhoenixPostResurrection;
        public static HediffDef HVT_THediffWraith;
        public static HediffDef HVT_Wraithform;
        public static HediffDef HVT_ZizBuff;

        public static EffecterDef HVT_Zomburst;

        public static ThingDef HVT_AlbatrossSquall;
        public static ThingDef HVT_BowerShield;
        public static ThingDef HVT_FireflyLight;
        public static ThingDef HVT_InnerWildfire;
        public static ThingDef HVT_OilbirdAura;
        public static ThingDef HVT_ColdColdHeart;
        public static ThingDef HVT_PulverizationBeam;

        [MayRequireBiotech]
        public static GeneDef HVT_AChanshiGene;
        [MayRequireBiotech]
        public static GeneDef HVT_AEruditeGene;
        [MayRequireBiotech]
        public static GeneDef HVT_AUndyingGene;
    }
    public class Thought_Situational_RelationsWithEmpire : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (this.pawn.Faction.def.HasRoyalTitles)
            {
                return -20f;
            }
            int num = 0;
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f.def.HasRoyalTitles)
                {
                    num -= (this.pawn.Faction.GoodwillWith(f) / 10);
                }
            }
            return this.BaseMoodOffset * num;
        }
    }
    public class ThoughtWorker_Imperial : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (other.Faction == null || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.Faction.def.HasRoyalTitles || other.Faction.def.HasModExtension<AnarchistHatedFaction>())
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_Leader : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.royalty.MainTitle() != null || ModsConfig.IdeologyActive && other.Ideo != null && other.Ideo.GetRole(other) != null)
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_HVT_AnarchistVsServile : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, other) || !other.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_Servile))
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_HVT_ServileVsAnarchist : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, other) || !other.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_Anarchist))
            {
                return false;
            }
            return true;
        }
    }
    public class Thought_Situational_LeadersInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            if (this.pawn.royalty.MainTitle() != null || (ModsConfig.IdeologyActive && this.pawn.ideo != null && this.pawn.ideo.Ideo.GetRole(this.pawn) != null))
            {
                return -12f;
            }
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != this.pawn && enumerator.Current.royalty.MainTitle() != null)
                        {
                            num += enumerator.Current.royalty.MostSeniorTitle.def.seniority / 100;
                        }
                    }
                }
            } else if (pawn.GetCaravan() != null) {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p != this.pawn && p.Faction != null && this.pawn.Faction == p.Faction)
                    {
                        num += p.royalty.MostSeniorTitle.def.seniority / 100;
                    }
                }
            }
            if (ModsConfig.IdeologyActive && this.pawn.ideo != null)
            {
                if (this.pawn.Map != null)
                {
                    using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current != this.pawn && enumerator.Current.ideo != null && enumerator.Current.ideo.Ideo == this.pawn.ideo.Ideo && enumerator.Current.ideo.Ideo.GetRole(enumerator.Current) != null)
                            {
                                num += 1;
                            }
                        }
                    }
                } else if (pawn.GetCaravan() != null) {
                    foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                    {
                        if (p != this.pawn && p.Faction != null && this.pawn.Faction == p.Faction && p.ideo != null && p.ideo.Ideo == this.pawn.ideo.Ideo && p.ideo.Ideo.GetRole(p) != null)
                        {
                            num += 1;
                        }
                    }
                }
            }
            if (num <= 0)
            {
                return -8f;
            }
            if (num > 10)
            {
                num = 10;
            }
            return this.BaseMoodOffset * num;
        }
    }
    public class ThoughtWorker_HVT_TCuckoo : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCuckoo) || pawn.GetStatValue(StatDefOf.PsychicSensitivity, true, -1) < 1E-45f)
            {
                return false;
            }
            return true;
        }
    }
    public class Hediff_LPLoveMeter : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.relations != null)
            {
                foreach (DirectPawnRelation dpr in this.pawn.relations.DirectRelations)
                {
                    PsychicAwakeningUtility.LPLoveCheckRelations(dpr.def, this.pawn, dpr.otherPawn);
                }
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity >= 500f && Current.ProgramState == ProgramState.Playing)
            {
                PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeSuperRelations".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeSuperRelationsFantasy".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), false, 0f);
            }
        }
    }
    public class Hediff_LPLifeMeter : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500) && Current.ProgramState == ProgramState.Playing)
            {
                foreach (Hediff h in this.pawn.health.hediffSet.hediffs)
                {
                    if (h.CurStage != null && h.CurStage.lifeThreatening && h.FullyImmune())
                    {
                        if (Rand.Value <= 0.2f)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalentCheck(this.pawn, 5, true, "HVT_WokeBeatIllness".Translate(h.Label, this.pawn.Name.ToStringShort, this.pawn.gender.GetPossessive()).Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeBeatIllnessFantasy".Translate(h.Label, this.pawn.Name.ToStringShort, this.pawn.gender.GetPossessive()).Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), false, 0f);
                            break;
                        } else {
                            this.Severity = 0.001f;
                        }
                    }
                }
            }
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            if (Current.ProgramState == ProgramState.Playing)
            {
                PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 5, true, "HVT_WokeResurrection".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeResurrectionFantasy".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
        }
    }
    public class Hediff_LP6 : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            if (ResurrectionUtility.TryResurrect(pawnToRez))
            {
                PsychicAwakeningUtility.AwakenPsychicTalent(pawnToRez, true, "HVT_WokeDeath".Translate().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_WokeDeathFantasy".Translate().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve());
            }
        }
    }
    public class Hediff_IncarnatePsycastsKnown : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(150))
            {
                float castsKnown = 0f;
                foreach (Ability a in this.pawn.abilities.abilities)
                {
                    if (a.def.IsPsycast)
                    {
                        castsKnown += 1f;
                    }
                }
                if (castsKnown > 0f)
                {
                    this.Severity = castsKnown;
                } else {
                    this.Severity = 0.001f;
                }
                this.Severity = this.VPECompat();
            }
        }
        public float VPECompat()
        {
            return this.Severity;
        }
    }
    public class Hediff_TransEffect : HediffWithComps
    {
        public override void PreRemoved()
        {
            base.PreRemoved();
        }
        public override void PostTick()
        {
            base.PostTick();
            if (ModsConfig.AnomalyActive && this.pawn.IsHashIntervalTick(250) && this.pawn.IsMutant)
            {
                Messages.Message("HVT_ImmuneToGhoulizing".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, false);
                this.pawn.mutant.Revert();
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.pawn.story != null && PsychicAwakeningUtility.IsTranscendent(this.pawn))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.def, this.pawn, null);
                float newSev = 0f;
                foreach (Trait t in this.pawn.story.traits.allTraits) {
                    if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                    {
                        newSev += 1f;
                    }
                }
                if (newSev <= 0f)
                {
                    newSev = 0.001f;
                }
                hediff.Severity = newSev;
                this.pawn.health.AddHediff(hediff, null, null, null);
            }
        }
    }
    public class Hediff_CrossroadsPartII : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.pawn.Spawned)
                    {
                        Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_AlbatrossSquall);
                        if (t != null)
                        {
                            CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                            if (cdad != null)
                            {
                                cdad.spawnTick = Find.TickManager.TicksGame;
                            }
                            CompWindSource cws = t.TryGetComp<CompWindSource>();
                            if (cws != null)
                            {
                                cws.wind = Math.Max(2f, cws.wind + 0.025f);
                            }
                        } else {
                            GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_AlbatrossSquall, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                        }
                    }
                }
            }
        }
    }
    public class Hediff_IndeterminateGrowth : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.pawn))
            {
                this.Severity = Rand.Value * 4f * (1f+Math.Max(1f,this.pawn.ageTracker.AgeBiologicalYears-10f));
            }
        }
    }
    public class Hediff_Animalume : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (this.pawn.Corpse.Map != null)
            {

                List<Thing> animaTrees = this.pawn.Corpse.Map.listerThings.ThingsOfDef(ThingDefOf.Plant_TreeAnima);
                if (animaTrees.Count > 0)
                {
                    Thing animaTree = animaTrees.RandomElement<Thing>();
                    IntVec3 destination = animaTree.Position;
                    FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(this.pawn.Corpse, FleckDefOf.PsycastSkipFlashEntry, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                    dataAttachedOverlay.link.detachAfterTicks = 5;
                    this.pawn.Corpse.Map.flecks.CreateFleck(dataAttachedOverlay);
                    FleckMaker.Static(destination, this.pawn.Corpse.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                    FleckMaker.Static(destination, this.pawn.Corpse.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                    SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(this.pawn.Corpse.Position, this.pawn.Corpse.Map, false));
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(destination, this.pawn.Corpse.Map, false));
                    animaTree.Kill();
                    this.pawn.Corpse.Position = destination;
                    CompAbilityEffect_Teleport.SendSkipUsedSignal(this.pawn.Corpse.Position, this.pawn.Corpse);
                    ResurrectionUtility.TryResurrect(this.pawn.Corpse.InnerPawn);
                }
            } else if (Rand.Value <= 0.1f) {
                if (ResurrectionUtility.TryResurrect(this.pawn.Corpse.InnerPawn))
                {
                    if (PawnUtility.ShouldSendNotificationAbout(this.pawn))
                    {
                        Messages.Message("HVT_CaravanAnimaRez".Translate(this.pawn), null, MessageTypeDefOf.PositiveEvent, true);
                    }
                    if (this.pawn.needs != null && this.pawn.needs.mood != null)
                    {
                        this.pawn.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("AnimaScream"));
                    }
                }
            }
        }
    }
    public class Hediff_ApocritonControl : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.originalFaction = this.pawn.Faction;
            if (this.pawn.RaceProps.mechWeightClass < MechWeightClass.UltraHeavy)
            {
                if (this.newFaction != null && this.pawn.RaceProps.IsMechanoid)
                {
                    this.pawn.SetFaction(this.newFaction);
                    this.pawn.jobs.StopAll(false, true);
                    if (originalCaster != null)
                    {
                        LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.originalCaster), this.pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                    }
                    if (this.pawn.Faction != null && this.pawn.Faction != Faction.OfPlayer && this.pawn.HostileTo(Faction.OfPlayer))
                    {
                        LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction, true, true, false, false, true, false, false), pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                    }
                }
            }
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            if (ModsConfig.BiotechActive && this.pawn.GetOverseer() != null)
            {
                return;
            }
            this.pawn.SetFaction(this.originalFaction);
            this.pawn.jobs.StopAll(false, true);
            this.Severity = 0f;
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (ModsConfig.BiotechActive && this.pawn.GetOverseer() != null)
            {
                return;
            }
            if (!this.pawn.Dead)
            {
                this.pawn.SetFaction(this.originalFaction);
                if (this.pawn.jobs != null)
                {
                    this.pawn.jobs.StopAll(false, true);
                }
                if (this.pawn.Faction != null && this.pawn.Faction != Faction.OfPlayer && this.pawn.HostileTo(Faction.OfPlayer))
                {
                    LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction, true, true, false, false, true, false, false), pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.originalCaster, "originalCaster", false);
            Scribe_References.Look<Faction>(ref this.newFaction, "newFaction", false);
            Scribe_References.Look<Faction>(ref this.originalFaction, "originalFaction", false);
        }
        public Pawn originalCaster;
        public Faction newFaction;
        public Faction originalFaction = Faction.OfMechanoids;
    }
    public class Hediff_Aptenodytes : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                if (Rand.Chance(0.0001f))
                {
                    for (int i = 0; i < this.pawn.skills.skills.Count; i++)
                    {
                        if (this.pawn.skills.skills[i].GetLevel() >= 15)
                        {
                            PsychicAwakeningUtility.ColonyHuddle(this.pawn);
                            return;
                        }
                    }
                    if (this.Severity >= 500f)
                    {
                        PsychicAwakeningUtility.ColonyHuddle(this.pawn);
                    }
                }
                foreach (Hediff h in this.pawn.health.hediffSet.hediffs)
                {
                    if (h.CurStage != null && h.CurStage.lifeThreatening && h.FullyImmune())
                    {
                        if (Rand.Value <= 0.075f)
                        {
                            PsychicAwakeningUtility.ColonyHuddle(this.pawn);
                            return;
                        }
                    }
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            if (ResurrectionUtility.TryResurrect(pawnToRez))
            {
                PsychicAwakeningUtility.ColonyHuddle(this.pawn);
            }
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            PsychicAwakeningUtility.ColonyHuddle(this.pawn);
        }
    }
    public class Hediff_Bower : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25))
            {
                if (this.Severity >= 0.5f && this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_BowerShield);
                    if (t != null)
                    {
                        CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                        if (cdad != null)
                        {
                            cdad.spawnTick = Find.TickManager.TicksGame;
                        }
                    } else {
                        GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_BowerShield, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                    }
                    if (this.pawn.IsHashIntervalTick(625))
                    {
                        if (this.pawn.Spawned)
                        {
                            foreach (Pawn p in this.pawn.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (p.RaceProps.IsFlesh && p.Position.DistanceTo(this.pawn.Position) <= 2.5f)
                                {
                                    HautsUtility.StatScalingHeal(1f,StatDefOf.PsychicSensitivity,p,p);
                                }
                            }
                        } else if (this.pawn.GetCaravan() != null) {
                            foreach (Pawn p in this.pawn.GetCaravan().pawns.InnerListForReading)
                            {
                                HautsUtility.StatScalingHeal(2f, StatDefOf.PsychicSensitivity, p, p);
                            }
                        }
                    }
                }
            }
        }
    }
    public class Hediff_Budgie : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(60000) && this.pawn.Faction != null && this.pawn.Faction == Faction.OfPlayerSilentFail)
            {
                foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                {
                    if (f != this.pawn.Faction && f.def.humanlikeFaction && !f.def.PermanentlyHostileTo(FactionDefOf.PlayerColony) && f.HasGoodwill)
                    {
                        Faction.OfPlayerSilentFail.TryAffectGoodwillWith(f, (int)Math.Ceiling((double)Math.Min(12,this.pawn.GetPsylinkLevel())/3), true, true, HistoryEventDefOf.ReachNaturalGoodwill, null);
                    }
                }
            }
        }
    }
    public class Hediff_Dove : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25) && this.pawn.MapHeld != null)
            {
                GameCondition activeCondition = this.pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.PsychicDrone);
                if (activeCondition != null)
                {
                    activeCondition.Duration = 0;
                }
                if (this.pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.PsychicSoothe) == null)
                {
                    IncidentParms parms = new IncidentParms
                    {
                        target = this.pawn.MapHeld
                    };
                    IncidentDef soothe = DefDatabase<IncidentDef>.GetNamed("PsychicSoothe");
                    soothe.Worker.TryExecute(parms);
                }
            }
        }
    }
    public class Hediff_Dulosis : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.playerSettings != null)
            {
                this.pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            this.pawn.health.RemoveHediff(this);
        }
    }
    public class Hediff_InnerWildfire : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.pawn.Spawned)
                    {
                        Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_InnerWildfire);
                        if (t != null)
                        {
                            CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                            if (cdad != null)
                            {
                                cdad.spawnTick = Find.TickManager.TicksGame;
                            }
                        } else {
                            GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_InnerWildfire, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                        }
                    }
                }
            }
        }
    }
    public class Hediff_Fossil : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury)
                {
                    this.pawn.health.RemoveHediff(hediffs[i]);
                } else if (hediffs[i] is Hediff_MissingPart hmp) {
                    if (this.pawn.Spawned)
                    {
                        MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(this.pawn, hediffs[i].Part, this.pawn.Position, this.pawn.Map);
                    } else {
                        this.pawn.health.RestorePart(hediffs[i].Part);
                    }
                }
            }
            this.Fossilize();
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                this.Fossilize();
            }
        }
        public void Fossilize()
        {
            if (this.pawn.story != null)
            {
                this.pawn.story.skinColorOverride = new Color(0.549f, 0.5607f, 0.3961f);
                this.pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            foreach (BodyPartRecord bpr in this.pawn.RaceProps.body.AllParts)
            {
                if (bpr.depth == BodyPartDepth.Outside && pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).Contains(bpr) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(bpr) && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_THediffFossilPart,bpr))
                {
                    this.pawn.health.AddHediff(HVTRoyaltyDefOf.HVT_THediffFossilPart, bpr);
                }
            }
        }
    }
    public class Hediff_BlotOutTheSun : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25) && this.pawn.psychicEntropy.IsCurrentlyMeditating && this.pawn.Spawned &&  this.pawn.Map.gasGrid.DensityAt(this.pawn.Position,GasType.BlindSmoke) <= 80)
            {
                GenExplosion.DoExplosion(this.pawn.PositionHeld, this.pawn.MapHeld, 2.9f, DamageDefOf.Smoke, null, -1, -1f, SoundDefOf.Psycast_Skip_Pulse, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
            }
        }
    }
    public class Hediff_DawnChorus : Hediff_PreDamageModification
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(60000))
            {
                PsychicAwakeningUtility.PsychicHeal(this.pawn, true);
                this.Severity = 1f;
                if (this.pawn.needs != null && this.pawn.needs.mood != null)
                {
                    this.pawn.needs.mood.thoughts.memories.TryGainMemory(HVTRoyaltyDefOf.HVT_GoldfinchThought);
                }
                if (this.pawn.equipment != null)
                {
                    foreach (ThingWithComps twc in this.pawn.equipment.AllEquipmentListForReading)
                    {
                        if (twc.HitPoints < twc.MaxHitPoints)
                        {
                            twc.HitPoints = Math.Min((int)(GenMath.RoundRandom(twc.MaxHitPoints) / 60f) + twc.HitPoints, twc.MaxHitPoints);
                        }
                    }
                }
                if (this.pawn.apparel != null)
                {
                    foreach (Apparel a in this.pawn.apparel.WornApparel)
                    {
                        if (a.HitPoints < a.MaxHitPoints)
                        {
                            a.HitPoints = Math.Min((int)(GenMath.RoundRandom(a.MaxHitPoints) / 60f) + a.HitPoints, a.MaxHitPoints);
                        }
                    }
                }
            }
        }
    }
    public class Hediff_HornetSting : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            HediffComp_ExtendedVerbGiver evg = this.TryGetComp<HediffComp_ExtendedVerbGiver>();
            if (evg != null)
            {
                this.baseRange = evg.VerbTracker.PrimaryVerb.verbProps.range;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            HediffComp_ExtendedVerbGiver evg = this.TryGetComp<HediffComp_ExtendedVerbGiver>();
            if (evg != null && this.pawn.IsHashIntervalTick(15))
            {
                Verb verb = evg.VerbTracker.PrimaryVerb;
                Type type = verb.verbProps.GetType();
                VerbProperties verbPropsCopy = Activator.CreateInstance(type) as VerbProperties;
                foreach (FieldInfo fieldInfo in type.GetFields())
                {
                    try
                    {
                        FieldInfo field = type.GetField(fieldInfo.Name);
                        field.SetValue(verbPropsCopy, fieldInfo.GetValue(verb.verbProps));
                    }
                    catch { }
                }
                Traverse traverse = Traverse.Create(verbPropsCopy).Field("range");
                traverse.SetValue(Math.Max(1f,this.baseRange * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * this.pawn.GetStatValue(VFEDefOf.VEF_VerbRangeFactor)));
                verb.verbProps = verbPropsCopy;
            }
        }
        private float baseRange = 0;
    }
    public class Hediff_EnsureJoyNeed : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.EnsureJoy();
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                this.EnsureJoy();
            }
        }
        public void EnsureJoy()
        {
            if (this.pawn.story != null)
            {
                for (int i = this.pawn.story.traits.TraitsSorted.Count - 1; i >= 0; i --)
                {
                    if (this.pawn.story.traits.TraitsSorted[i].CurrentData.disablesNeeds != null && this.pawn.story.traits.TraitsSorted[i].CurrentData.disablesNeeds.Contains(DefDatabase<NeedDef>.GetNamed("Joy")))
                    {
                        this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.TraitsSorted[i]);
                    }
                }
            }
            if (ModsConfig.BiotechActive && this.pawn.genes != null)
            {
                for (int i = this.pawn.genes.GenesListForReading.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.genes.GenesListForReading[i].def.disablesNeeds != null && this.pawn.genes.GenesListForReading[i].def.disablesNeeds.Contains(DefDatabase<NeedDef>.GetNamed("Joy")))
                    {
                        this.pawn.genes.RemoveGene(this.pawn.genes.GenesListForReading[i]);
                    }
                }
            }
            for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
            {
                if (this.pawn.health.hediffSet.hediffs[i].def.disablesNeeds != null && this.pawn.health.hediffSet.hediffs[i].def.disablesNeeds.Contains(DefDatabase<NeedDef>.GetNamed("Joy")))
                {
                    this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                }
            }
        }
    }
    public class Hediff_Jackdaw : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.maxSeverity)
            {
                this.Severity = 0.001f;
                PsychicAwakeningUtility.MakeGoodEvent(this.pawn);
            }
        }
    }
    public class Hediff_KeaCuriosity : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.pawn))
            {
                if (this.pawn.skills != null)
                {
                    foreach (SkillRecord s in this.pawn.skills.skills)
                    {
                        if (!s.TotallyDisabled)
                        {
                            s.Level += (int)(Rand.Value * 5);
                        }
                    }
                }
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(10))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    PsychicAwakeningUtility.KeaOffsetPsyfocusLearning(this.pawn,0.0002f*this.pawn.GetStatValue(StatDefOf.MeditationFocusGain),0.01f);
                }
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast psycast)
            {
                PsychicAwakeningUtility.KeaOffsetPsyfocusLearning(this.pawn, psycast.FinalPsyfocusCost(target),1f);
            } else if (verb is VFECore.Abilities.Verb_CastAbility vcavfe && HautsUtility.IsVPEPsycast(vcavfe.ability)) {
                PsychicAwakeningUtility.KeaOffsetPsyfocusLearning(this.pawn, HautsUtility.GetVPEPsyfocusCost(vcavfe.ability),1f);
            }
        }
    }
    public class Hediff_Apollyon : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.maxSeverity && this.pawn.SpawnedOrAnyParentSpawned)
            {
                this.pawnsToSpawn.Clear();
                this.Severity = 0.001f;
                for (int i = 0; i < (int)this.pawn.GetStatValue(StatDefOf.PsychicSensitivity); i++)
                {
                    Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.pawn.kindDef, this.pawn.Faction, PawnGenerationContext.NonPlayer, this.pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                    if (newPawn != null)
                    {
                        newPawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LocustClone));
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in newPawn.story.traits.allTraits)
                        {
                            if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic || PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove) {
                            newPawn.story.traits.RemoveTrait(t);
                        }
                        List<Gene> genesToRemove = new List<Gene>();
                        if (ModsConfig.BiotechActive && newPawn.genes != null)
                        {
                            foreach (Gene g in newPawn.genes.GenesListForReading)
                            {
                                if (PsychicAwakeningUtility.IsAwakenedPsychicGene(g.def))
                                {
                                    genesToRemove.Add(g);
                                }
                            }
                        }
                        foreach (Gene g in genesToRemove)
                        {
                            newPawn.genes.RemoveGene(g);
                        }
                        Hediff_Psylink hediff_Psylink = newPawn.GetMainPsylinkSource();
                        if (hediff_Psylink != null)
                        {
                            newPawn.health.RemoveHediff(hediff_Psylink);
                        }
                        for (int j = 0; j < 5; j++)
                        {
                            HealthUtility.FixWorstHealthCondition(newPawn);
                        }
                        newPawn.inventory.DestroyAll();
                        if (newPawn.equipment != null)
                        {
                            foreach (ThingWithComps thingWithComps in newPawn.equipment.AllEquipmentListForReading)
                            {
                                CompBiocodable comp = thingWithComps.GetComp<CompBiocodable>();
                                if (comp != null && !comp.Biocoded)
                                {
                                    comp.CodeFor(newPawn);
                                }
                            }
                        }
                        foreach (Apparel apparel in newPawn.apparel.WornApparel)
                        {
                            PawnApparelGenerator.PostProcessApparel(apparel, newPawn);
                            CompBiocodable comp = apparel.TryGetComp<CompBiocodable>();
                            if (comp != null && !comp.Biocoded)
                            {
                                comp.CodeFor(newPawn);
                            }
                        }
                        newPawn.apparel.LockAll();
                        pawnsToSpawn.Add(newPawn);
                        if (ModsConfig.IdeologyActive && newPawn.ideo != null && this.pawn.ideo != null)
                        {
                            newPawn.ideo.SetIdeo(this.pawn.ideo.Ideo);
                        }
                    }
                }
                if (this.pawn.SpawnedOrAnyParentSpawned)
                {
                    foreach (Pawn p in pawnsToSpawn)
                    {
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.PositionHeld, this.pawn.MapHeld, 6, null);
                        GenPlace.TryPlaceThing(p, loc, this.pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                        FleckMaker.AttachedOverlay(p, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                        DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(p.PositionHeld, p.MapHeld, false));
                        if (!p.IsColonistPlayerControlled)
                        {
                            LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(p));
                        }
                    }
                }
                this.pawnsToSpawn.Clear();
            }
        }
        private List<Pawn> pawnsToSpawn = new List<Pawn>();
    }
    public class Hediff_Locusta : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();
            if (this.pawn.Downed)
            {
                PsychicAwakeningUtility.LocustVanish(this.pawn);
            }
        }
    }
    public class Hediff_EchoKnight : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.maxSeverity && (this.doppelganger == null || this.doppelganger.Dead || this.doppelganger.Discarded || this.doppelganger.Destroyed))
            {
                this.MakeDoppelganger();
            }
        }
        private void MakeDoppelganger()
        {
            Pawn pawn = this.pawn;
            float ageBiologicalYearsFloat = pawn.ageTracker.AgeBiologicalYearsFloat;
            float num = pawn.ageTracker.AgeChronologicalYearsFloat;
            if (num > ageBiologicalYearsFloat)
            {
                num = ageBiologicalYearsFloat;
            }
            PawnKindDef kindDef = pawn.kindDef;
            Faction faction = pawn.Faction;
            PawnGenerationContext context = PawnGenerationContext.NonPlayer;
            int tile = -1;
            bool forceGenerateNewPawn = true;
            bool allowDead = false;
            bool allowDowned = false;
            bool canGeneratePawnRelations = false;
            bool mustBeCapableOfViolence = false;
            float colonistRelationChanceFactor = 0f;
            bool forceAddFreeWarmLayerIfNeeded = false;
            bool allowGay = true;
            bool allowPregnant = false;
            bool allowFood = true;
            bool allowAddictions = true;
            bool inhabitant = false;
            bool certainlyBeenInCryptosleep = false;
            bool forceRedressWorldPawnIfFormerColonist = false;
            bool worldPawnFactionDoesntMatter = false;
            float biocodeWeaponChance = 0f;
            float biocodeApparelChance = 0f;
            Pawn extraPawnForExtraRelationChance = null;
            float relationWithExtraPawnChanceFactor = 0f;
            Predicate<Pawn> validatorPreGear = null;
            Predicate<Pawn> validatorPostGear = null;
            IEnumerable<TraitDef> forcedTraits = null;
            IEnumerable<TraitDef> prohibitedTraits = null;
            float? minChanceToRedressWorldPawn = null;
            Gender? fixedGender = new Gender?((Rand.Chance(0.99f) ? pawn.gender : pawn.gender.Opposite()));
            Ideo ideo = pawn.Ideo;
            Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kindDef, faction, context, tile, forceGenerateNewPawn, allowDead, allowDowned, canGeneratePawnRelations, mustBeCapableOfViolence, colonistRelationChanceFactor, forceAddFreeWarmLayerIfNeeded, allowGay, allowPregnant, allowFood, allowAddictions, inhabitant, certainlyBeenInCryptosleep, forceRedressWorldPawnIfFormerColonist, worldPawnFactionDoesntMatter, biocodeWeaponChance, biocodeApparelChance, extraPawnForExtraRelationChance, relationWithExtraPawnChanceFactor, validatorPreGear, validatorPostGear, forcedTraits, prohibitedTraits, minChanceToRedressWorldPawn, new float?(ageBiologicalYearsFloat), new float?(num), fixedGender, null, null, null, ideo, false, false, false, false, null, null, pawn.genes.Xenotype, pawn.genes.CustomXenotype, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, true));
            newPawn.Name = NameTriple.FromString(pawn.Name.ToString(), false);
            newPawn.genes.xenotypeName = pawn.genes.xenotypeName;
            if (Rand.Chance(0.95f))
            {
                newPawn.story.favoriteColor = pawn.story.favoriteColor;
            }
            newPawn.story.Childhood = pawn.story.Childhood;
            if (Rand.Chance(0.95f))
            {
                newPawn.story.Adulthood = pawn.story.Adulthood;
            }
            newPawn.story.traits.allTraits.Clear();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if ((!ModsConfig.BiotechActive || trait.sourceGene == null) && trait.def != HVTRoyaltyDefOf.HVT_TTraitLovebug)
                {
                    newPawn.story.traits.GainTrait(new Trait(trait.def, trait.Degree, trait.ScenForced), false);
                }
            }
            newPawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LovebugDoppel));
            newPawn.genes.Endogenes.Clear();
            newPawn.genes.Xenogenes.Clear();
            foreach (Gene g in pawn.genes.Endogenes)
            {
                newPawn.genes.AddGene(g.def, false);
            }
            foreach (Gene g in pawn.genes.Xenogenes)
			{
                newPawn.genes.AddGene(g.def, true);
            }
            newPawn.story.headType = pawn.story.headType;
            newPawn.story.bodyType = pawn.story.bodyType;
            if (Rand.Chance(0.95f))
            {
                newPawn.story.hairDef = pawn.story.hairDef;
            }
            if (Rand.Chance(0.99f))
            {
                newPawn.story.HairColor = pawn.story.HairColor;
            }
            newPawn.story.SkinColorBase = pawn.story.SkinColorBase;
            newPawn.story.skinColorOverride = pawn.story.skinColorOverride;
            newPawn.story.furDef = pawn.story.furDef;
            if (Rand.Chance(0.95f))
            {
                newPawn.style.beardDef = pawn.style.beardDef;
            }
            if (ModsConfig.IdeologyActive && Rand.Chance(0.95f))
            {
                newPawn.style.BodyTattoo = pawn.style.BodyTattoo;
                newPawn.style.FaceTattoo = pawn.style.FaceTattoo;
            }
            newPawn.skills.skills.Clear();
            foreach (SkillRecord skillRecord in pawn.skills.skills)
            {
                SkillRecord item = new SkillRecord(newPawn, skillRecord.def)
                {
                    levelInt = skillRecord.levelInt,
                    passion = skillRecord.passion,
                    xpSinceLastLevel = skillRecord.xpSinceLastLevel,
                    xpSinceMidnight = skillRecord.xpSinceMidnight
                };
                newPawn.skills.skills.Add(item);
            }
            newPawn.health.hediffSet.hediffs.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in hediffs)
            {
                if (hediff.def.duplicationAllowed && (hediff.Part == null || newPawn.health.hediffSet.HasBodyPart(hediff.Part)))
                {
                    Hediff hediff2 = HediffMaker.MakeHediff(hediff.def, newPawn, hediff.Part);
                    hediff2.CopyFrom(hediff);
                    newPawn.health.hediffSet.AddDirect(hediff2, null, null);
                }
            }
            newPawn.needs.AllNeeds.Clear();
            foreach (Need need in pawn.needs.AllNeeds)
            {
                Need need2 = (Need)Activator.CreateInstance(need.def.needClass, new object[]
                {
                    newPawn
                });
                need2.def = need.def;
                newPawn.needs.AllNeeds.Add(need2);
                need2.SetInitialLevel();
                need2.CurLevel = need.CurLevel;
                newPawn.needs.BindDirectNeedFields();
            }
            if (pawn.needs.mood != null)
            {
                List<Thought_Memory> memories = newPawn.needs.mood.thoughts.memories.Memories;
                memories.Clear();
                foreach (Thought_Memory thought_Memory in pawn.needs.mood.thoughts.memories.Memories)
                {
                    Thought_Memory thought_Memory2 = (Thought_Memory)ThoughtMaker.MakeThought(thought_Memory.def);
                    thought_Memory2.CopyFrom(thought_Memory);
                    thought_Memory2.pawn = newPawn;
                    memories.Add(thought_Memory2);
                }
            }
			foreach (Ability ability in pawn.abilities.abilities)
			{
				if (newPawn.abilities.GetAbility(ability.def, false) == null)
				{
					newPawn.abilities.GainAbility(ability.def);
				}
            }
            VFECore.Abilities.CompAbilities comp = pawn.GetComp<VFECore.Abilities.CompAbilities>();
            VFECore.Abilities.CompAbilities comp2 = newPawn.GetComp<VFECore.Abilities.CompAbilities>();
            if (comp != null && comp2 != null)
            {
                List<VFECore.Abilities.Ability> learnedAbilities = HautsTraitsRoyalty.GetInstanceField(typeof(VFECore.Abilities.CompAbilities), comp, "learnedAbilities") as List<VFECore.Abilities.Ability>;
                for (int i = 0; i < learnedAbilities.Count; i++)
                {
                    if (!comp2.HasAbility(learnedAbilities[i].def))
                    {
                        comp2.GiveAbility(learnedAbilities[i].def);
                        HautsUtility.VPEUnlockAbility(newPawn, learnedAbilities[i].def);
                    }
                }
            }
            List<Ability> abilities = newPawn.abilities.abilities;
			for (int i = abilities.Count - 1; i >= 0; i--)
			{
				Ability ability2 = abilities[i];
				if (pawn.abilities.GetAbility(ability2.def, false) == null)
				{
					newPawn.abilities.RemoveAbility(ability2.def);
				}
            }
            if (pawn.guest != null)
            {
                newPawn.guest.Recruitable = pawn.guest.Recruitable;
            }
            newPawn.Drawer.renderer.SetAllGraphicsDirty();
            newPawn.Notify_DisabledWorkTypesChanged();
            if (pawn.SpawnedOrAnyParentSpawned)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.PositionHeld, pawn.MapHeld, 6, null);
                GenPlace.TryPlaceThing(newPawn, loc, pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                if (!newPawn.IsColonistPlayerControlled)
                {
                    LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                }
            }
            this.doppelganger = newPawn;
            this.Severity = 0.001f;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.doppelganger, "doppelganger", false);
        }
        private Pawn doppelganger;
    }
    public class Hediff_TheHeartOfDarkness : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(60))
            {
                for (int i = this.pawn.health.hediffSet.hediffs.Count -1; i >= 0; i--)
                {
                    if (this.pawn.health.hediffSet.hediffs[i] is Hediff_DarknessExposure)
                    {
                        this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                    }
                }
            }
        }
    }
    public class Hediff_Oilbird : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.Spawned)
            {
                if (this.activeAura == null)
                {
                    this.MakeNewAura(this.pawn.Position);
                } else if (this.activeAura.Map == null || this.activeAura.Map != this.pawn.Map) {
                    if (!this.activeAura.Destroyed)
                    {
                        this.activeAura.Destroy();
                    }
                    this.MakeNewAura(this.pawn.Position);
                }
            } else if (this.activeAura != null && !this.activeAura.Destroyed) {
                this.activeAura.Destroy();
            }
        }
        public override void PreRemoved()
        {
            base.PreRemoved();
            if (this.activeAura != null && !this.activeAura.Destroyed)
            {
                this.activeAura.Destroy();
            }
        }
        public void MakeNewAura(IntVec3 position)
        {
            this.activeAura = GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_OilbirdAura, position, this.pawn.Map, WipeMode.Vanish);
            CompAuraEmitter cae = this.activeAura.TryGetComp<CompAuraEmitter>();
            if (cae != null)
            {
                cae.faction = this.pawn.Faction??null;
                cae.creator = this.pawn;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.activeAura, "activeAura", false);
        }
        public Thing activeAura;
    }
    public class Hediff_Resplendence : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.needs.beauty != null && !this.pawn.Suspended && (!this.pawn.needs.beauty.def.freezeWhileSleeping || this.pawn.Awake()) && (!this.pawn.needs.beauty.def.freezeInMentalState || !this.pawn.InMentalState) && (this.pawn.SpawnedOrAnyParentSpawned || this.pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(this.pawn)))
            {
                this.pawn.needs.beauty.CurLevel += 0.00025f;
            }
        }
    }
    public class Hediff_Polux : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity >= this.def.maxSeverity && this.pawn.Spawned)
            {
                this.Severity = this.def.initialSeverity;
                int cells = GenRadial.NumCellsInRadius(10f);
                for (int i = 0; i < cells; i++)
                {
                    bool done = false;
                    IntVec3 c = this.pawn.Position + GenRadial.RadialPattern[i];
                    if (c.InBounds(this.pawn.Map) && c.IsValid)
                    {
                        TerrainDef td = this.pawn.Map.terrainGrid.TerrainAt(c);
                        if (this.pawn.Map.Biome == BiomeDefOf.SeaIce)
                        {
                            this.pawn.Map.terrainGrid.SetTerrain(c, TerrainDefOf.Ice);
                            done = true;
                        } else if (td.driesTo != null) {
                            this.pawn.Map.terrainGrid.SetTerrain(c, td.driesTo);
                            done = true;
                        }
                        TerrainDef utd = this.pawn.Map.terrainGrid.UnderTerrainAt(c);
                        if (utd != null)
                        {
                            if (this.pawn.Map.Biome == BiomeDefOf.SeaIce)
                            {
                                this.pawn.Map.terrainGrid.SetUnderTerrain(c, TerrainDefOf.Ice);
                                done = true;
                            } else if (td.driesTo != null) {
                                this.pawn.Map.terrainGrid.SetUnderTerrain(c, td.driesTo);
                                done = true;
                            }
                        }
                        if (c.CanUnpollute(this.pawn.Map))
                        {
                            this.pawn.Map.pollutionGrid.SetPolluted(c, false, false);
                            done = true;
                        }
                        if (!td.IsFloor && !td.affordances.Contains(TerrainAffordanceDefOf.SmoothableStone) && !td.IsRiver && !td.IsWater)
                        {
                            List<TerrainDef> tdList = HautsUtility.FertilityTerrainDefs(this.pawn.Map);
                            IOrderedEnumerable<TerrainDef> source = from e in tdList.FindAll((TerrainDef e) => (double)e.fertility > (double)td.fertility && !e.IsWater && !e.IsRiver)
                                                                    orderby e.fertility
                                                                    select e;
                            if (source.Count<TerrainDef>() != 0)
                            {
                                TerrainDef newTerr = source.First<TerrainDef>();
                                this.pawn.Map.terrainGrid.SetTerrain(c, newTerr);
                                done = Rand.Chance(0.2f);
                            }
                        }
                    }
                    if (done)
                    {
                        SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(this.pawn.Position, this.pawn.Map, false));
                        return;
                    }
                }
            }
        }

    }
    public class Hediff_Pearl : HediffWithComps
    {
        public override void Tick()
        {
            if (this.pawn.IsHashIntervalTick(60))
            {
                if (this.pawn.apparel != null)
                {
                    foreach (Apparel a in this.pawn.apparel.WornApparel)
                    {
                        if (a.def.useHitPoints)
                        {
                            a.HitPoints = Math.Min(a.HitPoints+1,a.MaxHitPoints);
                        }
                    }
                }
                if (this.pawn.equipment != null)
                {
                    foreach (Thing t in this.pawn.equipment.AllEquipmentListForReading)
                    {
                        if (t.def.useHitPoints)
                        {
                            t.HitPoints = Math.Min(t.HitPoints+1,t.MaxHitPoints);
                        }
                    }
                }
                if (this.pawn.IsHashIntervalTick(2400) && this.Severity >= 1f)
                {
                    float mfg = this.pawn.GetStatValue(StatDefOf.MeditationFocusGain);
                    if (Rand.Chance(0.08f*mfg))
                    {
                        List<CompQuality> items = new List<CompQuality>();
                        if (this.pawn.apparel != null)
                        {
                            foreach (Apparel a in this.pawn.apparel.WornApparel)
                            {
                                CompQuality qc = a.TryGetComp<CompQuality>();
                                if (qc != null)
                                {
                                    items.Add(qc);
                                }
                            }
                        }
                        if (this.pawn.equipment != null)
                        {
                            foreach (Thing t in this.pawn.equipment.AllEquipmentListForReading)
                            {
                                CompQuality qc = t.TryGetComp<CompQuality>();
                                if (qc != null)
                                {
                                    items.Add(qc);
                                }
                            }
                        }
                        if (items.Count > 0)
                        {
                            CompQuality cq = items.RandomElement();
                            cq.SetQuality((QualityCategory)Mathf.Min((int)(cq.Quality + (byte)1), 6), null);
                        }
                    }
                    if (Rand.Chance(0.005f * mfg))
                    {
                        ThingDef itemDef;
                        if (Rand.Chance(0.5f))
                        {
                            itemDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                                 where tdef.thingClass != null && tdef.thingClass == typeof(Book)
                                                 select tdef).RandomElement();
                        } else {
                            itemDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                       where tdef.thingClass != null && tdef.thingClass == typeof(Building_Art) && tdef.Minifiable && tdef.building != null && tdef.building.expandHomeArea == true
                                       select tdef).RandomElement();
                        }
                        if (itemDef != null)
                        {
                            ThingDef stuff = GenStuff.RandomStuffFor(itemDef);
                            Thing thing = ThingMaker.MakeThing(itemDef, stuff);
                            CompQuality compQuality = thing.TryGetComp<CompQuality>();
                            if (compQuality != null)
                            {
                                compQuality.SetQuality(Rand.Chance(0.8f) ? QualityCategory.Masterwork: QualityCategory.Legendary, ArtGenerationContext.Colony);
                            }
                            if (thing.def.Minifiable)
                            {
                                thing = thing.MakeMinified();
                            }
                            if (thing.def.CanHaveFaction)
                            {
                                thing.SetFaction(this.pawn.Faction, null);
                            }
                            if (this.pawn.Spawned)
                            {
                                IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.Position, this.pawn.Map, 6, null);
                                GenPlace.TryPlaceThing(thing, loc, this.pawn.Map, ThingPlaceMode.Near, null, null, default);
                                thing.Notify_DebugSpawned();
                                if (thing.Position != null && thing.Map != null)
                                {
                                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                    thing.Map.flecks.CreateFleck(dataStatic);
                                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                    thing.Map.flecks.CreateFleck(dataStatic2);
                                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(thing.Position, thing.Map, false));
                                }
                            } else if (this.pawn.inventory != null) {
                                this.pawn.inventory.innerContainer.TryAdd(thing, true);
                            }
                        }
                    }
                    this.Severity -= 1f;
                }
            }
            base.Tick();
        }
    }
    public class Hediff_Hammerspace : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity == this.def.maxSeverity)
            {
                this.Severity = 0.001f;
                List<Thing> list = new List<Thing>();
                int tries = 10;
                while (list.Count < this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * (1f + Rand.Value) && tries > 0)
                {
                    ThingSetMakerDef thingSetMakerDef;
                    float treasure = Rand.Value;
                    if (treasure <= 0.4f)
                    {
                        thingSetMakerDef = ThingSetMakerDefOf.DebugCaravanInventory;
                    } else if (treasure <= 0.8f) {
                        thingSetMakerDef = ThingSetMakerDefOf.ResourcePod;
                    } else if (treasure <= 0.95f) {
                        thingSetMakerDef = ThingSetMakerDefOf.MapGen_AncientTempleContents;
                    } else {
                        thingSetMakerDef = ThingSetMakerDefOf.Reward_ItemsStandard;
                    }
                    list = thingSetMakerDef.root.Generate(default(ThingSetMakerParams));
                    tries--;
                }
                for (int i = list.Count - 1; i >= this.pawn.GetPsylinkLevel(); i--)
                {
                    list.Remove(list[i]);
                }
                if (list.Count > 0)
                {
                    foreach (Thing t in list)
                    {
                        t.stackCount = (int)Math.Min(Rand.Value*100f,t.def.stackLimit);
                        this.pawn.inventory.innerContainer.TryAdd(t, true);
                    }
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                }
            }
        }
    }
    public class Hediff_Robin : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(300) && this.pawn.GetCaravan() != null && this.pawn.GetCaravan().pather.MovingNow && Rand.Chance(Math.Min(10.05f, 10.004f * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity))))
            {
                float maxValue = Rand.Chance(0.9f) ? Math.Min(6f, this.pawn.GetPsylinkLevel()) * 600f : -1f;
                ThingDef tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                 where ((tdef.BaseMarketValue <= maxValue || maxValue < 0f) && (tdef.category == ThingCategory.Item || (tdef.category == ThingCategory.Building && tdef.Minifiable)) && !tdef.thingClass.IsAssignableFrom(typeof(MinifiedThing)) && !tdef.thingClass.IsAssignableFrom(typeof(MinifiedTree)) && !tdef.thingClass.IsAssignableFrom(typeof(Corpse)) && !tdef.thingClass.IsAssignableFrom(typeof(UnfinishedThing)))
                                 select tdef).RandomElement();
                if (tDef != null)
                {
                    Thing thing = ThingMaker.MakeThing(tDef, GenStuff.RandomStuffFor(tDef));
                    CompQuality cq = thing.TryGetComp<CompQuality>();
                    if (cq != null)
                    {
                        cq.SetQuality(QualityUtility.GenerateQuality(Rand.Chance(0.8f) ? QualityGenerator.BaseGen : QualityGenerator.Reward), ArtGenerationContext.Outsider);
                    }
                    thing.PostPostMake();
                    thing.stackCount = Math.Min(tDef.stackLimit, maxValue > 0f ? (int)Math.Ceiling(maxValue / tDef.BaseMarketValue) : tDef.stackLimit);
                    if (tDef.Minifiable)
                    {
                        Thing thing2 = thing.MakeMinified();
                        this.pawn.GetCaravan().AddPawnOrItem(thing2, true);
                    } else {
                        this.pawn.GetCaravan().AddPawnOrItem(thing, true);
                    }
                    Messages.Message("HVT_RobinGetItem".Translate().CapitalizeFirst().Formatted(this.pawn.Name.ToStringShort, thing.Label).Resolve(), null, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    public class Hediff_ColdColdHeart : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(25))
            {
                if (this.pawn.health.hediffSet.HasHediff(HediffDefOf.Hypothermia))
                {
                    this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia));
                }
                Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_ColdColdHeart);
                if (t != null)
                {
                    CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                    if (cdad != null)
                    {
                        cdad.spawnTick = Find.TickManager.TicksGame;
                    }
                } else {
                    GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_ColdColdHeart, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                }
            }
        }
    }
    public class Hediff_Ororo : HediffWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            this.weather = WeatherDefOf.Clear;
        }
        public GameConditionDef ConditionDef
        {
            get
            {
                return GameConditionDefOf.WeatherController;
            }
        }
        public IEnumerable<GameCondition> CausedConditions
        {
            get
            {
                return this.causedConditions.Values;
            }
        }
        public int MyTile
        {
            get
            {
                if (this.pawn.SpawnedOrAnyParentSpawned)
                {
                    return this.pawn.Tile;
                }
                else if (this.pawn.GetCaravan() != null)
                {
                    return this.pawn.GetCaravan().Tile;
                }
                return -1;
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if ((this.pawn.IsPlayerControlled || DebugSettings.ShowDevGizmos) && !this.pawn.DeadOrDowned && !this.pawn.Suspended && !this.pawn.InMentalState && this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
            {
                yield return new Command_Action
                {
                    defaultLabel = this.weather.LabelCap,
                    defaultDesc = "HVT_ChangeWeather".Translate(),
                    icon = ContentFinder<Texture2D>.Get("PsychicTraits/Abilities/HVT_PkWeatherControl", true),
                    action = delegate
                    {
                        List<WeatherDef> allDefsListForReading = new List<WeatherDef>();
                        foreach (WeatherDef wd in DefDatabase<WeatherDef>.AllDefsListForReading)
                        {
                            if (!wd.HasModExtension<NotStormable>())
                            {
                                allDefsListForReading.Add(wd);
                            }
                        }
                        int num = allDefsListForReading.FindIndex((WeatherDef w) => w == this.weather);
                        num++;
                        if (num >= allDefsListForReading.Count)
                        {
                            num = 0;
                        }
                        GameConditionDef oldConsequent = this.consequent;
                        this.weather = allDefsListForReading[num];
                        this.ReSetupAllConditions(oldConsequent);
                    },
                    hotKey = KeyBindingDefOf.Misc1
                };
            }
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
        }
        public bool InAoE(int tile)
        {
            return this.MyTile != -1 && tile != -1 && Find.WorldGrid.ApproxDistanceInTiles(tile, this.MyTile) < Math.Max(6f,(float)this.pawn.GetPsylinkLevel());
        }
        public GameCondition GetAndNullifyConditionInstance(Map map)
        {
            GameCondition activeCondition = null;
            for (int i = map.GameConditionManager.ActiveConditions.Count - 1; i >= 0; i--)
            {
                GameCondition gc = map.GameConditionManager.ActiveConditions[i];
                if (gc.def == this.ConditionDef)
                {
                    if (gc.conditionCauser != null)
                    {
                        if (gc.conditionCauser == this.pawn)
                        {
                            activeCondition = gc;
                            this.SetupCondition(activeCondition, map);
                            if (!this.causedConditions.ContainsKey(map))
                            {
                                this.causedConditions.Add(map, activeCondition);
                            }
                        } else {
                            CompCauseGameCondition_ForceWeather ccgcfw = gc.conditionCauser.TryGetComp<CompCauseGameCondition_ForceWeather>();
                            if (ccgcfw != null)
                            {
                                if (ccgcfw.weather != this.weather)
                                {
                                    ccgcfw.weather = this.weather;
                                    this.SetupCondition(gc, map);
                                }
                            }
                            else if (!gc.conditionCauser.DestroyedOrNull())
                            {
                                gc.conditionCauser.Kill();
                            }
                        }
                    }
                } else if (gc.def.weatherDef != null && gc.def.weatherDef != this.weather) {
                    gc.End();
                }
            }
            return activeCondition;
        }
        public override void PostTick()
        {
            base.PostTick();
            if (!this.pawn.Downed)
            {
                foreach (Map map in Find.Maps)
                {
                    if (this.InAoE(map.Tile))
                    {
                        this.EnforceConditionOn(map);
                    }
                }
            }
            Hediff_Ororo.tmpDeadConditionMaps.Clear();
            foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
            {
                if (keyValuePair.Value.Expired || !keyValuePair.Key.GameConditionManager.ConditionIsActive(keyValuePair.Value.def))
                {
                    Hediff_Ororo.tmpDeadConditionMaps.Add(keyValuePair.Key);
                }
            }
            foreach (Map map2 in Hediff_Ororo.tmpDeadConditionMaps)
            {
                this.causedConditions.Remove(map2);
            }
        }
        private GameCondition EnforceConditionOn(Map map)
        {
            GameCondition gameCondition = this.GetAndNullifyConditionInstance(map);
            if (gameCondition == null)
            {
                gameCondition = this.CreateConditionOn(map);
            } else {
                gameCondition.TicksLeft = gameCondition.TransitionTicks;
            }
            StormCreateCondition scc = this.weather.GetModExtension<StormCreateCondition>();
            if (scc != null)
            {
                this.consequent = scc.conditionDef;
                if (map.gameConditionManager.GetActiveCondition(scc.conditionDef) == null)
                {
                    GameCondition gc = GameConditionMaker.MakeConditionPermanent(scc.conditionDef);
                    gc.conditionCauser = this.pawn;
                    map.gameConditionManager.RegisterCondition(gc);
                    this.causedConditionsConsequent.Add(map, gc);
                    gc.suppressEndMessage = true;
                }
            }
            return gameCondition;
        }
        protected virtual GameCondition CreateConditionOn(Map map)
        {
            if (this.causedConditions.ContainsKey(map))
            {
                return this.causedConditions[map];
            }
            GameCondition gameCondition = GameConditionMaker.MakeCondition(this.ConditionDef, -1);
            gameCondition.Duration = gameCondition.TransitionTicks;
            gameCondition.conditionCauser = this.pawn;
            map.gameConditionManager.RegisterCondition(gameCondition);
            this.causedConditions.Add(map, gameCondition);
            this.SetupCondition(gameCondition, map);
            return gameCondition;
        }
        protected virtual void SetupCondition(GameCondition condition, Map map)
        {
            condition.suppressEndMessage = true;
            ((GameCondition_ForceWeather)condition).weather = this.weather;
        }
        protected void ReSetupAllConditions(GameConditionDef oldConsequent)
        {
            foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
            {
                this.SetupCondition(keyValuePair.Value, keyValuePair.Key);
            }
            for (int i = this.causedConditionsConsequent.Count - 1; i >= 0; i--)
            {
                GameCondition gc = this.causedConditionsConsequent.TryGetValue(this.causedConditionsConsequent.Keys.ToList()[i]);
                if (gc != null && gc.def != this.consequent)
                {
                    gc.End();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.causedConditions.RemoveAll((KeyValuePair<Map, GameCondition> x) => !Find.Maps.Contains(x.Key));
            }
            Scribe_Collections.Look<Map, GameCondition>(ref this.causedConditions, "causedConditions", LookMode.Reference, LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.causedConditions.RemoveAll((KeyValuePair<Map, GameCondition> x) => x.Value == null);
                foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
                {
                    keyValuePair.Value.conditionCauser = this.pawn;
                }
            }
            Scribe_Collections.Look<Map, GameCondition>(ref this.causedConditionsConsequent, "causedConditionsConsequent", LookMode.Reference, LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.causedConditionsConsequent.RemoveAll((KeyValuePair<Map, GameCondition> x) => x.Value == null);
                foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditionsConsequent)
                {
                    keyValuePair.Value.conditionCauser = this.pawn;
                }
            }
            Scribe_Defs.Look<WeatherDef>(ref this.weather, "weather");
            Scribe_Defs.Look<GameConditionDef>(ref this.consequent, "consequent");
        }
        public WeatherDef weather;
        public GameConditionDef consequent;
        private Dictionary<Map, GameCondition> causedConditions = new Dictionary<Map, GameCondition>();
        private Dictionary<Map, GameCondition> causedConditionsConsequent = new Dictionary<Map, GameCondition>();
        private static List<Map> tmpDeadConditionMaps = new List<Map>();
    }
    public class Hediff_SwanLove : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                this.DetermineSeverity();
                this.HandOutBuffs();
            }
        }
        public void DetermineSeverity()
        {
            if (this.pawn.story != null && this.pawn.story.traits.HasTrait(TraitDefOf.Asexual))
            {
                this.Severity = 1f;
                return;
            }
            if (this.pawn.relations != null)
            {
                foreach (DirectPawnRelation dpr in this.pawn.relations.DirectRelations)
                {
                    if (dpr.def == PawnRelationDefOf.Spouse || dpr.def == PawnRelationDefOf.Fiance || dpr.def == PawnRelationDefOf.Lover)
                    {
                        this.Severity = 1f;
                        return;
                    }
                }
            }
            this.Severity = 0.001f;
        }
        public void HandOutBuffs()
        {
            foreach (DirectPawnRelation dpr in this.pawn.relations.DirectRelations)
            {
                if (dpr.def == PawnRelationDefOf.Spouse || dpr.def == PawnRelationDefOf.Fiance || dpr.def == PawnRelationDefOf.Lover)
                {
                    this.CreateBuff(dpr.otherPawn, Math.Max(this.pawn.relations.OpinionOf(dpr.otherPawn),1f));
                } else if (dpr.def == PawnRelationDefOf.Bond) {
                    this.CreateBuff(dpr.otherPawn, HVTRoyaltyDefOf.HVT_SwanBuff.maxSeverity / 2f);
                }
            }
        }
        public void CreateBuff(Pawn buffee, float severity)
        {
            if (buffee.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_SwanBuff))
            {
                buffee.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_SwanBuff).Severity = severity;
            } else {
                Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_SwanBuff, buffee);
                hediff.Severity = severity;
                buffee.health.AddHediff(hediff);
            }
        }
    }
    public class Hediff_PolarMigration : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsCaravanMember())
            {
                this.pawn.GetCaravan().Tile = this.pawn.GetCaravan().pather.Destination;
                this.pawn.GetCaravan().pather.StopDead();
            }
        }
    }
    public class Hediff_INeedMoreSteel : HediffWithComps
    {
        public override string Label
        {
            get
            {
                return base.Label + " (" + this.animalType.label + ")";
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.DetermineAnimalType();
        }
        public void DetermineAnimalType()
        {
            PawnKindDef oldType = this.animalType;
            this.animalType = (from pawnKind in DefDatabase<PawnKindDef>.AllDefsListForReading
                               where pawnKind.RaceProps.Animal && !pawnKind.RaceProps.Dryad && pawnKind.RaceProps.trainability.intelligenceOrder >= 20
                               select pawnKind).RandomElement();
            if (oldType != this.animalType && !this.spawnedAnimals.NullOrEmpty())
            {
                for (int i = this.spawnedAnimals.Count - 1; i >= 0; i--)
                {
                    this.spawnedAnimals[i].Destroy();
                    this.RemoveAnimal(this.spawnedAnimals[i]);
                }
            }
            this.Severity = this.def.initialSeverity;
            this.RecalculateMax();
        }
        public void RecalculateMax()
        {
            this.sumBodySize = 0f;
            foreach (Pawn p in this.spawnedAnimals)
            {
                this.sumBodySize += p.BodySize;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                this.RecalculateMax();
            }
            if (this.Severity == this.def.maxSeverity && this.pawn.SpawnedOrAnyParentSpawned && this.sumBodySize <= 2f * this.pawn.GetPsylinkLevel())
            {
                this.Severity = 0.001f;
                Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.animalType, this.pawn.Faction, PawnGenerationContext.NonPlayer, this.pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                if (newPawn != null)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        HealthUtility.FixWorstHealthCondition(newPawn);
                    }
                }
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.PositionHeld, this.pawn.MapHeld, 6, null);
                GenPlace.TryPlaceThing(newPawn, loc, this.pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                if (newPawn.training != null)
                {
                    this.TrainAnimal(newPawn);
                }
                if (newPawn.Faction != Faction.OfPlayerSilentFail)
                {
                    LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                }
                Hediff_WQBond hediff = (Hediff_WQBond)HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_WQBond, newPawn);
                hediff.creator = this.pawn;
                hediff.wqHediff = this;
                newPawn.health.AddHediff(hediff);
                this.spawnedAnimals.Add(newPawn);
                this.RecalculateMax();
            }
        }
        private void TrainAnimal(Pawn newPawn)
        {
            foreach (TrainableDef td in DefDatabase<TrainableDef>.AllDefs)
            {
                if (newPawn.training.CanAssignToTrain(td))
                {
                    newPawn.training.SetWantedRecursive(td, true);
                    newPawn.training.Train(td, null, true);
                }
            }
        }
        public void RemoveAnimal(Pawn pawn)
        {
            this.spawnedAnimals.Remove(pawn);
            this.RecalculateMax();
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos && this.animalType != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "HVT_DetermineNewAnimal".Translate(),
                    action = delegate
                    {
                        this.DetermineAnimalType();
                    }
                };
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.animalType, "animalType");
            Scribe_Values.Look<float>(ref this.sumBodySize, "sumBodySize", 0f, false);
            Scribe_Collections.Look<Pawn>(ref this.spawnedAnimals, "spawnedAnimals", LookMode.Reference, Array.Empty<object>());
        }
        private PawnKindDef animalType = PawnKindDefOf.Thrumbo;
        private float sumBodySize = 0f;
        private List<Pawn> spawnedAnimals = new List<Pawn>();
    }
    public class Hediff_WQBond : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.wqHediff != null)
            {
                this.wqHediff.RemoveAnimal(this.pawn);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.creator, "creator", false);
            Scribe_References.Look<Hediff_INeedMoreSteel>(ref this.wqHediff, "wqHediff", false);
        }
        public Pawn creator;
        public Hediff_INeedMoreSteel wqHediff;
    }
    public class Hediff_DragonsHoard : Hediff_PreDamageModification
    {
        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "HVT_DragonTooltip".Translate(this.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo));
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(2500))
            {
                for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.health.hediffSet.hediffs[i].def.makesSickThought || this.pawn.health.hediffSet.hediffs[i].Bleeding)
                    {
                        this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                    }
                }
            }
        }
    }
    public class Hediff_ThalassicGrandeur : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            PsychicAwakeningUtility.AwakenPsychicTalent(this.pawn, false, "HVT_WokeByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), false);
            if (PawnGenerator.IsBeingGenerated(this.pawn))
            {
                int bonusTranses = (int)(Rand.Value * 3);
                while (bonusTranses > 0)
                {
                    PsychicAwakeningUtility.AchieveTranscendence(this.pawn, "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), 0.25f, true, null, true, false, false);
                    bonusTranses--;
                }
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(200) && Rand.MTBEventOccurs(120f, 60000f, 200f))
            {
                PsychicAwakeningUtility.AchieveTranscendence(this.pawn,"HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(),0.25f,false,null,true,false,false);
            }
        }
    }
    public class Hediff_Censure : HediffWithComps
    {
        public override string TipStringExtra {
            get {
                return base.TipStringExtra + "HVT_ErinysCensureTooltip".Translate(((int)this.Severity).ToStringTicksToPeriod(true, true, true, true,true));
            } 
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.psychicEntropy != null)
            {
                this.pawn.psychicEntropy.OffsetPsyfocusDirectly(-100f);
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.pawn.psychicEntropy != null)
            {
                this.pawn.psychicEntropy.OffsetPsyfocusDirectly(-100f);
            }
        }
    }
    public class Hediff_LeeringSphinx : Hediff
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.Severity >= 2f)
            {
                this.pawn.Kill(null);
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.Severity >= 2f)
            {
                this.Severity = 1.99f;
            }
        }
    }
    public class Hediff_Seraphim : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.transferredOut == true && this.pawn.story != null)
            {
                this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph));
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.pawn.Faction != null && this.pawn.story != null && this.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph))
            {
                List<Pawn> eligiblePawns = new List<Pawn>();
                if (this.pawn.Map != null)
                {
                    foreach (Pawn p in this.pawn.Map.mapPawns.AllPawns)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else if (this.pawn.GetCaravan() != null) {
                    foreach (Pawn p in this.pawn.GetCaravan().PawnsListForReading)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (p.Faction != null && p.Faction == this.pawn.Faction && p.story != null)
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (p.Faction != null && p.Faction == this.pawn.Faction && p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
                if (eligiblePawns.Count > 0)
                {
                    List<Pawn> pawnsToRemove = new List<Pawn>();
                    if (eligiblePawns.Contains(this.pawn))
                    {
                        pawnsToRemove.Add(this.pawn);
                    }
                    foreach (Pawn p in eligiblePawns)
                    {
                        if (p.GetStatValue(StatDefOf.PsychicSensitivity) < 1E-45f || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
                        {
                            pawnsToRemove.Add(p);
                        }
                    }
                    foreach (Pawn p in pawnsToRemove)
                    {
                        pawnsToRemove.Remove(p);
                    }
                    if (eligiblePawns.Count > 0)
                    {
                        Pawn newHost = eligiblePawns.RandomElement();
                        if (!PsychicAwakeningUtility.IsAwakenedPsychic(newHost))
                        {
                            if (newHost.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                            {
                                PsychicAwakeningUtility.AwakenPsychicTalent(newHost, true, "HVT_WokeningDefault", "HVT_WokeningDefault");
                            }
                            List<TraitDef> awakenings = new List<TraitDef>();
                            foreach (Trait t in this.pawn.story.traits.allTraits)
                            {
                                if (PsychicAwakeningUtility.IsAwakenedTrait(t.def))
                                {
                                    awakenings.Add(t.def);
                                }
                            }
                            if (awakenings.Count > 0)
                            {
                                newHost.story.traits.GainTrait(new Trait(awakenings.RandomElement()));
                            }
                        }
                        newHost.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_TTraitSeraph));
                        this.transferredOut = true;
                        if (newHost.Faction == Faction.OfPlayerSilentFail)
                        {
                            LookTargets lt;
                            if (newHost.Spawned)
                            {
                                lt = newHost;
                            } else {
                                lt = null;
                            }
                            string how = HautsUtility.IsHighFantasy() ? "HVT_SeraphTextFantasy".Translate(newHost.Name.ToStringFull, this.pawn.Name.ToStringFull).CapitalizeFirst() : "HVT_SeraphText".Translate(newHost.Name.ToStringFull, this.pawn.Name.ToStringFull).CapitalizeFirst();
                            ChoiceLetter notification = LetterMaker.MakeLetter(
                    "HVT_SeraphLabel".Translate(newHost.Name.ToStringShort), how, LetterDefOf.PositiveEvent, lt, null, null, null);
                            Find.LetterStack.ReceiveLetter(notification, null);

                        }
                    }
                }
            }
        }
        public bool transferredOut = false;
    }
    public class Hediff_Wraithly : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.transferredOut == true && this.pawn.story != null)
            {
                List<Trait> traitsToRemove = new List<Trait>();
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    pawn.story.traits.RemoveTrait(t);
                }
                traitsToRemove.Clear();
                if (ModsConfig.BiotechActive && this.pawn.genes != null && geneToRemove != null)
                {
                    this.pawn.genes.RemoveGene(this.pawn.genes.GetGene(geneToRemove));
                }
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicAwakeningUtility.IsAwakenedTrait(t.def))
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    pawn.story.traits.RemoveTrait(t);
                }
                if (!PsychicAwakeningUtility.IsAwakenedPsychic(this.pawn))
                {
                    pawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic,PawnGenerator.RandomTraitDegree(HVTRoyaltyDefOf.HVT_LatentPsychic)));
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            this.Severity = 0.002f;
        }
        public bool transferredOut = false;
        public GeneDef geneToRemove = null;
    }
    public class Hediff_Woke : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.justResurrected = false;
        }
        public override void PostTick()
        {
            base.PostTick();
            this.justResurrected = false;
            if (this.pawn.IsHashIntervalTick(60000) && !this.pawn.IsMutant)
            {
                float mtb = 250f;
                if (ModsConfig.BiotechActive && this.pawn.genes != null)
                {
                    float denominator = 1f;
                    foreach (Gene g in this.pawn.genes.GenesListForReading)
                    {
                        denominator += g.def.biostatArc > 0 ? 1f : 0f;
                    }
                    mtb /= denominator;
                }
                if (Rand.MTBEventOccurs(mtb, 3600000f, 60000f))
                {
                    PsychicAwakeningUtility.AchieveTranscendence(this.pawn, "HVT_WokeningDefault".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeningDefaultFantasy".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), 0.25f, false, null, true, false, false);
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            if (PsychicAwakeningUtility.ShouldTranscend(pawn) && pawnToRez.story != null && !pawnToRez.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
            {
                if (Rand.Value <= 0.05f)
                {
                    this.justResurrected = true;
                    if (ResurrectionUtility.TryResurrect(pawnToRez))
                    {
                        PsychicAwakeningUtility.AchieveTranscendence(pawnToRez, "HVT_TransDeath".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_TransDeathFantasy".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), 0.2f, false);
                    }
                } else if (ModsConfig.IdeologyActive && this.pawn.Corpse != null && this.pawn.Corpse.Map != null) {
                    List<Thing> archonexi = this.pawn.Corpse.Map.listerThings.ThingsOfDef(ThingDefOf.ArchonexusCore);
                    if (archonexi.Count > 0)
                    {
                        if (ResurrectionUtility.TryResurrect(pawnToRez))
                        {
                            PsychicAwakeningUtility.AchieveTranscendence(pawnToRez, "HVT_TransArchoDeath".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_TransArchoDeathFantasy".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), 1f, false);
                        }
                    }
                }
            }
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            if (Rand.Value <= 0.3f && !this.justResurrected && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakeningAfterglow) && !pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
            {
                PsychicAwakeningUtility.AchieveTranscendence(pawn, "HVT_TransResurrection".Translate().CapitalizeFirst(), "HVT_TransResurrection".Translate().CapitalizeFirst(), 0.5f);
            }
        }
        private bool justResurrected;
    }
    public class HediffCompProperties_Transcendenceinator : HediffCompProperties_PsyfocusSpentTracker
    {
        public HediffCompProperties_Transcendenceinator()
        {
            this.compClass = typeof(HediffComp_Transcendenceinator);
        }
        public int ticksToReset;
        public List<string> aboutTransingStrings;
        public List<string> aboutTransingStringsMonolithRequired;
    }
    public class HediffComp_Transcendenceinator : HediffComp_PsyfocusSpentTracker
    {
        public new HediffCompProperties_Transcendenceinator Props
        {
            get
            {
                return (HediffCompProperties_Transcendenceinator)this.props;
            }
        }
        private void StartNewTimer()
        {
            if (this.ticksToNextReset <= 0)
            {
                this.ticksToNextReset = this.Props.ticksToReset;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.curPsyfocus = 0f;
            this.ticksToNextReset = 0;
            this.hasSentTransClueMessage = false;
        }
        public override void UpdatePsyfocusExpenditure(float offset)
        {
            base.UpdatePsyfocusExpenditure(offset);
            this.StartNewTimer();
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(180) && !this.hasSentTransClueMessage && HVT_Mod.settings.enableTranscendenceHints && this.Pawn.Faction != null && this.Pawn.Faction == Faction.OfPlayerSilentFail && this.Pawn.story != null && !PsychicAwakeningUtility.IsTranscendent(this.Pawn))
            {
                string letterText = "HVT_AboutTransingTextPrefix".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve();
                List<string> possibleTransMethods = this.Props.aboutTransingStrings;
                if (ModsConfig.AnomalyActive && Find.Anomaly.monolith != null)
                {
                    possibleTransMethods.AddRange(this.Props.aboutTransingStringsMonolithRequired);
                }
                letterText += "\n\n" + possibleTransMethods.RandomElement().CapitalizeFirst().Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true);
                Find.LetterStack.ReceiveLetter("HVT_AboutTransingLabel".Translate(), letterText, LetterDefOf.NeutralEvent, null, 0, true);
                this.hasSentTransClueMessage = true;
            }
            Pawn_PsychicEntropyTracker ppet = this.Pawn.psychicEntropy;
            if (ppet != null && ppet.IsCurrentlyMeditating)
            {
                this.StartNewTimer();
            }
            this.ticksToNextReset--;
            if (this.ticksToNextReset <= 0)
            {
                this.parent.Severity = this.parent.def.minSeverity;
                return;
            }
            if (ppet != null)
            {
                if (ppet.CurrentPsyfocus != this.curPsyfocus)
                {
                    this.parent.Severity += Math.Max(0f,ppet.CurrentPsyfocus-this.curPsyfocus);
                    this.curPsyfocus = ppet.CurrentPsyfocus;
                }
            }
            if (this.parent.Severity == this.parent.def.maxSeverity)
            {
                this.ticksToNextReset = 0;
                this.parent.Severity = this.parent.def.minSeverity;
                PsychicAwakeningUtility.AchieveTranscendence(this.Pawn, "HVT_TransPsyfocuser".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), "HVT_TransPsyfocuserFantasy".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), 1f);
                this.StartNewTimer();
                return;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextReset, "ticksToNextReset", 0, false);
            Scribe_Values.Look<float>(ref this.curPsyfocus, "curPsyfocus", 0f, false);
            Scribe_Values.Look<bool>(ref this.hasSentTransClueMessage, "hasSentTransClueMessage", false, false);
        }
        public int ticksToNextReset;
        public float curPsyfocus;
        public bool hasSentTransClueMessage = false;
    }
    public class RemovedOnAwakening : DefModExtension
    {
        public RemovedOnAwakening()
        {

        }
        public float awakenChance = -1f;
    }
    public class CompProperties_TranscendNearbyOnDeath : CompProperties
    {
        public CompProperties_TranscendNearbyOnDeath()
        {
            this.compClass = typeof(CompTranscendNearbyOnDeath);
        }
        public float chance;
        public float radius;
    }
    public class CompTranscendNearbyOnDeath : ThingComp
    {
        public CompProperties_TranscendNearbyOnDeath Props
        {
            get
            {
                return (CompProperties_TranscendNearbyOnDeath)this.props;
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if ((mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly) && previousMap != null && this.parent.SpawnedOrAnyParentSpawned && Rand.Chance(this.Props.chance))
            {
                foreach (Pawn p in previousMap.mapPawns.AllHumanlikeSpawned)
                {
                    if (PsychicAwakeningUtility.IsAwakenedPsychic(p) && p.Position.DistanceTo(this.parent.PositionHeld) <= this.Props.radius)
                    {
                        PsychicAwakeningUtility.AchieveTranscendence(p, "HVT_TransApocritonBeow".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVT_TransApocriton".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), 1f);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_LPMastery : HediffCompProperties
    {
        public HediffCompProperties_LPMastery()
        {
            this.compClass = typeof(HediffComp_LPMastery);
        }
        public int skillLevel;
        public int psylinkLevel;
    }
    public class HediffComp_LPMastery : HediffComp
    {
        public HediffCompProperties_LPMastery Props
        {
            get
            {
                return (HediffCompProperties_LPMastery)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Pawn.IsHashIntervalTick(60) && Current.ProgramState == ProgramState.Playing)
            {
                int gpl = this.Pawn.GetPsylinkLevel();
                if (gpl >= this.Props.psylinkLevel)
                {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(this.Pawn, 1, true, "HVT_WokeHighPsyLevel".Translate(gpl, this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")), "HVT_WokeHighPsyLevelFantasy".Translate(gpl, this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")));
                }
                for (int i = 0; i < this.Pawn.skills.skills.Count; i++)
                {
                    if (this.Pawn.skills.skills[i].GetLevel() >= this.Props.skillLevel)
                    {
                        PsychicAwakeningUtility.AwakenPsychicTalentCheck(this.Pawn, 1, true, "HVT_WokeSkillLevel".Translate(this.Pawn.skills.skills[i].def.defName.ToLower(), this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), "HVT_WokeSkillLevelFantasy".Translate(this.Pawn.skills.skills[i].def.defName.ToLower(), this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), false, 0f);
                        break;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_InflictTranscendence : HediffCompProperties
    {
        public HediffCompProperties_InflictTranscendence()
        {
            this.compClass = typeof(HediffComp_InflictTranscendence);
        }
        public float MTBDays;
    }
    public class HediffComp_InflictTranscendence : HediffComp
    {
        public HediffCompProperties_InflictTranscendence Props
        {
            get
            {
                return (HediffCompProperties_InflictTranscendence)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.IsHashIntervalTick(60) && Rand.MTBEventOccurs(this.Props.MTBDays, 60000f, 60f))
            {
                PsychicAwakeningUtility.AchieveTranscendence(this.parent.pawn, "HVT_TransArchogeneDelay".Translate(this.parent.pawn.Name.ToStringFull), "HVT_TransArchogeneDelayFantasy".Translate(this.parent.pawn.Name.ToStringFull), 0.55f);
                this.parent.Severity -= 1;
            }
        }
    }
    public class HediffCompProperties_Chanshi : HediffCompProperties_SatisfiesNeeds
    {
        public HediffCompProperties_Chanshi()
        {
            this.compClass = typeof(HediffComp_Chanshi);
        }
    }
    public class HediffComp_Chanshi : HediffComp_SatisfiesNeeds
    {
        public new HediffCompProperties_Chanshi Props
        {
            get
            {
                return (HediffCompProperties_Chanshi)this.props;
            }
        }
        public override bool ConditionsMetToSatisfyNeeds {
            get
            {
                return this.Pawn.psychicEntropy != null && this.Pawn.psychicEntropy.IsCurrentlyMeditating;
            }
        }
    }
    public class HediffCompProperties_ManaBarrier : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_ManaBarrier()
        {
            this.compClass = typeof(HediffComp_ManaBarrier);
        }
        public Dictionary<TraitDef,float> contributingTraits;
        public Dictionary<GeneDef,float> contributingGenes;
    }
    public class HediffComp_ManaBarrier : HediffComp_DamageNegation
    {
        public new HediffCompProperties_ManaBarrier Props
        {
            get
            {
                return (HediffCompProperties_ManaBarrier) this.props;
            }
        }
        public float ManaBarrierStrength
        {
            get
            {
                float manaBarrierStrength = 0f;
                if (this.Pawn.psychicEntropy != null && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
                {
                    if (this.Props.contributingTraits != null && this.Pawn.story != null)
                    {
                        foreach (Trait t in this.Pawn.story.traits.allTraits)
                        {
                            if (this.Props.contributingTraits.ContainsKey(t.def))
                            {
                                manaBarrierStrength += this.Props.contributingTraits.TryGetValue(t.def);
                            }
                        }
                    }
                    if (ModsConfig.BiotechActive && this.Props.contributingGenes != null && this.Pawn.genes != null)
                    {
                        foreach (GeneDef gd in this.Props.contributingGenes.Keys.ToList())
                        {
                            if (this.Pawn.genes.HasActiveGene(gd))
                            {
                                manaBarrierStrength += this.Props.contributingGenes.TryGetValue(gd);
                            }
                        }
                    }
                }
                return manaBarrierStrength;
            }
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            if (this.ManaBarrierStrength > 0f && this.Pawn.psychicEntropy.CurrentPsyfocus >= 0.01f)
            {
                float maxNegatableDamage = 100f * this.Pawn.psychicEntropy.CurrentPsyfocus*this.ManaBarrierStrength;
                float toughnessContribution = 1f;
                if (this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) < 1f && this.Props.shouldUseIncomingDamageFactor)
                {
                    toughnessContribution = 2f/(1f + this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor));
                    maxNegatableDamage *= toughnessContribution;
                }
                float actualNegationUsed = Math.Min(maxNegatableDamage,dinfo.Amount);
                dinfo.SetAmount(Math.Max(dinfo.Amount - maxNegatableDamage,0f));
                this.Pawn.psychicEntropy.OffsetPsyfocusDirectly(-actualNegationUsed / (this.ManaBarrierStrength * 100f * toughnessContribution));
                this.DoGraphics(dinfo,amount);
            }
        }
        public override void DoGraphics(DamageInfo dinfo, float amount)
        {
            if (this.Props.soundOnBlock != null && this.Pawn.Spawned)
            {
                this.Props.soundOnBlock.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            }
            if (amount > 0 && this.Pawn.SpawnedOrAnyParentSpawned)
            {
                Vector3 loc = this.Pawn.SpawnedParentOrMe.TrueCenter();
                {
                if (this.Props.fleckOnBlock != null)
                    FleckMaker.Static(loc, this.Pawn.MapHeld, this.Props.fleckOnBlock, 0.67f);
                }
                if (this.Props.throwDustPuffsOnBlock)
                {
                    int num2 = (int)Mathf.Min(10f, 2f + amount / 10f);
                    for (int i = 0; i < num2; i++)
                    {
                        FleckMaker.ThrowDustPuff(loc, this.Pawn.MapHeld, Rand.Range(0.8f, 1.2f));
                    }
                }
            }
        }
    }
    public class HediffCompProperties_MoteTranscendent : HediffCompProperties_MoteConditional
    {
        public HediffCompProperties_MoteTranscendent()
        {
            this.compClass = typeof(HediffComp_MoteTranscendent);
        }
        public bool mustBeTranscendent;
    }
    public class HediffComp_MoteTranscendent : HediffComp_MoteConditional
    {
        public new HediffCompProperties_MoteTranscendent Props
        {
            get
            {
                return (HediffCompProperties_MoteTranscendent)this.props;
            }
        }
        public override bool DisableMote()
        {
            return !HVT_Mod.settings.visibleTransEffect;
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(2500) && this.Props.mustBeTranscendent && !PsychicAwakeningUtility.IsTranscendent(this.Pawn))
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    public class HediffCompProperties_TechnopathicControl : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_TechnopathicControl()
        {
            this.compClass = typeof(HediffComp_TechnopathicControl);
        }
        public Dictionary<MechWeightClass, float> chancesPerWeight;
        public HediffDef hediff;
        public float baseSeverity = 1f;
    }
    public class HediffComp_TechnopathicControl : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_TechnopathicControl Props
        {
            get
            {
                return (HediffCompProperties_TechnopathicControl)this.props;
            }
        }
        public override float ChanceForVictim(Pawn victim)
        {
            return this.Props.chancesPerWeight.ContainsKey(victim.RaceProps.mechWeightClass) ? this.Props.chancesPerWeight.TryGetValue(victim.RaceProps.mechWeightClass) : 0f;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim.RaceProps.IsMechanoid && !victim.kindDef.isBoss)
            {
                if (this.Props.hediff != null && (this.Props.victimScalar == null || victim.GetStatValue(this.Props.victimScalar) > float.Epsilon))
                {
                    Hediff alreadyExtant = victim.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
                    float severity = this.ScaledValue(victim, this.Props.baseSeverity, valueToScale);
                    if (alreadyExtant != null)
                    {
                        alreadyExtant.Severity += severity;
                        if (alreadyExtant is Hediff_ApocritonControl ac)
                        {
                            ac.newFaction = this.parent.pawn.Faction;
                            ac.originalCaster = this.parent.pawn;
                        }
                    } else {
                        Hediff_ApocritonControl toAdd = (Hediff_ApocritonControl)HediffMaker.MakeHediff(this.Props.hediff, victim);
                        toAdd.newFaction = this.parent.pawn.Faction;
                        toAdd.originalCaster = this.parent.pawn;
                        toAdd.originalFaction = alreadyExtant is Hediff_ApocritonControl ac ? ac.originalFaction : victim.Faction;
                        toAdd.Severity = severity;
                        victim.health.AddHediff(toAdd);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_AndItsMyOwnHeart : HediffCompProperties_ExtraDamageOnHit
    {
        public HediffCompProperties_AndItsMyOwnHeart()
        {
            this.compClass = typeof(HediffComp_AndItsMyOwnHeart);
        }
        public List<HediffDef> hediffsToRemoveSelf;
    }
    public class HediffComp_AndItsMyOwnHeart : HediffComp_ExtraDamageOnHit
    {
        public new HediffCompProperties_AndItsMyOwnHeart Props
        {
            get
            {
                return (HediffCompProperties_AndItsMyOwnHeart)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(180))
            {
                for (int i = this.Pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff hediff = this.Pawn.health.hediffSet.hediffs[i];
                    if (this.Props.hediffsToRemoveSelf.Contains(hediff.def))
                    {
                        this.Pawn.health.RemoveHediff(hediff);
                    } else if (hediff.def.isBad && hediff.CurStage != null && hediff.CurStage.capMods != null) {
                        foreach (PawnCapacityModifier pcm in hediff.CurStage.capMods)
                        {
                            if (pcm.capacity == PawnCapacityDefOf.Consciousness && pcm.setMax <= 0.3f)
                            {
                                this.Pawn.health.RemoveHediff(hediff);
                            }
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_BigBrain : HediffCompProperties
    {
        public HediffCompProperties_BigBrain()
        {
            this.compClass = typeof(HediffComp_BigBrain);
        }
        public float researchPerHour;
        public float darkKnowledgePerHour;
        public float skillPerHour;
        public StatDef allButPsyfocusScalar;
        public float psyfocusPerHour;
    }
    public class HediffComp_BigBrain : HediffComp
    {
        public HediffCompProperties_BigBrain Props
        {
            get
            {
                return (HediffCompProperties_BigBrain)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(250))
            {
                bool didResearch = false;
                if (this.Pawn.Faction != null && this.Pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    if (Find.ResearchManager.GetProject(null) != null)
                    {
                        Find.ResearchManager.ResearchPerformed(this.Props.researchPerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) * Math.Max(0.08f,this.Pawn.GetStatValue(StatDefOf.ResearchSpeed)) / 0.0825f, this.Pawn);
                        didResearch = true;
                    } else if (ModsConfig.AnomalyActive && (Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Basic) != null || Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Advanced) != null)) {
                        Find.ResearchManager.ApplyKnowledge(KnowledgeCategoryDefOf.Advanced, this.Props.darkKnowledgePerHour*this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar)*Math.Max(0.08f,this.Pawn.GetStatValue(StatDefOf.EntityStudyRate))/10f);
                        didResearch = true;
                    }
                }
                if (!didResearch)
                {
                    if (this.Pawn.psychicEntropy != null)
                    {
                        this.Pawn.psychicEntropy.OffsetPsyfocusDirectly(this.Props.psyfocusPerHour/10f);
                    }
                    if (this.Pawn.skills != null)
                    {
                        SkillRecord sr = this.Pawn.skills.skills.RandomElement();
                        this.Pawn.skills.Learn(sr.def, this.Props.skillPerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) / 10f);
                    }
                }
                if (this.Pawn.IsHashIntervalTick(2500))
                {
                    HautsUtility.LearnLanguage(this.Pawn,null,0.05f*this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar));
                }
            }
        }
    }
    public class HediffCompProperties_HealPermanentWoundsPsyScaling : HediffCompProperties
    {
        public HediffCompProperties_HealPermanentWoundsPsyScaling()
        {
            this.compClass = typeof(HediffComp_HealPermanentWoundsPsyScaling);
        }
        public IntRange ticksToHeal;
    }
    public class HediffComp_HealPermanentWoundsPsyScaling : HediffComp
    {
        public HediffCompProperties_HealPermanentWoundsPsyScaling Props
        {
            get
            {
                return (HediffCompProperties_HealPermanentWoundsPsyScaling)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.ResetTicksToHeal();
        }
        private void ResetTicksToHeal()
        {
            if (this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
            {
                this.ticksToHeal = (int)(this.Props.ticksToHeal.RandomInRange / this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.ticksToHeal > 0)
            {
                this.ticksToHeal--;
            } else {
                HediffComp_HealPermanentWoundsPsyScaling.TryRegenerateInjury(base.Pawn, this.parent.LabelCap);
                this.ResetTicksToHeal();
            }
        }
        public static void TryRegenerateInjury(Pawn pawn, string cause)
        {
            Hediff hediff;
            if (!(from hd in pawn.health.hediffSet.hediffs
                  where hd.IsPermanent() || hd.def.hediffClass == typeof(Hediff_MissingPart) || hd.def.chronic
                  select hd).TryRandomElement(out hediff))
            {
                return;
            }
            if (hediff.def.hediffClass == typeof(Hediff_MissingPart))
            {
                pawn.health.RestorePart(hediff.Part);
            }
            else
            {
                HealthUtility.Cure(hediff);
            }
        }
        private int ticksToHeal;
    }
    public class HediffCompProperties_GlassSquid : HediffCompProperties_WaterImmersionSeverity
    {
        public HediffCompProperties_GlassSquid()
        {
            this.compClass = typeof(HediffComp_GlassSquid);
        }
        public float healPerSeverity;
    }
    public class HediffComp_GlassSquid : HediffComp_WaterImmersionSeverity
    {
        public new HediffCompProperties_GlassSquid Props
        {
            get
            {
                return (HediffCompProperties_GlassSquid)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(10))
            {
                this.bankedHealing += this.parent.Severity * this.Props.healPerSeverity;
                if (this.bankedHealing > 1f)
                {
                    this.bankedHealing -= 1f;
                    float heal = 1f;
                    List<Hediff> injuries = new List<Hediff>();
                    List<Hediff> missingParts = new List<Hediff>();
                    foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
                    {
                        if (h is Hediff_Injury)
                        {
                            injuries.Add(h);
                        } else if (h is Hediff_MissingPart && (h.Part.parent == null || (!this.Pawn.health.hediffSet.PartIsMissing(h.Part.parent) && !this.Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(h.Part.parent)))) {
                            missingParts.Add(h);
                        }
                    }
                    foreach (Hediff h in injuries)
                    {
                        float toHeal = Math.Min(h.Severity, heal);
                        h.Severity -= toHeal;
                        heal -= toHeal;
                        if (heal <= 0f)
                        {
                            return;
                        }
                    }
                    if (heal > 0f)
                    {
                        foreach (Hediff h in missingParts)
                        {
                            BodyPartRecord part = h.Part;
                            this.Pawn.health.RemoveHediff(h);
                            Hediff hediff5 = this.Pawn.health.AddHediff(HediffDefOf.Misc, part, null, null);
                            float partHealth = this.Pawn.health.hediffSet.GetPartHealth(part);
                            hediff5.Severity = Mathf.Max(partHealth - 1f, partHealth * 0.9f);
                            return;
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.bankedHealing, "bankedHealing", 0f, false);
        }
        public float bankedHealing;
    }
    public class HediffCompProperties_KaboomBaby : HediffCompProperties_InflictHediffOnHit
    {
        public HediffCompProperties_KaboomBaby()
        {
            this.compClass = typeof(HediffComp_KaboomBaby);
        }
        public float explosionRadius;
        public FloatRange explosionDmg;
        public DamageDef explosionType;
        public int explosionPeriodicity;
    }
    public class HediffComp_KaboomBaby : HediffComp_InflictHediffOnHit
    {
        public new HediffCompProperties_KaboomBaby Props
        {
            get
            {
                return (HediffCompProperties_KaboomBaby)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(this.Props.explosionPeriodicity) && this.Pawn.Spawned)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.explosionRadius, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (this.Pawn.HostileTo(p))
                    {
                        GenExplosion.DoExplosion(this.Pawn.Position, this.Pawn.Map, this.Props.explosionRadius, this.Props.explosionType, this.Pawn, (int)(this.Props.explosionDmg.RandomInRange), -1f);
                        break;
                    }
                }
            }
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim.Spawned)
            {
                foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(victim.Position, this.Props.explosionRadius, true))
                {
                    if (iv3.IsValid && GenSight.LineOfSight(victim.Position, iv3, victim.Map, true, null, 0, 0) && FilthMaker.TryMakeFilth(iv3, victim.Map, ThingDefOf.Filth_Fuel, 1, FilthSourceFlags.None, true))
                    {
                        continue;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_AuraOofOuchOwie : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_AuraOofOuchOwie()
        {
            this.compClass = typeof(HediffComp_AuraOofOuchOwie);
        }
        public HediffDef hediffFoe;
    }
    public class HediffComp_AuraOofOuchOwie : HediffComp_AuraHediff
    {
        public new HediffCompProperties_AuraOofOuchOwie Props
        {
            get
            {
                return (HediffCompProperties_AuraOofOuchOwie)this.props;
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (self.HostileTo(pawn))
            {
                if (pawn.health.hediffSet.TryGetHediff(this.Props.hediffFoe, out Hediff h))
                {
                    h.Severity += h.def.initialSeverity;
                } else {
                    pawn.health.AddHediff(this.Props.hediffFoe,null);
                }
            }
        }
    }
    public class HediffCompProperties_PupilOfTheGrave : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_PupilOfTheGrave()
        {
            this.compClass = typeof(HediffComp_PupilOfTheGrave);
        }
        public float corpseSearchRadius;
    }
    public class HediffComp_PupilOfTheGrave : HediffComp_DamageNegation
    {
        public new HediffCompProperties_PupilOfTheGrave Props
        {
            get
            {
                return (HediffCompProperties_PupilOfTheGrave)this.props;
            }
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            List<Thing> corpses = new List<Thing>();
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(this.Pawn.Position,this.Pawn.Map,this.Props.corpseSearchRadius,true).ToList())
            {
                if (t is Pawn p && ModsConfig.AnomalyActive && p.IsMutant)
                {
                    corpses.Add(p);
                } else if (t is Corpse c) {
                    corpses.Add(c);
                }
            }
            if (corpses.Count > 0)
            {
                float initialDamage = dinfo.Amount;
                base.DoModificationInner(ref dinfo, ref absorbed, amount);
                float removedDamage = Math.Max(0f,initialDamage - dinfo.Amount);
                while (removedDamage > 0 && corpses.Count > 0)
                {
                    Thing corpse = corpses.RandomElement();
                    Pawn p = corpse as Pawn;
                    float toTake = removedDamage;
                    if (corpse is Corpse c) {
                        toTake = Math.Min(removedDamage*75/c.InnerPawn.health.LethalDamageThreshold,c.HitPoints);
                    }
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(corpse.PositionHeld.ToVector3(), corpse.MapHeld, FleckDefOf.Smoke, 1f);
                    dataStatic.rotationRate = Rand.Range(-30f, 30f);
                    dataStatic.velocityAngle = (float)Rand.Range(30, 40);
                    dataStatic.velocitySpeed = Rand.Range(0.5f, 0.7f);
                    corpse.MapHeld.flecks.CreateFleck(dataStatic);
                    corpses.Remove(corpse);
                    corpse.TakeDamage(new DamageInfo(dinfo.Def,toTake,99f,-1f,dinfo.Instigator,p!=null? p.health.hediffSet.GetRandomNotMissingPart(dinfo.Def) : null,dinfo.Weapon,dinfo.Category));
                    removedDamage -= toTake;
                }
            }
        }
    }
    public class HediffCompProperties_PsiEmitter : HediffCompProperties_CreateThingsBySpendingSeverity
    {
        public HediffCompProperties_PsiEmitter()
        {
            this.compClass = typeof(HediffComp_PsiEmitter);
        }
    }
    public class HediffComp_PsiEmitter : HediffComp_CreateThingsBySpendingSeverity
    {
        public new HediffCompProperties_PsiEmitter Props
        {
            get
            {
                return (HediffCompProperties_PsiEmitter)this.props;
            }
        }
        public override void SpawnInRadius(Thing thing)
        {
            if (HiveUtility.TotalSpawnedHivesCount(this.parent.pawn.Map) < 150)
            {
                InfestationUtility.SpawnTunnels(1, this.parent.pawn.Map, true, true, null, CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 50, null), null);
            }
        }
    }
    public class HediffCompProperties_PsiDisruptor : HediffCompProperties_Aura
    {
        public HediffCompProperties_PsiDisruptor()
        {
            this.compClass = typeof(HediffComp_PsiDisruptor);
        }
    }
    public class HediffComp_PsiDisruptor : HediffComp_Aura
    {
        public new HediffCompProperties_PsiDisruptor Props
        {
            get
            {
                return (HediffCompProperties_PsiDisruptor)this.props;
            }
        }
        public override bool ValidatePawn(Pawn self, Pawn p, bool inCaravan)
        {
            return base.ValidatePawn(self, p, inCaravan) && p.RaceProps.Insect && !p.kindDef.isBoss;
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (self.HostileTo(pawn))
            {
                if (pawn.CurJob != null)
                {
                    pawn.jobs.StopAll(true);
                }
                if (pawn.InAggroMentalState)
                {
                    pawn.MentalState.RecoverFromState();
                }
                if (pawn.Spawned && pawn.GetLord() != null && pawn.GetLord().GetType() != typeof(LordJob_DefendPoint))
                {
                    pawn.SetFaction(null);
                    LordMaker.MakeNewLord(this.Pawn.Faction, new LordJob_DefendPoint(pawn.Position,12f,4f), this.Pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                }
            }
        }
    }
    public class HediffCompProperties_Kudzu : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_Kudzu()
        {
            this.compClass = typeof(HediffComp_Kudzu);
        }
        public float bonusPlantGrowth;
    }
    public class HediffComp_Kudzu : HediffComp_AuraHediff
    {
        public new HediffCompProperties_Kudzu Props
        {
            get
            {
                return (HediffCompProperties_Kudzu)this.props;
            }
        }
        public override void AffectSelf()
        {
            foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.FunctionalRange, true).OfType<Plant>().Distinct<Plant>())
            {
                if (!plant.Blighted)
                {
                    plant.Growth += (this.Props.bonusPlantGrowth*this.Props.tickPeriodicity*plant.GrowthRateFactor_Fertility) / (60000f * plant.def.plant.growDays);
                    plant.DirtyMapMesh(plant.Map);
                }
            }
        }
        public override bool ValidatePawn(Pawn self, Pawn p, bool inCaravan)
        {
            if (p.SpawnedOrAnyParentSpawned)
            {
                bool preValidation = false;
                if (p.PositionHeld.GetPlant(p.MapHeld) != null)
                {
                    preValidation = true;
                } else {
                    foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, 1.49f, true).OfType<Plant>().Distinct<Plant>())
                    {
                        if (plant.def.plant.IsTree)
                        {
                            preValidation = true;
                        }
                    }
                }
                if (preValidation)
                {
                    return base.ValidatePawn(self, p, inCaravan);
                }
            }
            return false;
        }
    }
    public class HediffCompProperties_Astrology : HediffCompProperties_BoredomAdjustment
    {
        public HediffCompProperties_Astrology()
        {
            this.compClass = typeof(HediffComp_Astrology);
        }
        public float likelihood;
    }
    public class HediffComp_Astrology : HediffComp_BoredomAdjustment
    {
        public new HediffCompProperties_Astrology Props
        {
            get
            {
                return (HediffCompProperties_Astrology)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks) && Rand.Chance(this.Props.likelihood))
            {
                if (this.Pawn.needs.joy != null)
                {
                    foreach (JoyKindDef jkd in this.Props.boredoms.Keys)
                    {
                        float tolerance = this.Pawn.needs.joy.tolerances[jkd];
                        if (tolerance > 0f)
                        {
                            PsychicAwakeningUtility.MakeGoodEvent(this.Pawn);
                            return;
                        }
                    }
                }
            }
            base.CompPostTick(ref severityAdjustment);
        }
    }
    public class HediffCompProperties_AuraAllelopathy : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraAllelopathy()
        {
            this.compClass = typeof(HediffComp_AuraAllelopathy);
        }
        public float damage;
        public DamageDef damageType;
        public float lifeLeech;
        public float psyfocusLeech;
    }
    public class HediffComp_AuraAllelopathy : HediffComp_Aura
    {
        public new HediffCompProperties_AuraAllelopathy Props
        {
            get
            {
                return (HediffCompProperties_AuraAllelopathy)this.props;
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (pawn.psychicEntropy != null && self.psychicEntropy != null && self.psychicEntropy.CurrentPsyfocus < 1f)
            {
                float leech = Math.Min(Math.Min(this.Props.psyfocusLeech,pawn.psychicEntropy.CurrentPsyfocus),1f-self.psychicEntropy.CurrentPsyfocus);
                pawn.psychicEntropy.OffsetPsyfocusDirectly(-leech);
                if (leech > 0f)
                {
                    self.psychicEntropy.OffsetPsyfocusDirectly(leech);
                }
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            List<Hediff> healables = new List<Hediff>();
            foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury)
                {
                    healables.Add(h);
                } else if (h is HediffWithComps hwc) {
                    HediffComp_Immunizable hci = hwc.TryGetComp<HediffComp_Immunizable>();
                    if (hci != null)
                    {
                        healables.Add(h);
                    }
                }
            }
            if (healables.Count > 0)
            {
                List<Plant> plants = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.range, true).OfType<Plant>().Distinct<Plant>().ToList<Plant>();
                while (healables.Count > 0 && plants.Count > 0)
                {
                    Plant plant = plants.RandomElement();
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(plant.PositionHeld.ToVector3(), plant.MapHeld, FleckDefOf.Smoke, 1f);
                    dataStatic.rotationRate = Rand.Range(-30f, 30f);
                    dataStatic.velocityAngle = (float)Rand.Range(30, 40);
                    dataStatic.velocitySpeed = Rand.Range(0.5f, 0.7f);
                    plant.MapHeld.flecks.CreateFleck(dataStatic);
                    plant.TakeDamage(new DamageInfo(this.Props.damageType, this.Props.damage));
                    Hediff h2 = healables.RandomElement();
                    h2.Severity -= this.Props.lifeLeech;
                    if (h2.ShouldRemove)
                    {
                        this.Pawn.health.RemoveHediff(h2);
                        healables.Remove(h2);
                    }
                    plants.Remove(plant);
                }
            }
        }
    }
    public class HediffCompProperties_AuraLadybug : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_AuraLadybug()
        {
            this.compClass = typeof(HediffComp_AuraLadybug);
        }
        public List<ThingDef> exemptFilthTypes;
    }
    public class HediffComp_AuraLadybug : HediffComp_AuraHediff
    {
        public new HediffCompProperties_AuraLadybug Props
        {
            get
            {
                return (HediffCompProperties_AuraLadybug)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            foreach (Filth filth in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.range, true).OfType<Filth>().Distinct<Filth>())
            {
                if (!this.Props.exemptFilthTypes.Contains(filth.def))
                {
                    filth.ThinFilth();
                }
            }
        }
    }
    public class HediffCompProperties_Leghorn : HediffCompProperties_ForcedByOtherProperty
    {
        public HediffCompProperties_Leghorn()
        {
            this.compClass = typeof(HediffComp_Leghorn);
        }
        public ThingDef mealDef;
    }
    public class HediffComp_Leghorn : HediffComp_ForcedByOtherProperty
    {
        public new HediffCompProperties_Leghorn Props
        {
            get
            {
                return (HediffCompProperties_Leghorn)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity == this.parent.def.maxSeverity)
            {
                if (this.Pawn.Spawned || this.Pawn.GetCaravan() != null)
                {
                    this.parent.Severity = 0.001f;
                    int mealsToPlace = Math.Min(Math.Max((int)(2 * this.Pawn.GetPsylinkLevel() * this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity)), 1), 100);
                    while (mealsToPlace > 0)
                    {
                        Thing fineMeal = ThingMaker.MakeThing(this.Props.mealDef);
                        fineMeal.stackCount = Math.Min(fineMeal.def.stackLimit, mealsToPlace);
                        mealsToPlace -= fineMeal.stackCount;
                        if (this.Pawn.Spawned)
                        {
                            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 6, null);
                            GenPlace.TryPlaceThing(fineMeal, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
                            FleckMaker.AttachedOverlay(fineMeal, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                            FleckMaker.AttachedOverlay(fineMeal, FleckDefOf.PsycastSkipOuterRingExit, Vector3.zero, 1f, -1f);
                            fineMeal.Notify_DebugSpawned();
                        }
                        else if (this.Pawn.GetCaravan() != null)
                        {
                            this.Pawn.GetCaravan().AddPawnOrItem(fineMeal, true);
                        }
                    }
                    if (this.Pawn.Spawned)
                    {
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                    }
                }
            }
        }
    }
    public class HediffCompProperties_AuraMeerkat : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_AuraMeerkat()
        {
            this.compClass = typeof(HediffComp_AuraMeerkat);
        }
    }
    public class HediffComp_AuraMeerkat : HediffComp_AuraHediff
    {
        public new HediffCompProperties_AuraMeerkat Props
        {
            get
            {
                return (HediffCompProperties_AuraMeerkat)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.Pawn))
            {
                if (this.Pawn.skills != null)
                {
                    foreach (SkillRecord s in this.Pawn.skills.skills)
                    {
                        if (!s.TotallyDisabled)
                        {
                            s.Level += (int)(Rand.Value * 5);
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_SkillCopier : HediffCompProperties_Aura
    {
        public HediffCompProperties_SkillCopier()
        {
            this.compClass = typeof(HediffComp_SkillCopier);
        }
        public int baseXPgainPerPeriod = 2;
        public StatDef effectScalar;
    }
    public class HediffComp_SkillCopier : HediffComp_Aura
    {
        public new HediffCompProperties_SkillCopier Props
        {
            get
            {
                return (HediffCompProperties_SkillCopier)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.Pawn))
            {
                if (this.Pawn.skills != null)
                {
                    foreach (SkillRecord s in this.Pawn.skills.skills)
                    {
                        if (!s.TotallyDisabled)
                        {
                            s.Level += (int)(Rand.Value * 5);
                        }
                    }
                }
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            if (self.skills != null && pawn.skills != null)
            {
                base.AffectPawn(self, pawn);
                foreach (SkillRecord s in self.skills.skills)
                {
                    if (!s.TotallyDisabled)
                    {
                        float lvlDiff = 1f + pawn.skills.GetSkill(s.def).Level - s.Level;
                        if (lvlDiff > 0)
                        {
                            s.Learn(lvlDiff*this.Props.baseXPgainPerPeriod*self.GetStatValue(this.Props.effectScalar),true);
                        }
                    }
                }
                if (pawn.Faction != null)
                {
                    HautsUtility.LearnLanguage(self, pawn, 0.015f*self.GetStatValue(this.Props.effectScalar));
                }
            }
        }
    }
    public class HediffCompProperties_Mosquito : HediffCompProperties_InflictHediffOnHit
    {
        public HediffCompProperties_Mosquito()
        {
            this.compClass = typeof(HediffComp_Mosquito);
        }
        public float lifestealEfficiency = 1f;
        public float hemogenGainEfficiency = 1f;
    }
    public class HediffComp_Mosquito : HediffComp_InflictHediffOnHit
    {
        public new HediffCompProperties_Mosquito Props
        {
            get
            {
                return (HediffCompProperties_Mosquito)this.props;
            }
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            float remainingBlood = victim.health.hediffSet.HasHediff(this.Props.hediff) ? Math.Max(0f, this.Props.canOnlyIncreaseSeverityUpTo - victim.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff).Severity): this.Props.canOnlyIncreaseSeverityUpTo;
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim != this.parent.pawn)
            {
                List<Hediff_Injury> source = new List<Hediff_Injury>();
                this.parent.pawn.health.hediffSet.GetHediffs<Hediff_Injury>(ref source, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
                float lifeStolen = Math.Min(remainingBlood, this.ScaledValue(victim, this.Props.baseSeverity, valueToScale)) * this.Props.lifestealEfficiency;
                if (source.TryRandomElement(out Hediff_Injury hediff_Injury))
                {
                    hediff_Injury.Heal(100f*lifeStolen);
                }
                lifeStolen *= this.Props.hemogenGainEfficiency;
                GeneUtility.OffsetHemogen(this.parent.pawn, lifeStolen, true);
                GeneUtility.OffsetHemogen(victim, -lifeStolen, true);
            }
        }
    }
    public class HediffCompProperties_KopiLuwak : HediffCompProperties_CreateThingsBySpendingSeverity
    {
        public HediffCompProperties_KopiLuwak()
        {
            this.compClass = typeof(HediffComp_KopiLuwak);
        }
    }
    public class HediffComp_KopiLuwak : HediffComp_CreateThingsBySpendingSeverity
    {
        public new HediffCompProperties_KopiLuwak Props
        {
            get
            {
                return (HediffCompProperties_KopiLuwak)this.props;
            }
        }
        public override KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            if (Rand.Chance(2f/(this.Props.spawnedThingAndCountPerTrigger.Count+2f)))
            {
                ThingDef tDef;
                if (Rand.Chance(0.5f))
                {
                    tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                         where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.NeurotrainersPsycast)
                                         select tdef).RandomElement();
                } else {
                    tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                            where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.NeurotrainersSkill)
                            select tdef).RandomElement();
                }
                return new KeyValuePair<ThingDef, FloatRange>(tDef,new FloatRange(1f));
            }
            return base.GetThingToSpawn();
        }
    }
    public class HediffCompProperties_Oxpeck : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_Oxpeck()
        {
            this.compClass = typeof(HediffComp_Oxpeck);
        }
        public HediffDef hediffNice;
        public HediffDef hediffMean;
        public float baseSeverity;
        public float tameMTBdaysBase;
        public SimpleCurve tameMTBcurve;
    }
    public class HediffComp_Oxpeck : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_Oxpeck Props
        {
            get
            {
                return (HediffCompProperties_Oxpeck)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(150) && this.Props.tameMTBdaysBase > 0 && Rand.MTBEventOccurs(this.Props.tameMTBdaysBase/this.Props.tameMTBcurve.Evaluate(this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity)), 60000f, 150f))
            {
                if (this.Pawn.Spawned)
                {
                    bool addAnimal = true;
                    if (Rand.Chance(0.9f))
                    {
                        List<Pawn> animals = new List<Pawn>();
                        foreach (Pawn p in this.Pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (TameUtility.CanTame(p) && !p.Downed && !p.Position.Fogged(p.Map) && p.RaceProps.wildness > 0f)
                            {
                                animals.Add(p);
                            }
                        }
                        if (animals.Count > 0)
                        {
                            addAnimal = false;
                            this.TameAnimal(animals.RandomElement());
                        }
                    }
                    if (addAnimal) {
                        Map map = this.Pawn.Map;
                        IntVec3 loc;
                        if (RCellFinder.TryFindRandomPawnEntryCell(out loc, map, CellFinder.EdgeRoadChance_Animal, true, (IntVec3 cell) => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false, false, false).WithFenceblocked(true))))
                        {
                            PawnKindDef pawnKindDef;
                            if (map.Biome.AllWildAnimals.Where((PawnKindDef a) => map.mapTemperature.SeasonAcceptableFor(a.race, 0f)).TryRandomElementByWeight((PawnKindDef def) => this.CommonalityOfAnimalNow(map, def), out pawnKindDef))
                            {
                                IntVec3 iv3 = CellFinder.RandomClosewalkCellNear(loc, map, 4, null);
                                Pawn animal = (Pawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(pawnKindDef, null), iv3, map, WipeMode.Vanish);
                                this.TameAnimal(animal);
                            }
                        }
                    }
                } else {
                    Caravan c = this.Pawn.GetCaravan();
                    if (c != null)
                    {
                        BiomeDef b = Find.WorldGrid[c.Tile].biome;
                        if (b != null)
                        {
                            PawnKindDef pawnKindDef;
                            if (b.AllWildAnimals.TryRandomElementByWeight((PawnKindDef def) => this.CommonalityOfAnimalNow(c, b, def), out pawnKindDef))
                            {
                                Pawn animal = PawnGenerator.GeneratePawn(pawnKindDef, null);
                                c.AddPawn(animal,true);
                                Find.WorldPawns.PassToWorld(animal, PawnDiscardDecideMode.Decide);
                                this.TameAnimal(animal);
                            }
                        }
                    }
                }
            }
        }
        public float CommonalityOfAnimalNow(Map map, PawnKindDef def)
        {
            return ((ModsConfig.BiotechActive && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[map.Tile].pollution)) ? map.Biome.CommonalityOfPollutionAnimal(def) : map.Biome.CommonalityOfAnimal(def)) / def.wildGroupSize.Average;
        }
        public float CommonalityOfAnimalNow(Caravan caravan, BiomeDef b, PawnKindDef def)
        {
            return ((ModsConfig.BiotechActive && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[caravan.Tile].pollution)) ? b.CommonalityOfPollutionAnimal(def) : b.CommonalityOfAnimal(def)) / def.wildGroupSize.Average;
        }
        public void TameAnimal(Pawn pawn)
        {
            string text = pawn.LabelIndefinite();
            bool flag = pawn.Name != null;
            pawn.SetFaction(Faction.OfPlayer, null);
            string text2;
            if (!flag && pawn.Name != null)
            {
                if (pawn.Name.Numerical)
                {
                    text2 = "LetterAnimalSelfTameAndNameNumerical".Translate(text, pawn.Name.ToStringFull, pawn.Named("ANIMAL")).CapitalizeFirst();
                }
                else
                {
                    text2 = "LetterAnimalSelfTameAndName".Translate(text, pawn.Name.ToStringFull, pawn.Named("ANIMAL")).CapitalizeFirst();
                }
            } else {
                text2 = "LetterAnimalSelfTame".Translate(pawn).CapitalizeFirst();
            }
            ChoiceLetter notification = LetterMaker.MakeLetter(
            "LetterLabelAnimalSelfTame".Translate(pawn.KindLabel, pawn).CapitalizeFirst(), text2, LetterDefOf.PositiveEvent, pawn, null, null, null);
            Find.LetterStack.ReceiveLetter(notification, null);
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            Hediff hediff = HediffMaker.MakeHediff(this.Pawn.HostileTo(victim) ? this.Props.hediffMean : this.Props.hediffNice, victim);
            hediff.Severity = this.Props.baseSeverity;
            victim.health.AddHediff(hediff);
        }
    }
    public class HediffCompProperties_PitohuiPurgeOnHit : HediffCompProperties_CureHediffsOnHit
    {
        public HediffCompProperties_PitohuiPurgeOnHit()
        {
            this.compClass = typeof(HediffComp_PitohuiPurgeOnHit);
        }
    }
    public class HediffComp_PitohuiPurgeOnHit : HediffComp_CureHediffsOnHit
    {
        public new HediffCompProperties_PitohuiPurgeOnHit Props
        {
            get
            {
                return (HediffCompProperties_PitohuiPurgeOnHit)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.pawn.IsHashIntervalTick(200))
            {
                this.PurgePitohuiToxins(this.parent.pawn, this.parent.pawn);
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast)
            {
                List<LocalTargetInfo> targets = vca.ability.GetAffectedTargets(target).ToList();
                foreach (LocalTargetInfo lti in targets)
                {
                    if (lti.Pawn != null && !this.parent.pawn.HostileTo(lti.Pawn))
                    {
                        this.PurgePitohuiToxins(lti.Pawn, this.parent.pawn);
                    }
                }
            }
            else if (verb is VFECore.Abilities.Verb_CastAbility vcavfe && HautsUtility.IsVPEPsycast(vcavfe.ability))
            {
                GlobalTargetInfo[] targets = new GlobalTargetInfo[]
                {
                        target.ToGlobalTargetInfo(vcavfe.Caster.Map)
                };
                vcavfe.ability.ModifyTargets(ref targets);
                foreach (LocalTargetInfo lti in targets)
                {
                    if (lti.Pawn != null && !this.parent.pawn.HostileTo(lti.Pawn))
                    {
                        this.PurgePitohuiToxins(lti.Pawn, this.parent.pawn);
                    }
                }
            }
        }
        public void PurgePitohuiToxins(Pawn pawn, Pawn pitohui)
        {
            base.DoExtraEffects(pawn,1f);
            Hediff hediff1 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.DrugOverdose, false);
            if (hediff1 != null)
            {
                pawn.health.hediffSet.hediffs.Remove(hediff1);
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiOD".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                } else {
                    Messages.Message("HVT_PitohuiODother".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            Hediff hediff2 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning, false);
            if (hediff2 != null)
            {
                pawn.health.hediffSet.hediffs.Remove(hediff2);
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiFP".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                } else {
                    Messages.Message("HVT_PitohuiFPother".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            List<Hediff> hediffsToRemove = new List<Hediff>();
            bool removedDependency = false;
            bool removedAddiction = false;
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h as Hediff_ChemicalDependency != null)
                {
                    hediffsToRemove.Add(h);
                    removedDependency = true;
                } else if (h as Hediff_Addiction != null) {
                    hediffsToRemove.Add(h);
                    removedAddiction = true;
                }
            }
            foreach (Hediff h in hediffsToRemove)
            {
                pawn.health.hediffSet.hediffs.Remove(h);
            }
            if (removedDependency)
            {
                string how = HautsUtility.IsHighFantasy() ? "HVT_PitohuiGenDepFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : "HVT_PitohuiGenDep".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                Messages.Message(how, pawn, MessageTypeDefOf.PositiveEvent, true);
            }
            if (removedAddiction)
            {
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiAdd".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                } else {
                    Messages.Message("HVT_PitohuiAddOther".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    public class HediffCompProperties_AuraPtar : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraPtar()
        {
            this.compClass = typeof(HediffComp_AuraPtar);
        }
        public HediffDef hediffGood;
        public float minGoodSeverity;
        public HediffDef hediffBad;
        public float maxBadSeverity;
    }
    public class HediffComp_AuraPtar : HediffComp_Aura
    {
        public new HediffCompProperties_AuraPtar Props
        {
            get
            {
                return (HediffCompProperties_AuraPtar)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            if (this.parent.Severity >= this.Props.minGoodSeverity)
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffGood, this.parent.pawn, null);
                this.parent.pawn.health.AddHediff(hediff, null);
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (this.parent.Severity >= this.Props.minGoodSeverity && !this.parent.pawn.HostileTo(pawn))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffGood, pawn, null);
                pawn.health.AddHediff(hediff, null);
            } else if (this.parent.Severity <= this.Props.maxBadSeverity && this.parent.pawn.HostileTo(pawn)) {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffBad, pawn, null);
                pawn.health.AddHediff(hediff, null);
            }
        }
    }
    public class HediffCompProperties_AnimalImmunity : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_AnimalImmunity()
        {
            this.compClass = typeof(HediffComp_AnimalImmunity);
        }
        public bool onlyInsectoids = false;
    }
    public class HediffComp_AnimalImmunity : HediffComp_DamageNegation
    {
        public new HediffCompProperties_AnimalImmunity Props
        {
            get
            {
                return (HediffCompProperties_AnimalImmunity)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return base.ShouldDoEffect(dinfo) && dinfo.Instigator != null && dinfo.Instigator is Pawn p && p.RaceProps.Animal && (!this.Props.onlyInsectoids || p.RaceProps.Insect);
        }
    }
    public class HediffCompProperties_NeuralHeatShield : HediffCompProperties
    {
        public override void PostLoad()
        {
            base.PostLoad();
            ShieldsSystem.ApplyDrawPatches();
        }
        public HediffCompProperties_NeuralHeatShield()
        {
            this.compClass = typeof(HediffComp_NeuralHeatShield);
        }
        public float displayUnderSeverity = 1f;
        public float drawSize = 1f;
        public Color color = new Color(1f, 1f, 1f, 1f);
    }
    public class HediffComp_NeuralHeatShield : HediffComp_Draw
    {
        public HediffCompProperties_NeuralHeatShield Props
        {
            get
            {
                return (HediffCompProperties_NeuralHeatShield)this.props;
            }
        }
        protected bool ShouldDisplay
        {
            get
            {
                return this.Pawn.Spawned && !this.Pawn.Dead && !this.Pawn.Downed && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && this.parent.Severity <= this.Props.displayUnderSeverity;
            }
        }
        public override void DrawAt(Vector3 drawPos)
        {
            if (this.ShouldDisplay)
            {
                float num = this.Props.drawSize;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                int num2 = Find.TickManager.TicksGame - this.lastAbsorbDamageTick;
                if (num2 < 8)
                {
                    float num3 = (float)(8 - num2) / 8f * 0.05f;
                    num -= num3;
                }
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default;
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, this.Props.color), 0);
            }
        }
        private int lastAbsorbDamageTick = -9999;
    }
    public class HediffCompProperties_Scarab : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_Scarab()
        {
            this.compClass = typeof(HediffComp_Scarab);
        }
        public HediffDef switchToOnBreak;
        public float percentNeuralHeatOnHit;
    }
    public class HediffComp_Scarab : HediffComp_DamageNegation
    {
        public new HediffCompProperties_Scarab Props
        {
            get
            {
                return (HediffCompProperties_Scarab)this.props;
            }
        }
        public override void PayCostOfHit(float damageAmount)
        {
            this.Pawn.psychicEntropy.TryAddEntropy((damageAmount * this.Props.severityOnHit) + (this.Pawn.psychicEntropy.MaxEntropy * this.Props.percentNeuralHeatOnHit), null, true, true);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.psychicEntropy != null)
            {
                this.parent.Severity = this.Pawn.psychicEntropy.EntropyRelativeValue;
                if (this.parent.Severity >= 1f)
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.switchToOnBreak, this.Pawn);
                    this.Pawn.health.AddHediff(hediff, this.parent.Part);
                    hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                    this.Pawn.health.RemoveHediff(hediff);
                }
            }
        }
        public override bool ShouldDoModificationInner(DamageInfo dinfo)
        {
            return base.ShouldDoModificationInner(dinfo) && this.Pawn.psychicEntropy != null && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon;
        }
    }
    public class HediffCompProperties_InASwaddle : HediffCompProperties_PsyfocusSpentTracker
    {
        public HediffCompProperties_InASwaddle()
        {
            this.compClass = typeof(HediffComp_InASwaddle);
        }
        public float severityToTrigger;
    }
    public class HediffComp_InASwaddle : HediffComp_PsyfocusSpentTracker
    {
        public new HediffCompProperties_InASwaddle Props
        {
            get
            {
                return (HediffCompProperties_InASwaddle)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                return base.CompTipStringExtra + (HautsUtility.IsHighFantasy() ? "Hauts_PsyfocusSpentTrackerTooltipFantasy".Translate(this.parent.Severity, this.Props.severityToTrigger) : "Hauts_PsyfocusSpentTrackerTooltip".Translate(this.parent.Severity, this.Props.severityToTrigger));
            }
        }
        public override void UpdatePsyfocusExpenditure(float offset)
        {
            base.UpdatePsyfocusExpenditure(offset);
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                while (this.parent.Severity >= this.Props.severityToTrigger)
                {
                    Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.Pawn.kindDef, this.Pawn.Faction, PawnGenerationContext.NonPlayer, this.Pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                    if (newPawn != null)
                    {
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in newPawn.story.traits.allTraits)
                        {
                            if (PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove)
                        {
                            newPawn.story.traits.RemoveTrait(t);
                        }
                        List<Gene> genesToRemove = new List<Gene>();
                        if (ModsConfig.BiotechActive && newPawn.genes != null)
                        {
                            foreach (Gene g in newPawn.genes.GenesListForReading)
                            {
                                if (PsychicAwakeningUtility.IsAwakenedPsychicGene(g.def))
                                {
                                    genesToRemove.Add(g);
                                }
                            }
                        }
                        foreach (Gene g in genesToRemove)
                        {
                            newPawn.genes.RemoveGene(g);
                        }
                        for (int j = 0; j < 5; j++)
                        {
                            HealthUtility.FixWorstHealthCondition(newPawn);
                        }
                        if (ModsConfig.IdeologyActive && newPawn.ideo != null && this.Pawn.ideo != null)
                        {
                            newPawn.ideo.SetIdeo(this.Pawn.ideo.Ideo);
                        }
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.PositionHeld, this.Pawn.MapHeld, 6, null);
                        GenPlace.TryPlaceThing(newPawn, loc, this.Pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                        FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                        DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                        if (!newPawn.IsColonistPlayerControlled)
                        {
                            LordMaker.MakeNewLord(this.Pawn.Faction, new LordJob_EscortPawn(this.Pawn), this.Pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                        }
                    }
                    this.parent.Severity -= this.Props.severityToTrigger;
                }
            }
        }
    }
    public class HediffCompProperties_ThoughtsInChaos : HediffCompProperties_MentalStateOnHit
    {
        public HediffCompProperties_ThoughtsInChaos()
        {
            this.compClass = typeof(HediffComp_ThoughtsInChaos);
        }
        public float chancePerVictimPsylink;
        public float psylinkChanceMin;
    }
    public class HediffComp_ThoughtsInChaos : HediffComp_MentalStateOnHit
    {
        public new HediffCompProperties_ThoughtsInChaos Props
        {
            get
            {
                return (HediffCompProperties_ThoughtsInChaos)this.props;
            }
        }
        public override float ChanceForVictim(Pawn victim)
        {
            return Math.Max(this.Props.psylinkChanceMin, Math.Min(1f,base.ChanceForVictim(victim)) + (victim.GetPsylinkLevel() * this.Props.chancePerVictimPsylink));
        }
    }
    public class HediffCompProperties_TMN : HediffCompProperties_PreDamageModification
    {
        public HediffCompProperties_TMN()
        {
            this.compClass = typeof(HediffComp_TMN);
        }
        public HediffDef hediffSelf;
    }
    public class HediffComp_TMN : HediffComp_PreDamageModification
    {
        public new HediffCompProperties_TMN Props
        {
            get
            {
                return (HediffCompProperties_TMN)this.props;
            }
        }
        public override void TryDoModification(ref DamageInfo dinfo, ref bool absorbed)
        {
            this.Pawn.health.AddHediff(this.Props.hediffSelf,null);
            base.TryDoModification(ref dinfo, ref absorbed);
        }
    }
    public class HediffCompProperties_WithATasteOfYourLips : HediffCompProperties_DamageRetaliation
    {
        public HediffCompProperties_WithATasteOfYourLips()
        {
            this.compClass = typeof(HediffComp_WithATasteOfYourLips);
        }
    }
    public class HediffComp_WithATasteOfYourLips : HediffComp_DamageRetaliation
    {
        public new HediffCompProperties_WithATasteOfYourLips Props
        {
            get
            {
                return (HediffCompProperties_WithATasteOfYourLips)this.props;
            }
        }
        public override float RetaliationRange => base.RetaliationRange* Math.Max(2f, this.Pawn.GetPsylinkLevel());
    }
    public class HediffCompProperties_CarrionSpawn : HediffCompProperties
    {
        public HediffCompProperties_CarrionSpawn()
        {
            this.compClass = typeof(HediffComp_CarrionSpawn);
        }
        public float severityToTrigger;
        public bool setToOwnFaction = false;
        public float spawnRadius;
        public bool showProgressInTooltip = true;
        public float humanlikeChance;
        public FleckDef spawnFleck1;
        public FleckDef spawnFleck2;
        public SoundDef spawnSound;
    }
    public class HediffComp_CarrionSpawn : HediffComp
    {
        public HediffCompProperties_CarrionSpawn Props
        {
            get
            {
                return (HediffCompProperties_CarrionSpawn)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity, this.Props.severityToTrigger);
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity >= this.Props.severityToTrigger && this.Pawn.Spawned)
            {
                this.SpawnThings();
            }
        }
        public void SpawnThings()
        {
            List<PawnKindDef> corpseList = new List<PawnKindDef>();
            float humanlikeChance = Rand.Value;
            foreach (PawnKindDef pkd in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (humanlikeChance <= this.Props.humanlikeChance || !pkd.race.race.Humanlike)
                {
                    corpseList.Add(pkd);
                }
            }
            PawnKindDef kind = corpseList.RandomElement();
            Pawn pawn = PawnGenerator.GeneratePawn(kind, this.Props.setToOwnFaction ? this.Pawn.Faction : null);
            pawn.health.SetDead();
            if (pawn.apparel != null)
            {
                pawn.apparel.DestroyAll(DestroyMode.Vanish);
            }
            if (pawn.equipment != null)
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            }
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            Corpse corpse = pawn.MakeCorpse(null, null);
            corpse.Age = Mathf.RoundToInt((float)(Rand.Value * 60000000));
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, (int)Math.Ceiling(this.Props.spawnRadius), null);
            GenPlace.TryPlaceThing(corpse, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
            corpse.Notify_DebugSpawned();
            if (corpse.Position != null && corpse.Map != null && this.Props.spawnFleck1 != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(corpse.Position.ToVector3Shifted(), corpse.Map, this.Props.spawnFleck1, 1f);
                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                corpse.Map.flecks.CreateFleck(dataStatic);
                if (this.Props.spawnFleck2 != null)
                {
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(corpse.Position.ToVector3Shifted(), corpse.Map, this.Props.spawnFleck2, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    corpse.Map.flecks.CreateFleck(dataStatic2);
                }
            }
            this.parent.Severity -= this.Props.severityToTrigger;
        }
    }
    public class HediffCompProperties_MutantImmunity : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_MutantImmunity()
        {
            this.compClass = typeof(HediffComp_MutantImmunity);
        }
    }
    public class HediffComp_MutantImmunity : HediffComp_DamageNegation
    {
        public new HediffCompProperties_MutantImmunity Props
        {
            get
            {
                return (HediffCompProperties_MutantImmunity)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return base.ShouldDoEffect(dinfo) && dinfo.Instigator != null && dinfo.Instigator is Pawn p && p.IsMutant;
        }
    }
    public class HediffCompProperties_AuraTerror : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraTerror()
        {
            this.compClass = typeof(HediffComp_AuraTerror);
        }
        public ThoughtDef mapwideThought;
        public float baseFleeChance;
        public StatDef fleeChanceScalar;
        public float maxFleeChance;
        public IntRange fleeRange;
        public IntRange fleeDuration;
        public string iconPath;
    }
    public class HediffComp_AuraTerror : HediffComp_Aura
    {
        public new HediffCompProperties_AuraTerror Props
        {
            get
            {
                return (HediffCompProperties_AuraTerror)this.props;
            }
        }
        protected override void AffectPawns(Pawn p, List<Pawn> pawns, bool inCaravan = false)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if ((pawn.HostileTo(p) || p.HostileTo(pawn)) && pawn.needs.mood != null)
                {
                    Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(this.Props.mapwideThought);
                    if (!thought.def.IsSocial)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought, null);
                    }
                }
                if (pawn != null && this.ValidatePawn(p, pawn, inCaravan))
                {
                    AffectPawn(p, pawn);
                }
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (Rand.Chance(Math.Min(this.Props.maxFleeChance,this.Props.baseFleeChance*pawn.GetStatValue(this.Props.fleeChanceScalar))))
            {
                Job job = FleeUtility.FleeJob(pawn,self,this.Props.fleeRange.RandomInRange);
                job.expiryInterval = this.Props.fleeDuration.RandomInRange;
                job.mote = MoteMaker.MakeThoughtBubble(pawn, this.Props.iconPath, true);
                RestUtility.WakeUp(pawn, true);
                pawn.jobs.StopAll(false, true);
                pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, false, true, null, null, false, false, null, false, true, false);
            }
        }
    }
    public class HediffCompProperties_Egregoria : HediffCompProperties
    {
        public HediffCompProperties_Egregoria()
        {
            this.compClass = typeof(HediffComp_Egregoria);
        }
        public int periodicity;
        public float severityPerTotalPsylinkLevels;
    }
    public class HediffComp_Egregoria : HediffComp
    {
        public HediffCompProperties_Egregoria Props
        {
            get
            {
                return (HediffCompProperties_Egregoria)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity))
            {
                float severity = 0f;
                if (this.Pawn.SpawnedOrAnyParentSpawned)
                {
                    foreach (Pawn p in this.Pawn.MapHeld.mapPawns.AllPawns)
                    {
                        if (p.HasPsylink)
                        {
                            severity += p.GetPsylinkLevel();
                        }
                    }
                } else if (this.Pawn.IsCaravanMember()) {
                    Caravan c = this.Pawn.GetCaravan();
                    foreach (Pawn p in c.PawnsListForReading)
                    {
                        if (p.HasPsylink)
                        {
                            severity += p.GetPsylinkLevel();
                        }
                    }
                }
                this.parent.Severity = severity*this.Props.severityPerTotalPsylinkLevels;
            }
        }
    }
    public class HediffCompProperties_Sylar : HediffCompProperties_ForcedByOtherProperty
    {
        public HediffCompProperties_Sylar()
        {
            this.compClass = typeof(HediffComp_Sylar);
        }
    }
    public class HediffComp_Sylar : HediffComp_ForcedByOtherProperty
    {
        public new HediffCompProperties_Sylar Props
        {
            get
            {
                return (HediffCompProperties_Sylar)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.Pawn))
            {
                int bonusTranses = (int)(Rand.Value * 3);
                while (bonusTranses > 0)
                {
                    PsychicAwakeningUtility.AchieveTranscendence(this.Pawn, "", "", 0f, true, null, true, false, false);
                    bonusTranses--;
                }
                int bonusWokes = (int)(Rand.Value * 4);
                while (bonusWokes> 0)
                {
                    PsychicAwakeningUtility.AwakenPsychicTalent(this.Pawn,false,"","",true);
                    bonusWokes--;
                }
                if (this.Pawn.HasPsylink)
                {
                    int bonusPsylevels = (int)(Rand.Value * 3);
                    while (bonusPsylevels > 0)
                    {
                        this.Pawn.GetMainPsylinkSource().ChangeLevel(1, true);
                        bonusPsylevels--;
                    }
                }
                if (ModsConfig.BiotechActive && this.Pawn.genes != null && Rand.Chance(0.5f))
                {
                    List<GeneDef> unacquiredWokeGenes = PsychicAwakeningUtility.AwakenedGeneList();
                    foreach (Gene g in this.Pawn.genes.GenesListForReading)
                    {
                        if (PsychicAwakeningUtility.IsAwakenedPsychicGene(g.def) && unacquiredWokeGenes.Contains(g.def))
                        {
                            unacquiredWokeGenes.Remove(g.def);
                        }
                    }
                    if (unacquiredWokeGenes.Count > 0)
                    {
                        this.Pawn.genes.AddGene(unacquiredWokeGenes.RandomElement(),true);
                    }
                }
            }
        }
    }
    public class HediffCompProperties_LeWokisme : HediffCompProperties_ForcedByOtherProperty
    {
        public HediffCompProperties_LeWokisme()
        {
            this.compClass = typeof(HediffComp_LeWokisme);
        }
        public int newAwakeningPeriodicity;
    }
    public class HediffComp_LeWokisme : HediffComp_ForcedByOtherProperty
    {
        public new HediffCompProperties_LeWokisme Props
        {
            get
            {
                return (HediffCompProperties_LeWokisme)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.ticksToNextAwakening = this.Props.newAwakeningPeriodicity;
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            this.ticksToNextAwakening--;
            if (this.ticksToNextAwakening <= 0)
            {
                this.ticksToNextAwakening = this.Props.newAwakeningPeriodicity;
                string wokeString = "HVT_WokeByLeviathan".Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve();
                PsychicAwakeningUtility.AwakenPsychicTalent(this.Pawn,true,wokeString, wokeString, true);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextAwakening, "ticksToNextAwakening", 60000, false);
        }
        public int ticksToNextAwakening;
    }
    public class HediffCompProperties_MaybeGrantPsylink : HediffCompProperties
    {
        public HediffCompProperties_MaybeGrantPsylink()
        {
            this.compClass = typeof(HediffComp_MaybeGrantPsylink);
        }
        public int count;
        public HediffDef failureHediff;
        [MustTranslate]
        public string succeedLetterLabel;
        [MustTranslate]
        public string succeedLetterText;
        [MustTranslate]
        public string failLetterLabel;
        [MustTranslate]
        public string failLetterText;
        [MustTranslate]
        public string killLetterLabel;
        [MustTranslate]
        public string killLetterText;
    }
    public class HediffComp_MaybeGrantPsylink : HediffComp
    {
        public HediffCompProperties_MaybeGrantPsylink Props
        {
            get
            {
                return (HediffCompProperties_MaybeGrantPsylink)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity <= this.parent.def.minSeverity)
            {
                Pawn pawn = this.Pawn;
                float successChance = 1f;
                if (!PsychicAwakeningUtility.IsMythicTranscendent(pawn))
                {
                    if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                    {
                        Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                        if (psylink != null)
                        {
                            successChance -= ((float)psylink.level / 100f);
                        }
                    } else {
                        successChance -= ((float)pawn.GetPsylinkLevel() / (float)pawn.GetMaxPsylinkLevel());
                    }
                }
                if (Rand.Value <= successChance)
                {
                    pawn.ChangePsylinkLevel(this.Props.count, false);
                    ChoiceLetter notification = LetterMaker.MakeLetter(
                    this.Props.succeedLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.succeedLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                    Find.LetterStack.ReceiveLetter(notification, null);
                } else if (!pawn.health.hediffSet.HasHediff(this.Props.failureHediff)) {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.failureHediff, pawn, null);
                    pawn.health.AddHediff(hediff);
                    ChoiceLetter notification = LetterMaker.MakeLetter(
                    this.Props.failLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.failLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(pawn), null, null, null);
                    Find.LetterStack.ReceiveLetter(notification, null);
                } else {
                    pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.failureHediff).Severity += 1f;
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.failureHediff).Severity < 2f)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        this.Props.failLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.failLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(pawn), null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    } else {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        this.Props.killLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.killLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(this.parent.pawn), null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                }
                pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    public class HediffCompProperties_PsychicWisdom : HediffCompProperties
    {
        public HediffCompProperties_PsychicWisdom()
        {
            this.compClass = typeof(HediffComp_PsychicWisdom);
        }
        public float chanceToAwaken;
    }
    public class HediffComp_PsychicWisdom : HediffComp
    {
        public HediffCompProperties_PsychicWisdom Props
        {
            get
            {
                return (HediffCompProperties_PsychicWisdom)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.parent.Severity <= this.parent.def.minSeverity)
            {
                Pawn pawn = this.Pawn;
                if (pawn.story != null)
                {
                    ChoiceLetter notification;
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) || PsychicAwakeningUtility.IsAwakenedPsychic(pawn, false))
                    {
                        if (Rand.Value <= this.Props.chanceToAwaken)
                        {
                            PsychicAwakeningUtility.AwakenPsychicTalent(pawn, true, "HVT_GetThunderbirdstruck".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_GetThunderbirdstruck".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                        } else {
                            notification = LetterMaker.MakeLetter(
                        "HVT_ThunderbirdFailLetter".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdFailText".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NeutralEvent, new LookTargets(pawn), null, null, null);
                            Find.LetterStack.ReceiveLetter(notification, null);
                        }
                    } else {
                        int degree = (int)Math.Ceiling(Rand.Value * HVTRoyaltyDefOf.HVT_LatentPsychic.degreeDatas.Count);
                        Trait toGain = new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, degree);
                        pawn.story.traits.GainTrait(toGain, true);
                        if (HautsUtility.IsHighFantasy())
                        {
                            notification = LetterMaker.MakeLetter(
                            "HVT_ThunderbirdLatencyLetterFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdLatencyTextFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                        }
                        else
                        {
                            notification = LetterMaker.MakeLetter(
                            "HVT_ThunderbirdLatencyLetter".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdLatencyText".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                        }
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                }
                pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    public class HediffCompProperties_DamageNegationWraith : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_DamageNegationWraith()
        {
            this.compClass = typeof(HediffComp_DamageNegationWraith);
        }
    }
    public class HediffComp_DamageNegationWraith : HediffComp_DamageNegation
    {
        public new HediffCompProperties_DamageNegationWraith Props
        {
            get
            {
                return (HediffCompProperties_DamageNegationWraith)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            if (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon)
            {
                return false;
            }
            if (dinfo.Weapon != null && dinfo.Weapon.weaponTags != null && dinfo.Weapon.weaponTags.Contains("Bladelink"))
            {
                return false;
            }
            return base.ShouldDoEffect(dinfo);
        }
    }
    public class CompProperties_AbilityCantTargetWoke : CompProperties_AbilityEffect
    {
    }
    public class CompAbilityEffect_CantTargetWoke : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(p))
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "HVT_CantTargetWoke".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
    }
    public class CompAbilityEffect_Syzygy : CompAbilityEffect_ForcedByOtherProperty
    {
        public override void CompTick()
        {
            if (this.parent.CanCast && !this.parent.pawn.IsPlayerControlled && this.parent.pawn.CurJob != null && ((this.parent.pawn.CurJob.ability != null && this.parent.pawn.CurJob.ability.def.IsPsycast) || (this.parent.pawn.CurJob.verbToUse is VFECore.Abilities.Verb_CastAbility vca && HautsUtility.IsVPEPsycast(vca.ability))))
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(this.parent.pawn),null),JobCondition.InterruptForced,null,true,false);
            }
            base.CompTick();
        }
    }
    public class CompProperties_AbilityErinys : CompProperties_AbilityAiScansForTargets
    {
        public float aiUseFrequencyOnMundanes;
        public int cooldownOnDeafen;
        public int cooldownOnNonPsycasterKill;
        public int baseCooldownOnPsycasterKill;
        public int psylinkLevelCapForCooldown = 999;
        public HediffDef hediffToGrant;
        public float severityPerPsylinklevel;
    }
    public class CompAbilityEffect_Erinys : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityErinys Props
        {
            get
            {
                return (CompProperties_AbilityErinys)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                float psysens = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_ErinysCensure))
                {
                    Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_ErinysCensure, pawn, null);
                    pawn.health.AddHediff(hediff);
                }
                bool lethal = psysens <= float.Epsilon && (pawn.story == null || !PsychicAwakeningUtility.IsAwakenedPsychic(pawn));
                if (lethal)
                {
                    if (pawn.HasPsylink)
                    {
                        int psyLevel = pawn.GetPsylinkLevel();
                        int funcPsyLevel = Math.Min(this.Props.psylinkLevelCapForCooldown, psyLevel);
                        this.parent.StartCooldown(this.Props.baseCooldownOnPsycasterKill*(int)Math.Pow(funcPsyLevel, funcPsyLevel));
                        this.parent.pawn.health.hediffSet.TryGetHediff(this.Props.hediffToGrant, out Hediff h);
                        if (h != null)
                        {
                            h.Severity += psyLevel*this.Props.severityPerPsylinklevel;
                        } else {
                            h = HediffMaker.MakeHediff(this.Props.hediffToGrant,this.parent.pawn);
                            this.parent.pawn.health.AddHediff(h);
                            h.Severity = psyLevel * this.Props.severityPerPsylinklevel;
                        }
                        int psylinks = (int)Math.Ceiling(Rand.Value * pawn.GetPsylinkLevel());
                        for (int i = 0; i < psylinks; i++)
                        {
                            Thing thing = ThingMaker.MakeThing(ThingDefOf.PsychicAmplifier, null);
                            GenSpawn.Spawn(thing, pawn.Position, pawn.Map, Rot4.North, WipeMode.Vanish, false);
                            pawn.ChangePsylinkLevel(-1, false);
                        }
                    } else {
                        this.parent.StartCooldown(this.Props.cooldownOnNonPsycasterKill);
                    }
                    pawn.Kill(null);
                } else {
                    this.parent.StartCooldown(this.Props.cooldownOnDeafen);
                }
                if (this.parent.CooldownTicksRemaining > this.parent.def.cooldownTicksRange.max)
                {
                    this.parent.StartCooldown(this.parent.def.cooldownTicksRange.max);
                }
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return target.Pawn != null && (target.Pawn.story == null || !PsychicAwakeningUtility.IsAwakenedPsychic(target.Pawn)) && (target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > 1f || target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon || Rand.Value < this.Props.aiUseFrequencyOnMundanes || (target.Pawn.HasPsylink && target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon));
        }
    }
    public class CompProperties_AbilityResurrectSideEffect : CompProperties_AbilityAiScansForTargets
    {
        public List<HediffDef> hediffDefs;
        public HediffDef hediffDefToExplode;
        public bool addToBrain = true;
    }
    public class CompAbilityEffect_ResurrectSideEffect : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityResurrectSideEffect Props
        {
            get
            {
                return (CompProperties_AbilityResurrectSideEffect)this.props;
            }
        }
        public override float Range { 
            get {
                return Math.Max(base.Range,10f);
            } 
        }
        public override bool AdditionalQualifiers(Thing thing)
        {
            if (this.parent.pawn.Faction != null && thing is Corpse c && c.InnerPawn != null && c.InnerPawn.Faction != null && c.InnerPawn.Faction == this.parent.pawn.Faction && c.InnerPawn.MarketValue >= 1000f && !c.InnerPawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_PhoenixPostResurrection))
            {
                return true;
            }
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn innerPawn = ((Corpse)target.Thing).InnerPawn;
            if (innerPawn.health.hediffSet.HasHediff(this.Props.hediffDefToExplode) && (innerPawn.story == null || !innerPawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPhoenix)))
            {
                if (PawnUtility.ShouldSendNotificationAbout(innerPawn))
                {
                    if (HautsUtility.IsHighFantasy()) {
                        Messages.Message("HVT_PhoenixOverloadFantasy".Translate().CapitalizeFirst().Formatted(innerPawn.Named("PAWN")).AdjustedFor(innerPawn, "PAWN", true).Resolve(), innerPawn, MessageTypeDefOf.NegativeEvent, true);
                    } else {
                        Messages.Message("HVT_PhoenixOverload".Translate().CapitalizeFirst().Formatted(innerPawn.Named("PAWN")).AdjustedFor(innerPawn, "PAWN", true).Resolve(), innerPawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
                GenExplosion.DoExplosion(target.Cell, target.Thing.Map, 1f * this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity), DamageDefOf.Flame, null, 12, -1f, null, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                target.Thing.Destroy();
                return;
            }
            if (this.Props.hediffDefs != null)
            {
                foreach (HediffDef h in this.Props.hediffDefs)
                {
                    Hediff hediff = HediffMaker.MakeHediff(h, innerPawn, null);
                    if (this.Props.addToBrain)
                    {
                        innerPawn.health.AddHediff(hediff, innerPawn.health.hediffSet.GetBrain());
                    } else {
                        innerPawn.health.AddHediff(hediff);
                    }
                }
            }
            if (ResurrectionUtility.TryResurrect(innerPawn))
            {
                if (PawnUtility.ShouldSendNotificationAbout(innerPawn))
                {
                    Messages.Message("MessagePawnResurrected".Translate(innerPawn), innerPawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    public class CompProperties_AbilityPsychicAwakening : CompProperties_AbilityEffect
    {
        public float maxAwakenings = 2;
    }
    public class CompAbilityEffect_PsychicAwakening : CompAbilityEffect
    {
        public new CompProperties_AbilityPsychicAwakening Props
        {
            get
            {
                return (CompProperties_AbilityPsychicAwakening)this.props;
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.story == null)
                {
                    return false;
                }
                int wokes = 0;
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    if (PsychicAwakeningUtility.IsAwakenedTrait(t.def))
                    {
                        wokes++;
                    }
                }
                if (wokes >= this.Props.maxAwakenings)
                {
                    if (throwMessages)
                    {
                        Messages.Message("HVT_WontTargetAwakened2".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
            return base.Valid(target, throwMessages);
        }
    }
    public class CompProperties_AbilityMindControl : CompProperties_AbilityAiScansForTargets
    {
        public bool setsIdeo = true;
        public bool permanent = true;
        public bool failsOnAwokens;
        public bool failsOnTrans;
        public float durationHours = 1f;
        public float aiMinMarketValueToTarget;
        [MustTranslate]
        public string letterLabel;
        [MustTranslate]
        public string letterText;
    }
    public class CompAbilityEffect_MindControl : CompAbilityEffect
    {
        public new CompProperties_AbilityMindControl Props
        {
            get
            {
                return (CompProperties_AbilityMindControl)this.props;
            }
        }
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool canTarget = base.CanApplyOn(target, dest);
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.Faction == this.parent.pawn.Faction)
                {
                    return false;
                }
                if (pawn.story != null)
                {
                    if (this.Props.failsOnAwokens && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                    {
                        return false;
                    }
                    if (this.Props.failsOnTrans && PsychicAwakeningUtility.IsTranscendent(pawn))
                    {
                        return false;
                    }
                }
            }
            return canTarget;
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.Faction == this.parent.pawn.Faction)
                {
                    if (throwMessages)
                    {
                        if (HautsUtility.IsHighFantasy())
                        {
                            Messages.Message("HVT_WontTargetSameFactionFantasy".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        } else {
                            Messages.Message("HVT_WontTargetSameFaction".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    return false;
                }
                if (pawn.story != null)
                {
                    if (this.Props.failsOnAwokens && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                    {
                        if (throwMessages)
                        {
                            Messages.Message("HVT_WontTargetAwakened".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                        return false;
                    }
                    if (this.Props.failsOnTrans && PsychicAwakeningUtility.IsTranscendent(pawn))
                    {
                        if (throwMessages)
                        {
                            Messages.Message("HVT_WontTargetTrans".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                        return false;
                    }
                }
            }
            return base.Valid(target, throwMessages);
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                if (pawn.story != null)
                {
                    if (this.Props.failsOnAwokens && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                    {
                        return;
                    }
                    if (this.Props.failsOnTrans && PsychicAwakeningUtility.IsTranscendent(pawn))
                    {
                        return;
                    }
                }
                if (pawn.InMentalState)
                {
                    pawn.MentalState.RecoverFromState();
                }
                if (ModsConfig.IdeologyActive && this.parent.pawn.ideo != null && pawn.ideo != null && this.Props.setsIdeo)
                {
                    Ideo ideo = HautsTraitsRoyalty.GetInstanceField(typeof(Pawn_IdeoTracker), this.parent.pawn.ideo, "ideo") as Ideo;
                    pawn.ideo.SetIdeo(ideo);
                }
                Faction pawnFaction = pawn.Faction;
                if (this.parent.pawn.Faction != null)
                {
                    pawn.SetFaction(this.parent.pawn.Faction);
                }
                pawn.jobs.StopAll(false, true);
                if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer))
                {
                    LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction, true, true, false, false, true, false, false), pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                }
                LetterDef letterDef;
                if (this.parent.pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    letterDef = LetterDefOf.PositiveEvent;
                } else if (pawnFaction == Faction.OfPlayerSilentFail) {
                    letterDef = LetterDefOf.NegativeEvent;
                } else {
                    return;
                }
                ChoiceLetter notification = LetterMaker.MakeLetter(
                this.Props.letterLabel.Formatted(this.parent.pawn.Named("INITIATOR"), pawn.Named("RECIPIENT")), this.Props.letterText.Formatted(this.parent.pawn.Named("INITIATOR"), pawn.Named("RECIPIENT")), letterDef, new LookTargets(pawn), null, null, null);
                Find.LetterStack.ReceiveLetter(notification, null);
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return base.AICanTargetNow(target) && target.Pawn != null && (target.Pawn.story == null || !PsychicAwakeningUtility.IsAwakenedPsychic(target.Pawn)) && target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && target.Pawn.MarketValue >= this.Props.aiMinMarketValueToTarget;
        }
    }
    public class CompProperties_MassInversion : CompProperties_AbilityEffect
    {
        public CompProperties_MassInversion()
        {
            this.compClass = typeof(CompAbilityEffect_MassInversion);
        }
        public int tilesRadius = 0;
    }
    public class CompAbilityEffect_MassInversion : CompAbilityEffect
    {
        public new CompProperties_MassInversion Props
        {
            get
            {
                return (CompProperties_MassInversion)this.props;
            }
        }
        public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
        {
            GenDraw.DrawWorldRadiusRing(this.parent.pawn.Tile, 30);
            return base.Valid(target, throwMessages);
        }
        public override bool CanApplyOn(GlobalTargetInfo target)
        {
            if (Find.WorldGrid.TraversalDistanceBetween(this.parent.pawn.Tile, target.Tile, true, int.MaxValue) > 30)
            {
                Messages.Message("HVT_MassInversionOutOfRange".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (Find.World.worldObjects.WorldObjectAt<WorldObject>(target.Tile) == null)
            {
                Messages.Message("HVT_MassInversionNoTarget".Translate(), null, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return (base.CanApplyOn(target));
        }
        public override bool CanCast {
            get {

                if (this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility) != null)
                {
                    Ability oa = this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility);
                    if (oa.CanCast)
                    {
                        return (int)typeof(Ability).GetField("charges", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(oa) >= oa.def.charges;
                    }
                }
                return false;
            }
        }
        public override void Apply(GlobalTargetInfo target)
        {
            base.Apply(target);
            WorldObject worldObject = Find.World.worldObjects.WorldObjectAt<WorldObject>(target.Tile);
            if (worldObject != null)
            {
                foreach (WorldObject w in Find.World.worldObjects.AllWorldObjects)
                {
                    if (Find.WorldGrid.TraversalDistanceBetween(w.Tile, target.Tile, true, int.MaxValue) <= this.Props.tilesRadius)
                    {
                        MapParent mp = w as MapParent;
                        if (mp != null && mp.HasMap)
                        {
                            IncidentParms parms = new IncidentParms
                            {
                                target = mp.Map
                            };
                            DefDatabase<IncidentDef>.GetNamedSilentFail("VolcanicWinter").Worker.TryExecute(parms);
                            mp.Map.weatherManager.TransitionTo(DefDatabase<WeatherDef>.GetNamed("RainyThunderstorm"));
                        }
                    }
                }
                SoundDefOf.Thunder_OnMap.PlayOneShot(this.parent.pawn);
                WeatherEvent_LightningStrike lightningflash = new WeatherEvent_LightningStrike(this.parent.pawn.Map);
                lightningflash.WeatherEventDraw();
                MapParent mapParent = worldObject as MapParent;
                if (mapParent != null && mapParent.HasMap)
                {
                    foreach (Thing thing in mapParent.Map.spawnedThings)
                    {
                        thing.Destroy(DestroyMode.KillFinalize);
                    }
                }
                string how = HautsUtility.IsHighFantasy() ? "HVT_PsychicWMD1Fantasy".Translate(worldObject.Label) : "HVT_PsychicWMD1".Translate(worldObject.Label);
                ChoiceLetter notification = LetterMaker.MakeLetter(
                how, "HVT_PsychicWMD2".Translate(worldObject.Label, this.parent.pawn.Name.ToStringShort, this.parent.pawn.gender.GetObjective()).CapitalizeFirst(), LetterDefOf.Death, null, null, null, null);
                Find.LetterStack.ReceiveLetter(notification, null);
                foreach (Faction faction in Find.FactionManager.AllFactions)
                {
                    if (!faction.IsPlayer && !faction.defeated)
                    {
                        faction.TryAffectGoodwillWith(this.parent.pawn.Faction, -200, true, true, null, null);
                    }
                }
                worldObject.Destroy();
                Find.World.grid.StandardizeTileData();
                if (this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility) != null)
                {
                    Ability zizBeam = this.parent.pawn.abilities.GetAbility(HVTRoyaltyDefOf.HVT_ZizAbility);
                    typeof(Ability).GetField("charges", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(zizBeam,0);
                    zizBeam.StartCooldown(zizBeam.def.cooldownTicksRange.RandomInRange);
                    HautsFramework.HautsFramework.HautsActivatePostfix(zizBeam);
                }
            }
        }
    }
    public class CompProperties_PulverizationBeam : CompProperties_EffectWithDest
    {
        public int durationTicks;
    }
    public class CompAbilityEffect_PulverizationBeam : CompAbilityEffect_WithDest
    {
        public new CompProperties_PulverizationBeam Props
        {
            get
            {
                return (CompProperties_PulverizationBeam)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (target != null && target.Cell != null)
            {
                int maxBeams = (int)Math.Max(1f, this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                for (int i = 0; i < maxBeams; i++)
                {
                    PulverizationBeam powerBeam = (PulverizationBeam)GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_PulverizationBeam, target.Cell, this.parent.pawn.Map, WipeMode.Vanish);
                    powerBeam.duration = this.Props.durationTicks;
                    powerBeam.instigator = this.parent.pawn;
                    powerBeam.StartStrike();
                }
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return this.Caster.MapHeld != null && (this.Caster.PositionHeld.DistanceTo(target.Cell) > 15f || Rand.Value <= 0.5f) && this.Caster.MapHeld.listerThings.ThingsOfDef(HVTRoyaltyDefOf.HVT_PulverizationBeam).Count == 0;
        }
    }
    public class CompProperties_AbilityTargetForMassDestruction : CompProperties_AbilityAiScansForTargets
    {
        public float netMarketValueToFire;
        public float pastThisPercentDmgLoosenRestrictions;
        public float friendlyFireConsiderationFactor;
    }
    public class CompAbilityEffect_TargetForMassDestruction : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityTargetForMassDestruction Props
        {
            get
            {
                return (CompProperties_AbilityTargetForMassDestruction)this.props;
            }
        }
        public override bool AdditionalQualifiers(Thing thing)
        {
            if (HautsUtility.MissingHitPointPercentageFor(this.parent.pawn) >= this.Props.pastThisPercentDmgLoosenRestrictions)
            {
                return true;
            }
            float marketValue = thing.MarketValue;
            if ((thing is Pawn || (thing.def.building != null && (thing.def.building.IsTurret || thing.def.building.isTrap))) && GenSight.LineOfSight(this.parent.pawn.Position,thing.Position,thing.Map))
            {
                foreach (Thing t in GenRadial.RadialDistinctThingsAround(thing.Position, thing.Map, this.parent.def.EffectRadius, true))
                {
                    if (this.parent.pawn.HostileTo(t))
                    {
                        marketValue += t.MarketValue;
                    } else if (t.Faction != null) {
                        marketValue -= t.MarketValue * this.Props.friendlyFireConsiderationFactor;
                    }
                }
            }
            return marketValue > this.Props.netMarketValueToFire;
        }
    }
    public class Graphic_StandPower : Graphic_Mote
    {
        protected override bool ForcePropertyBlock
        {
            get
            {
                return true;
            }
        }
        public override void Init(GraphicRequest req)
        {
            this.data = req.graphicData;
            this.path = req.path;
            this.maskPath = req.maskPath;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            this.request = req;
        }
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Mote mote = (Mote)thing;
            Pawn pawn = mote.link1.Target.Thing as Pawn;
            Pawn pawn2 = ((mote.link1.Target.Thing is Corpse corpse) ? corpse.InnerPawn : null) ?? pawn;
            if (pawn2 == null)
            {
                pawn2 = this.lastPawn;
            }
            Color color = this.color;
            if (ModsConfig.IdeologyActive && pawn2.story != null && pawn2.story.favoriteColor != null)
            {
                color = pawn2.story.favoriteColor.Value;
            }
            color.a *= mote.Alpha;
            PawnRenderer renderer = pawn2.Drawer.renderer;
            if ((renderer != null ? renderer.renderTree : null) == null || !renderer.renderTree.Resolved)
            {
                return;
            }
            Rot4 rot2 = (pawn2.GetPosture() == PawnPosture.Standing) ? pawn2.Rotation : renderer.LayingFacing();
            Vector3 vector = pawn2.DrawPos;
            Building_Bed building_Bed = pawn2.CurrentBed();
            if (building_Bed != null)
            {
                Rot4 rotation = building_Bed.Rotation;
                rotation.AsInt += 2;
                vector -= rotation.FacingCell.ToVector3() * (pawn2.story.bodyType.bedOffset + pawn2.Drawer.renderer.BaseHeadOffsetAt(Rot4.South).z);
            }
            bool posture = pawn2.GetPosture() != PawnPosture.Standing;
            vector.y = mote.def.Altitude;
            if (this.lastPawn != pawn2 || this.lastFacing != rot2)
            {
                this.bodyMaterial = this.MakeMatFrom(this.request, renderer.BodyGraphic.MatAt(rot2, null).mainTexture);
            }
            Mesh mesh;
            if (pawn2.RaceProps.Humanlike)
            {
                mesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn2).MeshAt(rot2);
            } else {
                mesh = renderer.BodyGraphic.MeshAt(rot2);
            }
            this.bodyMaterial.SetVector("_pawnCenterWorld", new Vector4(vector.x, vector.z, 0f, 0f));
            this.bodyMaterial.SetVector("_pawnDrawSizeWorld", new Vector4(mesh.bounds.size.x, mesh.bounds.size.z, 0f, 0f));
            this.bodyMaterial.SetFloat(ShaderPropertyIDs.AgeSecs, mote.AgeSecs);
            this.bodyMaterial.SetColor(ShaderPropertyIDs.Color, color);
            Quaternion quaternion = Quaternion.AngleAxis((!posture) ? 0f : renderer.BodyAngle(PawnRenderFlags.None), Vector3.up);
            if (building_Bed == null || building_Bed.def.building.bed_showSleeperBody)
            {
                GenDraw.DrawMeshNowOrLater(mesh, vector, quaternion, this.bodyMaterial, false);
            }
            if (pawn2.RaceProps.Humanlike)
            {
                if (this.lastPawn != pawn2 || this.lastFacing != rot2)
                {
                    this.headMaterial = this.MakeMatFrom(this.request, renderer.HeadGraphic.MatAt(rot2, null).mainTexture);
                }
                Vector3 b = quaternion * renderer.BaseHeadOffsetAt(rot2) + new Vector3(0f, 0.001f, 0f);
                Mesh mesh2 = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn2).MeshAt(rot2);
                this.headMaterial.SetVector("_pawnCenterWorld", new Vector4(vector.x, vector.z, 0f, 0f));
                this.headMaterial.SetVector("_pawnDrawSizeWorld", new Vector4(mesh2.bounds.size.x, mesh.bounds.size.z, 0f, 0f));
                this.headMaterial.SetFloat(ShaderPropertyIDs.AgeSecs, mote.AgeSecs);
                this.headMaterial.SetColor(ShaderPropertyIDs.Color, color);
                GenDraw.DrawMeshNowOrLater(mesh2, vector + b, quaternion, this.headMaterial, false);
            }
            if (pawn2 != null)
            {
                this.lastPawn = pawn2;
            }
            this.lastFacing = rot2;
        }
        private Material MakeMatFrom(GraphicRequest req, Texture mainTex)
        {
            return MaterialPool.MatFrom(new MaterialRequest
            {
                mainTex = mainTex,
                shader = req.shader,
                color = this.color,
                colorTwo = this.colorTwo,
                renderQueue = req.renderQueue,
                shaderParameters = req.shaderParameters
            });
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        private GraphicRequest request;
        private Pawn lastPawn;
        private Rot4 lastFacing;
        private Material bodyMaterial;
        private Material headMaterial;
    }
    public class Verb_Shoot_NeuralHeatAmmo : Verb_Shoot
    {
        public override bool Available()
        {
            if (this.CasterIsPawn && this.CasterPawn.psychicEntropy.EntropyValue < 1f)
            {
                return false;
            }
            return base.Available();
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (this.CasterIsPawn)
            {
                this.CasterPawn.psychicEntropy.TryAddEntropy(-0.5f);
            }
        }
        /*public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caster.Position, this.verbProps.range * this.CasterPawn.GetStatValue(StatDefOf.PsychicSensitivity), Color.white);
        }*/
    }
    public class VerbCompProperties_StandPower : VerbCompProperties
    {
        public HediffDef hediff;
        public string text;
    }
    public class VerbComp_StandPower : VerbComp
    {
        public VerbCompProperties_StandPower Props
        {
            get
            {
                return this.props as VerbCompProperties_StandPower;
            }
        }
        public override void Notify_ShotFired()
        {
            base.Notify_ShotFired();
            if (this.parent.Manager != null && this.parent.Manager.Pawn != null)
            {
                if (!this.parent.Manager.Pawn.health.hediffSet.HasHediff(this.Props.hediff))
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, this.parent.Manager.Pawn, null);
                    this.parent.Manager.Pawn.health.AddHediff(hediff);
                } else {
                    this.parent.Manager.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff).Severity = this.Props.hediff.maxSeverity;
                }
                if (this.Props.text != null)
                {
                    Vector3 loc = new Vector3((float)this.parent.Manager.Pawn.PositionHeld.x + 1f, (float)this.parent.Manager.Pawn.PositionHeld.y, (float)this.parent.Manager.Pawn.PositionHeld.z + 1f);
                    MoteMaker.ThrowText(loc, this.parent.Manager.Pawn.MapHeld, this.Props.text, Color.white, -1f);
                }
            }
        }
    }
    public class PulverizationBeam : OrbitalStrike
    {
        public override void StartStrike()
        {
            base.StartStrike();
            MoteMaker.MakePowerBeamMote(base.Position, base.Map);
        }
        public override void Tick()
        {
            base.Tick();
            if (base.Destroyed)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                this.Pulverize();
            }
        }
        private void Pulverize()
        {
            IntVec3 c = (from x in GenRadial.RadialCellsAround(base.Position, 15f, true)
                         where x.InBounds(base.Map)
                         select x).RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(base.Position) / 15f, 1f) + 0.05f);
            PulverizationBeam.tmpThings.Clear();
            PulverizationBeam.tmpThings.AddRange(c.GetThingList(base.Map));
            for (int i = 0; i < PulverizationBeam.tmpThings.Count; i++)
            {
                Pawn pawn = PulverizationBeam.tmpThings[i] as Pawn;
                if (pawn != null && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitZiz))
                {
                    pawn.health.hediffSet.TryGetHediff(HVTRoyaltyDefOf.HVT_ZizBuff,out Hediff zizBuff);
                    if (zizBuff != null)
                    {
                        zizBuff.Severity = zizBuff.def.maxSeverity;
                    } else {
                        pawn.health.AddHediff(HVTRoyaltyDefOf.HVT_ZizBuff);
                    }
                } else {
                    BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
                    if (pawn != null)
                    {
                        battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_PowerBeam, this.instigator as Pawn);
                        Find.BattleLog.Add(battleLogEntry_DamageTaken);
                    }
                    PulverizationBeam.tmpThings[i].TakeDamage(new DamageInfo(DamageDefOf.Bomb, (float)PulverizationBeam.DamageAmountRange.RandomInRange, 0f, -1f, this.instigator, null, this.weaponDef, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true)).AssociateWithLog(battleLogEntry_DamageTaken);
                }
            }
            PulverizationBeam.tmpThings.Clear();
        }
        public const float Radius = 17f;
        private static readonly IntRange DamageAmountRange = new IntRange(45, 90);
        private static List<Thing> tmpThings = new List<Thing>();
    }
    public class ScenPart_ForcedLatentPsychic : ScenPart_PawnModifier
    {
        public override string Summary(Scenario scen)
        {
            if (HautsUtility.IsHighFantasy())
            {
                return "HVT_ForcedLatentPsychicFantasy".Translate();
            }
            return "HVT_ForcedLatentPsychic".Translate();
        }
        protected override void ModifyPawnPostGenerate(Pawn pawn, bool redressed)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return;
            }
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                return;
            }
            int degree = (int)Rand.Range(1, 6);
            if (degree == 1 && pawn.skills != null)
            {
                foreach (SkillRecord sr in pawn.skills.skills)
                {
                    if (sr.Level >= 15)
                    {
                        degree = (int)Rand.Range(2, 6);
                        break;
                    }
                }
            }
            pawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, degree, true), false);
            foreach (Trait t in pawn.story.traits.allTraits)
            {
                if (PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                {
                    pawn.story.traits.RemoveTrait(t);
                }
            }
        }
    }
    public class ScenPart_ForcedAwakenedPsychic : ScenPart_PawnModifier
    {
        public override string Summary(Scenario scen)
        {
            if (HautsUtility.IsHighFantasy())
            {
                return "HVT_ForcedAwakenedPsychicFantasy".Translate();
            }
            return "HVT_ForcedAwakenedPsychic".Translate();
        }
        protected override void ModifyPawnPostGenerate(Pawn pawn, bool redressed)
        {
            if (pawn.story == null || pawn.story.traits == null || pawn.IsMutant)
            {
                return;
            }
            if (!PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
            {
                PsychicAwakeningUtility.AwakenPsychicTalent(pawn, false, "", "", true);
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        pawn.story.traits.RemoveTrait(t);
                    }
                }
            }
        }
    }
    public class GrantWordPsycast : DefModExtension
    {
        public GrantWordPsycast()
        {

        }
    }
    public class GrantSkipPsycast : DefModExtension
    {
        public GrantSkipPsycast()
        {

        }
    }
    public class BloodRainImmune : DefModExtension
    {
        public BloodRainImmune() { }
    }
    public class NotStormable : DefModExtension
    {
        public NotStormable()
        {

        }
    }
    public class StormCreateCondition : DefModExtension
    {
        public StormCreateCondition()
        {

        }
        public GameConditionDef conditionDef;
    }
    public static class PsychicAwakeningUtility
    {
        //various checks
        public static bool IsAntipsychicTrait(TraitDef def, int degree, bool ignoreAwakenedTraits = true)
        {
            if (ignoreAwakenedTraits && awakenings.Contains(def))
            {
                return false;
            }
            if (def.DataAtDegree(degree).statFactors != null)
            {
                foreach (StatModifier sm in def.DataAtDegree(degree).statFactors)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 1f)
                    {
                        return true;
                    }
                }
            }
            if (def.DataAtDegree(degree).statOffsets != null)
            {
                foreach (StatModifier sm in def.DataAtDegree(degree).statOffsets)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsAntipsychicGene(GeneDef def, bool ignoreAwakenedGenes = true)
        {
            if (ignoreAwakenedGenes && IsAwakenedPsychicGene(def))
            {
                return false;
            }
            if (def.forcedTraits != null)
            {
                foreach (GeneticTraitData gtd in def.forcedTraits)
                {
                    if (PsychicAwakeningUtility.IsAntipsychicTrait(gtd.def, gtd.degree))
                    {
                        return true;
                    }
                }
            }
            if (def.statFactors != null)
            {
                foreach (StatModifier sm in def.statFactors)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 1f)
                    {
                        return true;
                    }
                }
            }
            if (def.statOffsets != null)
            {
                foreach (StatModifier sm in def.statOffsets)
                {
                    if (sm.stat == StatDefOf.PsychicSensitivity && sm.value < 0f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsAwakenedPsychic(Pawn pawn, bool checkGenes = true)
        {
            foreach (Trait x in pawn.story.traits.allTraits)
            {
                if (IsAwakenedTrait(x.def))
                {
                    return true;
                }
            }
            if (checkGenes && (HasAwakenedPsychicGenes(pawn)))
            {
                return true;
            }
            return false;
        }
        public static bool IsAwakenedTrait(TraitDef traitDef)
        {
            if (awakenings.Contains(traitDef))
            {
                return true;
            }
            return false;
        }
        public static bool HasAwakenedPsychicGenes(Pawn pawn)
        {
            if (ModsConfig.BiotechActive && pawn.genes != null && ContainsPsychicGene(pawn.genes))
            {
                return true;
            }
            return false;
        }
        public static bool ContainsPsychicGene(Pawn_GeneTracker genes)
        {
            foreach (Gene g in genes.GenesListForReading)
            {
                if (IsAwakenedPsychicGene(g.def))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsAwakenedPsychicGene(GeneDef geneDef)
        {
            if (wokeGenes.Contains(geneDef))
            {
                return true;
            }
            return false;
        }
        public static bool IsTranscendent(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (IsTranscendentTrait(t.def))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsTranscendentTrait(TraitDef traitDef)
        {
            return regularTranses.Contains(traitDef) || mythicTranses.Contains(traitDef);
        }
        public static bool IsMythicTranscendent(Pawn pawn)
        {
            if (pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (IsMythicTrait(t))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsMythicTrait(Trait trait)
        {
            return mythicTranses.Contains(trait.def);
        }
        //process of awakening methods
        public static void LPLoveCheckRelations(PawnRelationDef prd, Pawn pawn, Pawn otherPawn)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (prd == PawnRelationDefOf.Bond && Rand.Value <= 0.5f)
                {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeBond".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeBondFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve());
                } else if (prd == PawnRelationDefOf.Lover && Rand.Value <= 0.5f) {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeLover".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeLoverFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve());
                } else if (prd == PawnRelationDefOf.Fiance && Rand.Value <= 0.75f) {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeFiance".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeFianceFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve());
                } else if (prd == PawnRelationDefOf.Spouse) {
                    PsychicAwakeningUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeSpouse".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeSpouseFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve());
                }
            }
        }
        public static void AwakenPsychicTalentCheck(Pawn pawn, int requisiteLatentPsychicDegree, bool shouldCure, string triggerEvent, string triggerEventFantasy, bool alreadyHappened = false, float papillonCatalysisChance = 0.5f, bool dontAdjustForPawn = false)
        {
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitAptenodytes))
            {
                PsychicAwakeningUtility.ColonyHuddle(pawn);
                return;
            }
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic, requisiteLatentPsychicDegree))
            {
                for (int j = 1; j <= HVTRoyaltyDefOf.HVT_LatentPsychic.degreeDatas.Count; j++)
                {
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic, j))
                    {
                        pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic, j));
                    }
                }
                if (Current.ProgramState == ProgramState.Playing)
                {
                    TaggedString te = triggerEvent;
                    te = te.AdjustedFor(pawn, "PAWN", true).Resolve();
                    TaggedString tef = triggerEvent;
                    tef = tef.AdjustedFor(pawn, "PAWN", true).Resolve();
                    AwakenPsychicTalent(pawn,shouldCure,te, tef, alreadyHappened);
                } else {
                    AwakenPsychicTalent(pawn, shouldCure, "", "", alreadyHappened);
                }
            }
        }
        public static bool PsychicDeafMutantDeafInteraction(Pawn pawn, bool canCauseReversion = true)
        {
            if (ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.Def.hediff != null)
            {
                if (pawn.mutant.Def.hediff.stages != null)
                {
                    foreach (HediffStage hs in pawn.mutant.Def.hediff.stages)
                    {
                        if (hs.statFactors != null)
                        {
                            foreach (StatModifier sm in hs.statFactors)
                            {
                                if (sm.stat == StatDefOf.PsychicSensitivity && sm.value <= float.Epsilon)
                                {
                                    if (canCauseReversion && !PawnGenerator.IsBeingGenerated(pawn))
                                    {
                                        pawn.mutant.Revert();
                                    } else {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static void AwakenPsychicTalent(Pawn pawn, bool shouldCure, string triggerEvent, string triggerEventFantasy, bool alreadyHappened = false)
        {
            if (PsychicAwakeningUtility.PsychicDeafMutantDeafInteraction(pawn))
            {
                return;
            }
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitAptenodytes))
            {
                PsychicAwakeningUtility.ColonyHuddle(pawn);
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_LatentPsyTerminus, pawn, null);
            pawn.health.AddHediff(hediff);
            while (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic));
            }
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h.def.HasModExtension<RemovedOnAwakening>())
                {
                    hediffsToRemove.Add(h);
                }
            }
            foreach (Hediff h in hediffsToRemove)
            {
                pawn.health.RemoveHediff(h);
            }
            tmpAwakenings.Clear();
            foreach (TraitDef woke in awakenings)
            {
                if (!pawn.WorkTagIsDisabled(woke.requiredWorkTags) && !pawn.story.traits.HasTrait(woke))
                {
                    tmpAwakenings.Add(woke);
                }
            }
            if (tmpAwakenings.Count > 0)
            {
                TraitDef awakening = tmpAwakenings.RandomElement();
                Trait awakenTrait = new Trait(awakening);
                TaggedString explainTrait = "Awakening trait description cannot be formatted while the game is in a non-play state.";
                if (Current.ProgramState == ProgramState.Playing)
                {
                    explainTrait = HautsUtility.IsHighFantasy() ? awakening.GetModExtension<SuperPsychicTrait>().descKeyFantasy.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : awakening.GetModExtension<SuperPsychicTrait>().descKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                }
                pawn.story.traits.GainTrait(awakenTrait);
                if (!alreadyHappened)
                {
                    PsychicHeal(pawn, shouldCure);
                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        TaggedString letterLabel;
                        letterLabel = HautsUtility.IsHighFantasy() ? "HVT_TheWokeningFantasy".Translate(pawn.Name.ToStringShort) : "HVT_TheWokening".Translate(pawn.Name.ToStringShort);
                        TaggedString letterText;
                        if (triggerEvent != null)
                        {
                            letterText = triggerEvent;
                        } else {
                            letterText = HautsUtility.IsHighFantasy() ? "HVT_WokeningDefaultFantasy".Translate() : "HVT_WokeningDefault".Translate(pawn.Name.ToStringShort);
                        }
                        letterText += "\n\n" + explainTrait;
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        letterLabel.Formatted(pawn.Named("PAWN")), letterText.Formatted(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    Hediff hediff2 = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_AwakeningAfterglow, pawn, null);
                    pawn.health.AddHediff(hediff2);
                    if (pawn.psychicEntropy != null)
                    {
                        pawn.psychicEntropy.RechargePsyfocus();
                    }
                }
            }
        }
        //process of transcendence methods
        public static bool CanTranscendAnyways(Pawn pawn)
        {
            return false;
        }
        public static float MaxTransesForPawn(Pawn pawn)
        {
            float maxTranses = HVT_Mod.settings.maxTranscendences;
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LovebugDoppel) || pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitFossil))
                {
                    return 0;
                }
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_AwakenedErudite))
                {
                    maxTranses += 2f;
                }
                if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.HasActiveGene(HVTRoyaltyDefOf.HVT_AEruditeGene))
                {
                    maxTranses += 1f;
                }
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitLeviathan))
                {
                    maxTranses += 99999f;
                }
            }
            return maxTranses;
        }
        private static void PopulateEligibleTransList(List<TraitDef> transferTranses, List<TraitDef> listToTakeFrom, Pawn pawn)
        {
            foreach (TraitDef td in listToTakeFrom)
            {
                if ((td.requiredWorkTags & pawn.CombinedDisabledWorkTags) != WorkTags.None || (td.disabledWorkTags & pawn.kindDef.requiredWorkTags) != WorkTags.None)
                {
                    continue;
                }
                transferTranses.Add(td);
            }
        }
        public static void InduceArchiteTranscendenceDelay(Pawn pawn, List<GeneDef> genes)
        {
            int architeCount = 0;
            foreach (GeneDef g in genes)
            {
                if (g.biostatArc != 0)
                {
                    architeCount += g.biostatArc;
                }
            }
            if (Rand.Value <= (architeCount / 100))
            {
                Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_CountdownToTranscendence, pawn, null);
                pawn.health.AddHediff(hediff, null, null, null);
            }
        }
        public static bool ShouldTranscend(Pawn pawn)
        {
            if (pawn.story != null && (IsAwakenedPsychic(pawn, false) || (IsAwakenedPsychic(pawn) && Rand.Value < HVT_Mod.settings.wokeGeneTransSuccessChance)))
            {
                float maxTranses = MaxTransesForPawn(pawn);
                int currentTransCount = 0;
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (IsTranscendentTrait(t.def))
                    {
                        currentTransCount++;
                    }
                }
                if (currentTransCount < maxTranses)
                {
                    if (Rand.Value <= (1f + maxTranses - HVT_Mod.settings.maxTranscendences) / (1f + currentTransCount))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void AchieveTranscendence(Pawn pawn, string liminalEvent, string liminalEventFantasy, float additionalLevelChance, bool alreadyHappened = false, List<TraitDef> forbiddenTranses = null, bool sendLetter = true, bool mythsOnly = false, bool canRandomlyGetMythic = true)
        {
            if (PsychicAwakeningUtility.ShouldTranscend(pawn))
            {
                PsychicAwakeningUtility.AchieveTranscendenceDirect(pawn,liminalEvent,liminalEventFantasy,additionalLevelChance,alreadyHappened,forbiddenTranses,sendLetter,mythsOnly,canRandomlyGetMythic);
            }
        }
        public static void AchieveTranscendenceDirect(Pawn pawn, string liminalEvent, string liminalEventFantasy, float additionalLevelChance, bool alreadyHappened = false, List<TraitDef> forbiddenTranses = null, bool sendLetter = true, bool mythsOnly = false, bool canRandomlyGetMythic = true)
        {
            if (PsychicAwakeningUtility.PsychicDeafMutantDeafInteraction(pawn))
            {
                return;
            }
            tempTranses.Clear();
            List<TraitDef> transferTranses = new List<TraitDef>();
            if (mythsOnly)
            {
                PopulateEligibleTransList(transferTranses,mythicTranses,pawn);
            } else {
                PopulateEligibleTransList(transferTranses, regularTranses, pawn);
                if (canRandomlyGetMythic && Rand.Value <= 0.12f)
                {
                    PopulateEligibleTransList(transferTranses, mythicTranses, pawn);
                }
            }
            foreach (TraitDef t in transferTranses)
            {
                tempTranses.Add(t);
            }
            for (int i = 0; i < pawn.story.traits.allTraits.Count; i++)
            {
                if (tempTranses.Contains(pawn.story.traits.allTraits[i].def))
                {
                    tempTranses.Remove(pawn.story.traits.allTraits[i].def);
                }
            }
            if (forbiddenTranses != null)
            {
                for (int i = 0; i < forbiddenTranses.Count; i++)
                {
                    if (tempTranses.Contains(forbiddenTranses[i]))
                    {
                        tempTranses.Remove(forbiddenTranses[i]);
                    }
                }
            }
            if (tempTranses.Count > 0)
            {
                Trait transcendent = new Trait(tempTranses.RandomElement());
                TaggedString explainTrait = "Awakening trait description cannot be formatted while the game is in a non-play state.";
                if (Current.ProgramState == ProgramState.Playing)
                {
                    explainTrait = HautsUtility.IsHighFantasy() ? transcendent.def.GetModExtension<SuperPsychicTrait>().descKeyFantasy.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : transcendent.def.GetModExtension<SuperPsychicTrait>().descKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                }
                pawn.story.traits.GainTrait(transcendent);
                tempTranses.Remove(transcendent.def);
                if (!alreadyHappened)
                {
                    if (pawn.Map != null)
                    {
                        FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 20f);
                        FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 10f);
                        MoteMaker.MakeAttachedOverlay(pawn, DefDatabase<ThingDef>.GetNamed("Mote_ResurrectAbility"), Vector3.zero, 2f, -1f);
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(pawn);
                        WeatherEvent_LightningStrike lightningflash = new WeatherEvent_LightningStrike(pawn.Map);
                        lightningflash.WeatherEventDraw();
                    }
                    if (PawnUtility.ShouldSendNotificationAbout(pawn) && sendLetter)
                    {
                        TaggedString letterLabel = "HVT_TheTransing".Translate(pawn.Name.ToStringShort);
                        TaggedString letterText;
                        if (liminalEvent != null)
                        {
                            letterText = HautsUtility.IsHighFantasy() ? liminalEventFantasy : liminalEvent;
                        } else {
                            letterText = "HVT_TransDefault".Translate();
                        }
                        letterText += HautsUtility.IsHighFantasy() ? (" " + "HVT_TransSuffixFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() + " \n\n" + explainTrait) : (" " + "HVT_TransSuffix".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() + " \n\n" + explainTrait);
                        letterText = letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                        LookTargets toLook = new LookTargets(pawn);
                        ChoiceLetter awakenLetter = LetterMaker.MakeLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, toLook, null, null, null);
                        Find.LetterStack.ReceiveLetter(awakenLetter, null);
                    }
                }
            } else {
                Log.Error("HVT_CantGiveTrans".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
            if (pawn.psychicEntropy != null)
            {
                pawn.psychicEntropy.RechargePsyfocus();
            }
        }
        //read these private trait lists
        public static List<TraitDef> AwakenedTraitList()
        {
            return awakenings;
        }
        public static List<TraitDef> RegularTranscendentTraitList()
        {
            return regularTranses;
        }
        public static List<TraitDef> MythicTranscendentTraitList()
        {
            return mythicTranses;
        }
        public static List<TraitDef> AllTranscendentTraitList()
        {
            List<TraitDef> blah = regularTranses;
            blah.AddRange(mythicTranses);
            return blah;
        }
        public static List<GeneDef> AwakenedGeneList()
        {
            return wokeGenes;
        }
        public static int AwakenedTraitCount()
        {
            return awakenings.Count;
        }
        public static int TranscendentTraitCount()
        {
            return regularTranses.Count;
        }
        public static int MythicTransTraitCount()
        {
            return mythicTranses.Count;
        }
        //functions of specific traits
        public static void PsychicHeal(Pawn pawn, bool shouldCure)
        {
            HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
            if (shouldCure)
            {
                for (int i = 100; i > 0; i--)
                {
                    HealthUtility.FixWorstHealthCondition(pawn);
                }
            }
            if (pawn.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 10f);
                SoundDefOf.Thunder_OnMap.PlayOneShot(pawn);
                WeatherEvent_LightningStrike lightningflash = new WeatherEvent_LightningStrike(pawn.Map);
                lightningflash.WeatherEventDraw();
            }
        }
        public static void GrantEruditeEffects(Pawn pawn, int abilities)
        {
            PawnUtility.ChangePsylinkLevel(pawn, 1, false);
            Hediff_Psylink hediff_Psylink = pawn.GetMainPsylinkSource();
            if (!ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
            {
                for (int i = 0; i < abilities; i++)
                {
                    int randLevel = (int)Math.Ceiling(Rand.Value * hediff_Psylink.level);
                    List<AbilityDef> unknownPsycasts = DefDatabase<AbilityDef>.AllDefs.Where((AbilityDef a) => a.IsPsycast && a.level == randLevel && pawn.abilities.GetAbility(a) == null).ToList<AbilityDef>();
                    if (unknownPsycasts.Count > 0)
                    {
                        pawn.abilities.GainAbility(unknownPsycasts.RandomElement());
                    }
                }
            } else {
                int levelsToGain = abilities / 2;
                if (levelsToGain >= 5)
                {
                    levelsToGain = 4;
                }
                for (int i = 0; i < levelsToGain; i++)
                {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                }
            }
        }
        public static void ColonyHuddle(Pawn pawn)
        {
            Pawn guy = PsychicAwakeningUtility.ColonyHuddleInner(pawn);
            guy.SetFaction(pawn.Faction);
            if (pawn.SpawnedOrAnyParentSpawned)
            {
                CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => pawn.MapHeld.reachability.CanReachColony(c) && !c.Fogged(pawn.MapHeld), pawn.MapHeld, CellFinder.EdgeRoadChance_Neutral, out IntVec3 iv3);
                GenSpawn.Spawn(guy,iv3,pawn.MapHeld);
            } else if (pawn.GetCaravan() != null) {
                Find.WorldPawns.PassToWorld(guy, PawnDiscardDecideMode.Decide);
                guy.SetFaction(pawn.GetCaravan().Faction);
                pawn.GetCaravan().AddPawn(guy,true);
                guy.SetFaction(pawn.Faction);
            }
            ChoiceLetter choiceLetter = LetterMaker.MakeLetter("HVT_PengwengGetLabel".Translate(), "HVT_PengwengGetText".Translate(pawn.Name.ToStringShort,guy.Name.ToStringShort), LetterDefOf.PositiveEvent, guy);
            Find.LetterStack.ReceiveLetter(choiceLetter, null, 0, true);

        }
        public static Pawn ColonyHuddleInner(Pawn pawn)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("StrangerInBlack"), pawn.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, true, true, 20f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, ModsConfig.IdeologyActive ? pawn.Ideo : null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
        }
        public static void PsycastActivationRiderEffects(Psycast __instance)
        {
            if (__instance.pawn != null)
            {
                Pawn pawn = __instance.pawn;
                if (pawn.story != null)
                {
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird) && __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamed("WordOf") && pawn.psychicEntropy != null)
                    {
                        pawn.psychicEntropy.OffsetPsyfocusDirectly(-0.075f);
                    }
                    if (pawn.Map != null)
                    {
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitElectrophorus))
                        {
                            List<Pawn> pawns = new List<Pawn>();
                            foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (p.HostileTo(pawn))
                                {
                                    pawns.Add(p);
                                }
                            }
                            if (pawns.Count > 0)
                            {
                                int maxStrikes = Math.Max(1, (int)pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                                while (maxStrikes > 0)
                                {
                                    Pawn p = pawns.RandomElement<Pawn>();
                                    if (p != pawn && p.Position.DistanceTo(pawn.Position) <= 45f)
                                    {
                                        SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(pawn.Map);
                                        IntVec3 strikeLoc = p.Position;
                                        if (!strikeLoc.IsValid)
                                        {
                                            strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Standable(p.Map) && !p.Map.roofGrid.Roofed(sq), p.Map, 1000);
                                        }
                                        Mesh boltMesh = LightningBoltMeshPool.RandomBoltMesh;
                                        if (!strikeLoc.Fogged(p.Map))
                                        {
                                            GenExplosion.DoExplosion(strikeLoc, p.Map, 1.9f, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                                            Vector3 loc = strikeLoc.ToVector3Shifted();
                                            for (int i = 0; i < 4; i++)
                                            {
                                                FleckMaker.ThrowSmoke(loc, p.Map, 1.5f);
                                                FleckMaker.ThrowMicroSparks(loc, p.Map);
                                                FleckMaker.ThrowLightningGlow(loc, p.Map, 1.5f);
                                            }
                                        }
                                        SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, p.Map, false), MaintenanceType.None);
                                        SoundDefOf.Thunder_OnMap.PlayOneShot(info);
                                        Graphics.DrawMesh(boltMesh, strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity, FadedMaterialPool.FadedVersionOf(MatLoader.LoadMat("Weather/LightningBolt", -1), 1f), 0);
                                        maxStrikes--;
                                    }
                                }
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOrbWeaver))
                        {
                            FleckMaker.Static(pawn.Position, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 6f);
                            foreach (Plant plant in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Plant>().Distinct<Plant>())
                            {
                                plant.Growth += 0.1f * pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                                plant.DirtyMapMesh(plant.Map);
                            }
                            if (pawn.Faction != null)
                            {
                                foreach (Building building in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Building>().Distinct<Building>())
                                {
                                    if (building.Faction != null && building.Faction == pawn.Faction)
                                    {
                                        building.HitPoints += (int)(0.1f * building.MaxHitPoints * pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                                    }
                                }
                                List<Pawn> eligiblePatients = new List<Pawn>();
                                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>().Distinct<Pawn>())
                                {
                                    if (!p.HostileTo(pawn) && !pawn.HostileTo(p) && p.health.summaryHealth.SummaryHealthPercent < 1f)
                                    {
                                        eligiblePatients.Add(p);
                                    }
                                }
                                if (eligiblePatients.Count > 0)
                                {
                                    Pawn p = eligiblePatients.RandomElement<Pawn>();
                                    HautsUtility.StatScalingHeal(Math.Max(2f, Rand.Value * 10f), StatDefOf.PsychicSensitivity, p, p);
                                }
                            }
                            if (ModsConfig.BiotechActive)
                            {
                                foreach (Pawn pawn2 in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>())
                                {
                                    if (!pawn.HostileTo(pawn2) && !pawn2.HostileTo(pawn) && pawn2.RaceProps.IsMechanoid && MechRepairUtility.CanRepair(pawn2))
                                    {
                                        for (int i = 0; i < Math.Floor(4 * pawn.GetStatValue(StatDefOf.PsychicSensitivity)); i++)
                                        {
                                            MechRepairUtility.RepairTick(pawn2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void MakeGoodEvent(Pawn p)
        {
            List<IncidentDef> incidents = HautsUtility.goodEventPool;
            if (incidents.Count > 0)
            {
                bool incidentFired = false;
                int tries = 0;
                while (!incidentFired && tries <= 50)
                {
                    IncidentDef toTryFiring = incidents.RandomElement<IncidentDef>();
                    IncidentParms parms = null;
                    if (toTryFiring.TargetAllowed(Find.World))
                    {
                        parms = new IncidentParms
                        {
                            target = Find.World
                        };
                    }
                    else if (Find.Maps.Count > 0)
                    {
                        Map mapToHit = Find.Maps.RandomElement<Map>();
                        if (Find.AnyPlayerHomeMap != null && Rand.Value <= 0.5f)
                        {
                            mapToHit = Find.AnyPlayerHomeMap;
                        }
                        parms = new IncidentParms
                        {
                            target = mapToHit
                        };
                    }
                    if (parms != null)
                    {
                        IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(toTryFiring.category, parms.target);
                        incidentParms.forced = true;
                        if (toTryFiring.Worker.CanFireNow(parms))
                        {
                            incidentFired = true;
                            toTryFiring.Worker.TryExecute(parms);
                            break;
                        }
                    }
                    tries++;
                }
            }
        }
        public static void DoCanaryEffects(Pawn canary, Pawn p)
        {
            float magnitude = canary.health.capacities.GetLevel(PawnCapacityDefOf.Talking) * p.GetStatValue(StatDefOf.PsychicSensitivity);
            if (p.guest != null)
            {
                if (p.Faction == null || p.Faction != Faction.OfPlayerSilentFail || p.guest.IsPrisoner)
                {
                    p.guest.Recruitable = true;
                }
                if (p.guest.IsPrisoner)
                {
                    p.guest.resistance -= Math.Max(1f, 2f * magnitude);
                    if (p.guest.resistance < 0f)
                    {
                        p.guest.resistance = 0f;
                    }
                    if (ModsConfig.IdeologyActive)
                    {
                        p.guest.will -= Math.Max(0.2f, 0.4f * magnitude);
                        if (p.guest.will < 0f)
                        {
                            p.guest.will = 0f;
                        }
                    }
                }
            }
            bool shouldBoostMood = true;
            if (ModsConfig.IdeologyActive)
            {
                if (canary.ideo != null && p.ideo != null && canary.ideo.Ideo != null && p.ideo.Ideo != null)
                {
                    if (canary.ideo.Ideo != p.ideo.Ideo)
                    {
                        shouldBoostMood = false;
                        Ideo ideo = p.Ideo;
                        Precept_Role role = ideo.GetRole(p);
                        float num = Math.Max(0.01f, InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(canary, p) * magnitude);
                        if (p.ideo.IdeoConversionAttempt(num, canary.Ideo, true))
                        {
                            p.ideo.SetIdeo(canary.Ideo);
                            if (PawnUtility.ShouldSendNotificationAbout(canary) || PawnUtility.ShouldSendNotificationAbout(p))
                            {
                                string letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                                string letterText = "LetterConvertIdeoAttempt_Success".Translate(canary.Named("INITIATOR"), p.Named("RECIPIENT"), canary.Ideo.Named("IDEO"), ideo.Named("OLDIDEO")).Resolve();
                                LetterDef letterDef = LetterDefOf.PositiveEvent;
                                LookTargets lookTargets = new LookTargets(new TargetInfo[] { canary, p });
                                if (role != null)
                                {
                                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(p.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                                }
                                Find.LetterStack.ReceiveLetter(letterLabel, letterText, letterDef, lookTargets ?? canary, null, null, null, null, 0, true);
                            }
                        }
                    }
                }
            }
            if (shouldBoostMood && p.needs.mood != null)
            {
                p.needs.mood.thoughts.memories.TryGainMemory(HVTRoyaltyDefOf.HVT_CanarySong);
            }
        }
        public static void KeaOffsetPsyfocusLearning(Pawn pawn, float offset, float languageChance)
        {
            if (pawn.skills != null)
            {
                int maxSkill = 0;
                if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                {
                    Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                    if (psylink != null)
                    {
                        maxSkill = Math.Max((int)(psylink.level / 4), 1);
                    }
                } else {
                    maxSkill = 3 * pawn.GetPsylinkLevel();
                }
                foreach (SkillRecord skillRecord in pawn.skills.skills)
                {
                    if (skillRecord.Level < maxSkill)
                    {
                        skillRecord.Learn(offset * 5000f, true);
                        if (skillRecord.Level >= maxSkill)
                        {
                            skillRecord.Level = maxSkill;
                        }
                    }
                }
                if (Rand.Chance(languageChance))
                {
                    HautsUtility.LearnLanguage(pawn, null, 0.06f);
                }
            }
        }
        public static void LocustVanish(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Filth_Slime);
                DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Direct, null, null, default);
            }
            pawn.Destroy(DestroyMode.Vanish);
        }
        public static void MynahAbilityCopy(Pawn pawn, Pawn pawn2)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitMynah))
            {
                foreach (Ability a in pawn2.abilities.abilities)
                {
                    if (a is Psycast && Rand.Chance(0.2f) && pawn.abilities.GetAbility(a.def) == null)
                    {
                        pawn.abilities.GainAbility(a.def);
                        if (PawnUtility.ShouldSendNotificationAbout(pawn))
                        {
                            Messages.Message("HVT_MynahLearnAbility".Translate().CapitalizeFirst().Formatted(pawn.Name.ToStringShort, a.def.LabelCap, pawn2.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent, true);
                        }
                    }
                }
                VFECore.Abilities.CompAbilities comp = pawn.GetComp<VFECore.Abilities.CompAbilities>();
                VFECore.Abilities.CompAbilities comp2 = pawn2.GetComp<VFECore.Abilities.CompAbilities>();
                if (comp != null && comp2 != null)
                {
                    foreach (VFECore.Abilities.Ability a in comp2.LearnedAbilities)
                    {
                        if (HautsUtility.IsVPEPsycast(a) && Rand.Chance(0.2f) && !comp.HasAbility(a.def))
                        {
                            comp.GiveAbility(a.def);
                            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                            {
                                Messages.Message("HVT_MynahLearnAbility".Translate().CapitalizeFirst().Formatted(pawn.Name.ToStringShort, a.def.LabelCap, pawn2.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent, true);
                            }
                        }
                    }
                }
            }
        }
        public static void XerigiumHeal(Pawn doctor, Pawn patient)
        {
            List<Hediff> toHeals = new List<Hediff>();
            foreach (Hediff h in patient.health.hediffSet.hediffs)
            {
                if ((h is Hediff_Injury && !h.IsPermanent()) || (h is HediffWithComps hwc && hwc.TryGetComp<HediffComp_Immunizable>() != null))
                {
                    toHeals.Add(h);
                }
            }
            if (toHeals.Count > 0)
            {
                patient.health.RemoveHediff(toHeals.RandomElement());
            }
        }
        public static bool IsEligibleForWraithJump(Pawn p, Pawn wraith)
        {
            if (p.story != null && (p.Faction == null || wraith.Faction == null || p.Faction != wraith.Faction))
            {
                return true;
            }
            return false;
        }
        public static void WraithTransfer(Pawn pawn)
        {
            List<Pawn> eligiblePawns = new List<Pawn>();
            if (pawn.Faction != null)
            {
                if (pawn.Map != null)
                {
                    if (pawn.Map.mapPawns.AllHumanlikeSpawned.Count > 1)
                    {
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    } else {
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawns)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                } else if (pawn.GetCaravan() != null) {
                    foreach (Pawn p in pawn.GetCaravan().PawnsListForReading)
                    {
                        if (IsEligibleForWraithJump(p, pawn))
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (IsEligibleForWraithJump(p, pawn))
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
            }
            if (eligiblePawns.Count == 0)
            {
                if (pawn.Map != null)
                {
                    foreach (Pawn p in pawn.Map.mapPawns.AllPawns)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else if (pawn.GetCaravan() != null) {
                    foreach (Pawn p in pawn.GetCaravan().PawnsListForReading)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                } else {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (p.story != null)
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
            }
            if (eligiblePawns.Count > 0)
            {
                List<Pawn> pawnsToRemove = new List<Pawn>();
                if (eligiblePawns.Contains(pawn))
                {
                    pawnsToRemove.Add(pawn);
                }
                foreach (Pawn p in eligiblePawns)
                {
                    if (p.GetStatValue(StatDefOf.PsychicSensitivity) < 1E-45f || PsychicAwakeningUtility.IsAwakenedPsychic(p) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
                    {
                        pawnsToRemove.Add(p);
                    }
                }
                foreach (Pawn p in pawnsToRemove)
                {
                    eligiblePawns.Remove(p);
                }
                if (eligiblePawns.Count > 0)
                {
                    Pawn newHost = eligiblePawns.RandomElement();
                    //name change
                    TaggedString newHostOldName = newHost.Name.ToStringFull;
                    newHost.Name = NameTriple.FromString(pawn.Name.ToString(), false);
                    //replace faction.
                    Faction newHostOldFaction = newHost.Faction;
                    if (pawn.Faction != null && (newHost.Faction == null || newHost.Faction != pawn.Faction))
                    {
                        newHost.SetFaction(pawn.Faction);
                    }
                    //clear all relations and thoughts first, not replacing them w/ anything. Do NOT remove other pawns' relations with the host, except for the wraith
                    List<DirectPawnRelation> dprsToRemove = new List<DirectPawnRelation>();
                    List<DirectPawnRelation> dprsToTransfer = new List<DirectPawnRelation>();
                    List<Trait> traitsToRemove = new List<Trait>();
                    List<TraitDef> wokeTraitPool = new List<TraitDef>();
                    List<GeneDef> wokeGenePool = new List<GeneDef>();
                    GeneDef transferredGene = null;
                    Hediff_Wraithly hediff = (Hediff_Wraithly)pawn.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
                    if (newHost.relations != null)
                    {
                        foreach (DirectPawnRelation dpr in newHost.relations.DirectRelations)
                        {
                            dprsToRemove.Add(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToRemove)
                        {
                            newHost.relations.RemoveDirectRelation(dpr);
                        }
                    }
                    dprsToRemove.Clear();
                    if (pawn.relations != null)
                    {
                        foreach (DirectPawnRelation dpr in pawn.relations.DirectRelations)
                        {
                            if (dpr.otherPawn != newHost)
                            {
                                dprsToTransfer.Add(dpr);
                            }
                        }
                        foreach (DirectPawnRelation dpr in pawn.relations.DirectRelations)
                        {
                            dprsToRemove.Add(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToRemove)
                        {
                            pawn.relations.RemoveDirectRelation(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToTransfer)
                        {
                            newHost.relations.AddDirectRelation(dpr.def, dpr.otherPawn);
                        }
                    }
                    newHost.story.favoriteColor = pawn.story.favoriteColor;
                    //replace ideoligion
                    if (ModsConfig.IdeologyActive && newHost.ideo != null && pawn.ideo != null)
                    {
                        newHost.ideo.SetIdeo(pawn.ideo.Ideo);
                    }
                    //replace all backstories
                    newHost.story.Childhood = pawn.story.Childhood;
                    newHost.story.Adulthood = pawn.story.Adulthood;
                    //replace all non-genetic, non-exemption, non-wokepsychic traits; possession can only transfer ONE wokening, but any number of transcendences is fine
                    foreach (Trait t in newHost.story.traits.allTraits)
                    {
                        if (!HautsUtility.IsExciseTraitExempt(t.def) && t.sourceGene == null)
                        {
                            traitsToRemove.Add(t);
                        }
                    }
                    foreach (Trait t in traitsToRemove)
                    {
                        newHost.story.traits.RemoveTrait(t);
                    }
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (t.sourceGene == null && !PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                        {
                            if (PsychicAwakeningUtility.IsAwakenedTrait(t.def))
                            {
                                wokeTraitPool.Add(t.def);
                            }
                            else if (!HautsUtility.IsExciseTraitExempt(t.def))
                            {
                                newHost.story.traits.GainTrait(new Trait(t.def, t.Degree));
                            }
                        }
                    }
                    //add up to one woke trait or gene... otherwise, transes can't be passed on. If it somehow isn't possible to pass one over, forcibly awaken the new host
                    if (wokeTraitPool.Count > 0)
                    {
                        newHost.story.traits.GainTrait(new Trait(wokeTraitPool.RandomElement()));
                    } else if (ModsConfig.BiotechActive && newHost.genes != null && pawn.genes != null) {
                        foreach (Gene g in pawn.genes.GenesListForReading)
                        {
                            if (PsychicAwakeningUtility.IsAwakenedPsychicGene(g.def))
                            {
                                wokeGenePool.Add(g.def);
                            }
                        }
                        if (wokeGenePool.Count > 0)
                        {
                            transferredGene = wokeGenePool.RandomElement();
                            if (hediff != null)
                            {
                                hediff.geneToRemove = transferredGene;
                            }
                            newHost.genes.AddGene(transferredGene, true);
                        }
                    } else {
                        PsychicAwakeningUtility.AwakenPsychicTalent(newHost, false, "", "", true);
                    }
                    //NOW we can add the trans traits
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                        {
                            newHost.story.traits.GainTrait(new Trait(t.def));
                        }
                    }
                    //replace all skills & passions
                    newHost.skills.skills.Clear();
                    foreach (SkillRecord skillRecord in pawn.skills.skills)
                    {
                        SkillRecord item = new SkillRecord(newHost, skillRecord.def)
                        {
                            levelInt = skillRecord.levelInt,
                            passion = skillRecord.passion,
                            xpSinceLastLevel = skillRecord.xpSinceLastLevel,
                            xpSinceMidnight = skillRecord.xpSinceMidnight
                        };
                        newHost.skills.skills.Add(item);
                    }
                    //make host's psylink level at least as high as the wraith's
                    if (pawn.GetMainPsylinkSource() != null)
                    {
                        int levelDifference = pawn.GetPsylinkLevel() - newHost.GetPsylinkLevel();
                        if (levelDifference > 0)
                        {
                            newHost.ChangePsylinkLevel(levelDifference, false);
                        }
                    }
                    //NOW we can replace thoughts. old situational thoughts don't transfer, just memories
                    if (pawn.needs.mood != null && newHost.needs.mood != null)
                    {
                        List<Thought_Memory> memories = newHost.needs.mood.thoughts.memories.Memories;
                        memories.Clear();
                        foreach (Thought_Memory thought_Memory in pawn.needs.mood.thoughts.memories.Memories)
                        {
                            Thought_Memory thought_Memory2 = (Thought_Memory)ThoughtMaker.MakeThought(thought_Memory.def);
                            thought_Memory2.CopyFrom(thought_Memory);
                            thought_Memory2.pawn = newHost;
                            memories.Add(thought_Memory2);
                        }
                    }
                    //set hediff severity so that there's a 1-hour cooldown
                    Hediff hediff2 = newHost.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
                    if (hediff2 != null)
                    {
                        hediff2.Severity = 23f;
                    } else {
                        Hediff wraithCD = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_THediffWraith,newHost);
                        newHost.health.AddHediff(wraithCD);
                        wraithCD.Severity = 23f;
                    }
                    //psycast copy-over
                    List<AbilityDef> abilitiesToRemove = new List<AbilityDef>();
                    foreach (Ability a in newHost.abilities.abilities)
                    {
                        if (!(a is Psycast))
                        {
                            abilitiesToRemove.Add(a.def);
                        }
                    }
                    foreach (AbilityDef ad in abilitiesToRemove)
                    {
                        newHost.abilities.RemoveAbility(ad);
                    }
                    foreach (Ability ability in pawn.abilities.abilities)
                    {
                        if (newHost.abilities.GetAbility(ability.def, false) == null && ability is Psycast)
                        {
                            newHost.abilities.GainAbility(ability.def);
                        }
                    }
                    VFECore.Abilities.CompAbilities comp = pawn.GetComp<VFECore.Abilities.CompAbilities>();
                    VFECore.Abilities.CompAbilities comp2 = newHost.GetComp<VFECore.Abilities.CompAbilities>();
                    if (comp != null && comp2 != null)
                    {
                        List<VFECore.Abilities.Ability> learnedAbilities = HautsTraitsRoyalty.GetInstanceField(typeof(VFECore.Abilities.CompAbilities), comp, "learnedAbilities") as List<VFECore.Abilities.Ability>;
                        List<VFECore.Abilities.Ability> learnedAbilities2 = HautsTraitsRoyalty.GetInstanceField(typeof(VFECore.Abilities.CompAbilities), comp2, "learnedAbilities") as List<VFECore.Abilities.Ability>;
                        for (int i = learnedAbilities2.Count - 1; i>=0; i--)
                        {
                            if (HautsUtility.IsVPEPsycast(learnedAbilities2[i]))
                            {
                                learnedAbilities2.RemoveAt(i);
                            }
                        }
                        for (int i = 0; i < learnedAbilities.Count; i++)
                        {
                            if (!comp2.HasAbility(learnedAbilities[i].def) && HautsUtility.IsVPEPsycast(learnedAbilities[i]))
                            {
                                comp2.GiveAbility(learnedAbilities[i].def);
                                HautsUtility.VPEUnlockAbility(newHost, learnedAbilities[i].def);
                                HautsUtility.VPESetSkillPointsAndExperienceTo(newHost,pawn);
                            }
                        }
                    }
                    //I rewrote a lot of Wraith to use the code from Anomaly's duplication, but this is the only thing I've 100% ripped off bc I didn't even think of it
                    if (pawn.guest != null && newHost.guest != null)
                    {
                        newHost.guest.Recruitable = pawn.guest.Recruitable;
                    }
                    newHost.Drawer.renderer.SetAllGraphicsDirty();
                    newHost.Notify_DisabledWorkTypesChanged();
                    //send you a letter
                    LookTargets lt = null;
                    LetterDef letterNature = null;
                    if (pawn.Faction == Faction.OfPlayerSilentFail)
                    {
                        if (newHost.Spawned)
                        {
                            lt = newHost;
                        }
                        letterNature = LetterDefOf.PositiveEvent;
                    }
                    else if (newHostOldFaction == Faction.OfPlayerSilentFail)
                    {
                        if (newHost.Spawned)
                        {
                            lt = newHost;
                        }
                        letterNature = LetterDefOf.NegativeEvent;
                    }
                    if (letterNature != null)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                "HVT_WraithLabel".Translate(newHost.Name.ToStringShort), "HVT_WraithText".Translate(pawn.Name.ToStringFull, newHostOldName).CapitalizeFirst(), letterNature, lt, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    Hediff hediff3 = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_Wraithform, newHost, null);
                    newHost.health.AddHediff(hediff3);
                    hediff3.Severity = hediff3.def.maxSeverity;
                    FleckMaker.Static(newHost.Position, newHost.MapHeld, FleckDefOf.PsycastAreaEffect, 6f);
                    foreach (Thing thing in GenRadial.RadialDistinctThingsAround(newHost.Position, newHost.Map, 12f, true))
                    {
                        if (newHost.Faction == null || thing.Faction != newHost.Faction)
                        {
                            if (thing is Pawn p)
                            {
                                Pawn_StanceTracker stances = p.stances;
                                if (stances != null)
                                {
                                    StunHandler stunner = stances.stunner;
                                    if (stunner != null)
                                    {
                                        stunner.StunFor((int)(Rand.Value * 6f * newHost.GetStatValue(StatDefOf.PsychicSensitivity)), newHost, false);
                                    }
                                }
                            } else if (thing is Building b) {
                                CompStunnable comp3 = b.GetComp<CompStunnable>();
                                if (comp3 != null)
                                {
                                    comp3.StunHandler.StunFor((int)(Rand.Value * 6f * newHost.GetStatValue(StatDefOf.PsychicSensitivity)), newHost, false);
                                }
                            }
                        }
                    }
                    //what if lost????
                    if (!newHost.SpawnedOrAnyParentSpawned && newHost.GetCaravan() == null)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                "HVT_WraithLabel".Translate(newHost.Name.ToStringShort), "HVT_WraithLost".Translate(pawn.Name.ToStringFull, newHostOldName).CapitalizeFirst(), LetterDefOf.Death, null, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    if (hediff != null)
                    {
                        hediff.transferredOut = true;
                    }
                }
            }
        }
        /*the following tools for modders adding new awakenings or transcendences*/
        public static void AddAwakeningTrait(TraitDef def)
        {
            awakenings.Add(def);
        }
        public static void AddTranscendentTrait(TraitDef def)
        {
            regularTranses.Add(def);
        }
        public static void AddMythicTranscendentTrait(TraitDef def)
        {
            mythicTranses.Add(def);
        }
        public static void AddWokeGene(GeneDef def)
        {
            wokeGenes.Add(def);
        }
        private static readonly List<TraitDef> tempTranses = new List<TraitDef>();
        private static readonly List<TraitDef> regularTranses = new List<TraitDef>();
        private static readonly List<TraitDef> mythicTranses = new List<TraitDef>();
        private static readonly List<TraitDef> tmpAwakenings = new List<TraitDef>();
        private static readonly List<TraitDef> awakenings = new List<TraitDef>();
        private static readonly List<GeneDef> wokeGenes = new List<GeneDef>();
    }
}
