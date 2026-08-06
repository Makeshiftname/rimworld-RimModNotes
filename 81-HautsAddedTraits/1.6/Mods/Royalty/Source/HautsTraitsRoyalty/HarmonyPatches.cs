using HarmonyLib;
using HautsFramework;
using HautsTraits;
using RimWorld;
using RimWorld.Planet;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using MVCF.Utilities;

namespace HautsTraitsRoyalty
{
    [StaticConstructorOnStartup]
    public static class HautsTraitsRoyalty
    {
        private static readonly Type patchType = typeof(HautsTraitsRoyalty);
        static HautsTraitsRoyalty()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautstraitsroyalty.main");
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyGainTraitPrefix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyGainTraitPostfix)));
           harmony.Patch(AccessTools.Method(typeof(TraitModExtensionUtility), nameof(TraitModExtensionUtility.TraitGrantedStuffRegeneration)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyTraitGrantedStuffRegenerationPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MentalState), nameof(MentalState.RecoverFromState)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsRoyaltyRecoverFromStatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsNotify_PawnKilledPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.Notify_PawnDied)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsNotify_PawnDiedPostifx)));
            harmony.Patch(AccessTools.Method(typeof(Hediff_Level), nameof(Hediff_Level.ChangeLevel)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsLPMastery_ChangeLevelPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Hediff_Psylink), nameof(Hediff_Psylink.PostAdd)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_PostAddPostfix)));
            harmony.Patch(AccessTools.Method(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) }),
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
            harmony.Patch(AccessTools.Method(typeof(PawnBanishUtility), nameof(PawnBanishUtility.Banish), new[] { typeof(Pawn), typeof(bool) }),
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
                harmony.Patch(AccessTools.Method(typeof(Hediff_BloodRage), nameof(Hediff_BloodRage.PostAdd)),
                              postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_BloodRage_PostAddPostfix)));
            }
            harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.AddGene), new[] { typeof(GeneDef), typeof(bool) }),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_AddGenePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.AddGene), new[] { typeof(GeneDef), typeof(bool) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsAA_AddGenePostfix)));
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
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_Neuroquake), nameof(CompAbilityEffect_Neuroquake.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_Neuroquake_ApplyPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                           prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_IngestedPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_IngestedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsMiscUtility), nameof(HautsMiscUtility.TotalPsyfocusRefund)),
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
            harmony.Patch(AccessTools.Method(typeof(VEF.Abilities.Ability), nameof(VEF.Abilities.Ability.GetRangeForPawn)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GetRangeForPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_TryInteractWithPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Psycast), nameof(Psycast.CanApplyPsycastTo)),
                          postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_CanApplyPsycastToPostfix)));
            if (ModsConfig.OdysseyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(GravshipUtility), nameof(GravshipUtility.AbandonMap)),
                               prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GravshipUtility_AbandonMapPrefix)));
                harmony.Patch(AccessTools.Method(typeof(FishingUtility), nameof(FishingUtility.GetCatchesFor)),
                               postfix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_GetCatchesForPostfix)));
                MethodInfo methodInfoCC = typeof(CompCerebrexCore).GetMethod("DeactivateCore", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfoCC,
                              prefix: new HarmonyMethod(patchType, nameof(HautsTraitsTrans_DeactivateCorePrefix)));
            }
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.COaNN_TraitReset_ShouldDoBonusEffect)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsCOaNNIsLatentPsychicPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.COaNN_TraitReset_BonusEffects)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsCOaNNAwakenPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ModCompatibilityUtility), nameof(ModCompatibilityUtility.IsAwakenedPsychic)),
                           postfix: new HarmonyMethod(patchType, nameof(HautsTraitsIsAwakenedPsychicPostfix)));
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefs)
            {
                if (t.HasModExtension<SuperPsychicTrait>())
                {
                    if (t.GetModExtension<SuperPsychicTrait>().category == "awakening")
                    {
                        PsychicTraitAndGeneCheckUtility.AddAwakeningTrait(t);
                    } else if (t.GetModExtension<SuperPsychicTrait>().category == "transcendence") {
                        PsychicTraitAndGeneCheckUtility.AddTranscendentTrait(t);
                    } else if (t.GetModExtension<SuperPsychicTrait>().category == "mythic") {
                        PsychicTraitAndGeneCheckUtility.AddMythicTranscendentTrait(t);
                    }
                    TraitModExtensionUtility.AddExciseTraitExemption(t);
                }
            }
            foreach (GeneDef g in DefDatabase<GeneDef>.AllDefs)
            {
                if (g.HasModExtension<SuperPsychicGene>() && g.GetModExtension<SuperPsychicGene>().category == "awakening")
                {
                    PsychicTraitAndGeneCheckUtility.AddWokeGene(g);
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
        /*Psychic sensitivity-reducing traits can't be given to woke pawns.
         * Awakenings can't be given to Anomaly mutants with 0x psychic sensitivity. Their addition removes all psysens-reducing genes and traits, as well as Latent Psychic.
         * Latent Psychic can't be given to pawns that are already woke (either due to having woke traits or woke genes).
         * Transes can't be given to Anomaly mutants. They also can't be given to non-woke pawns.*/
        public static bool HautsTraitsRoyaltyGainTraitPrefix(TraitSet __instance, Trait trait, bool suppressConflicts)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn,false) && PsychicTraitAndGeneCheckUtility.IsAntipsychicTrait(trait.def, trait.Degree))
            {
                return false;
            }
            if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(trait.def))
            {
                if (AwakeningMethodsUtility.PsychicDeafMutantDeafInteraction(pawn, false))
                {
                    Log.Error("HVT_NoWokeMutants".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    return false;
                }
                if (ModsConfig.BiotechActive && pawn.genes != null)
                {
                    List<Gene> genesToRemove = new List<Gene>();
                    foreach (Gene g in pawn.genes.GenesListForReading)
                    {
                        if (PsychicTraitAndGeneCheckUtility.IsAntipsychicGene(g.def))
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
                    if (PsychicTraitAndGeneCheckUtility.IsAntipsychicTrait(t.def, t.Degree) || t.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
                    {
                        traitsToRemove.Add(t);
                    }
                }
                foreach (Trait t in traitsToRemove)
                {
                    __instance.RemoveTrait(t);
                }
            } else if (trait.def == HVTRoyaltyDefOf.HVT_LatentPsychic) {
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    return false;
                }
            } else if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(trait.def)) {
                if (AwakeningMethodsUtility.PsychicDeafMutantDeafInteraction(pawn, false))
                {
                    Log.Error("HVT_NoTransMutants".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    return false;
                }
                if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    Log.Error("HVT_CantGrantTrans".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                    AwakeningMethodsUtility.AwakenPsychicTalent(pawn, false, "HVT_WokeningDefault".Translate(), "HVT_WokeningDefaultFantasy".Translate());
                    return false;
                }
            }
            return true;
        }
        /*Defense in depth: if you somehow added Latent Psychic to a woke pawn anyways, LP will get removed
         * Handles some effects of awakening:
         * -HVT_LatentPsyTerminus is granted for about half an hour. It has to be added before any psylink additions from this patch, because we don't want pawns to awaken AND transcend off the same level, and Terminus prevents transcendence.
         * -All awakened traits gain their AwakenedDeathTracker, whose comps handle Revelation letters and some transcendence methods (most notably dying, hence the name).
         * -All awakened traits grant +1 psylink level (or +2 if the pawn had no psylinks beforehand).
         * -Erudite grants up to 10 psycasts and another psylink level.
         * -All transcendences grant the generic transcendence buff, which boosts consciousness + psysens and is responsible for the lensing warble VFX.
         * -Some transcendences have effects that confer bonuses over time. Therefore, if a pawn is generated with them, we should 'simulate' a history of their having buffed themselves earlier. This is usually handled via their hediff comps,
         *   but for those who don't have any unique ones... Sphinx gains a psylink level and bonus psycasts, while Thunderbird is assumed to have used its ability on itself to gain another awakening.
         * -Some transes have effects that only benefit the use of Word or Skip psycasts, so to ensure they aren't useless they also grant a Word or Skip psycast at a level the pawn could know (if there are any that haven't been learned yet).*/
        public static void HautsTraitsRoyaltyGainTraitPostfix(TraitSet __instance, Trait trait)
        {
            Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
            if (trait.def == HVTRoyaltyDefOf.HVT_LatentPsychic)
            {
                bool removeLP = false;
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                    {
                        removeLP = true;
                        break;
                    }
                }
                if (removeLP)
                {
                    pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic));
                }
            } else if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(trait.def)) {
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
                    PsychicPowerUtility.GrantEruditeEffects(pawn, 10);
                }
            } else if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(trait.def)) {
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff))
                {
                    Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_TranscendentHediff, pawn, null);
                    pawn.health.AddHediff(hediff, null, null, null);
                }
                if (PawnGenerator.IsBeingGenerated(pawn))
                {
                    if (trait.def == HVTRoyaltyDefOf.HVT_TTraitSphinx)
                    {
                        PsychicPowerUtility.GrantEruditeEffects(pawn, 2);
                    }
                    if (trait.def == HVTRoyaltyDefOf.HVT_TTraitThunderbird)
                    {
                        AwakeningMethodsUtility.AwakenPsychicTalent(pawn, false, "", "", true);
                    }
                }
                //if the trans trait was blocked due to not having a woke trait or gene, remove any hediffs and abilities it was going to grant
                if (!pawn.story.traits.HasTrait(trait.def))
                {
                    for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                    {
                        HediffComp_ForcedByOtherProperty fobp = pawn.health.hediffSet.hediffs[i].TryGetComp<HediffComp_ForcedByOtherProperty>();
                        if (fobp != null && fobp.Props.forcingTraits.Contains(trait.def))
                        {
                            pawn.health.RemoveHediff(pawn.health.hediffSet.hediffs[i]);
                        }
                    }
                    if (pawn.abilities != null && pawn.abilities.abilities != null)
                    {
                        for (int i = pawn.abilities.abilities.Count - 1; i >= 0; i--)
                        {
                            CompAbilityEffect_ForcedByOtherProperty fobp = pawn.abilities.abilities[i].CompOfType<CompAbilityEffect_ForcedByOtherProperty>();
                            if (fobp != null && fobp.Props.forcingTraits.Contains(trait.def))
                            {
                                pawn.abilities.RemoveAbility(pawn.abilities.abilities[i].def);
                            }
                        }
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
        /*For compatibility with effects that add traits without invoking GainTrait (COUGH COUGH character editor), the TGS Regeneration applied to all pawns near the start of gametime (and on an individual basis via DEV: Fix TraitGrantedStuff button)
         * also does the effects of the GainTrait postfix up above*/
        public static void HautsTraitsRoyaltyTraitGrantedStuffRegenerationPostfix(Pawn p)
        {
            if (p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                foreach (Trait t in p.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                    {
                        p.story.traits.RemoveTrait(p.story.traits.GetTrait(HVTRoyaltyDefOf.HVT_LatentPsychic));
                        break;
                    }
                }
            } else {
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p) && !p.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker))
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
                        PsychicPowerUtility.GrantEruditeEffects(p, 10);
                    }
                    if (ModsConfig.BiotechActive && p.genes != null)
                    {
                        List<Gene> genesToRemove = new List<Gene>();
                        foreach (Gene g in p.genes.GenesListForReading)
                        {
                            if (PsychicTraitAndGeneCheckUtility.IsAntipsychicGene(g.def))
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
                        if (PsychicTraitAndGeneCheckUtility.IsAntipsychicTrait(t.def, t.Degree))
                        {
                            traitsToRemove.Add(t);
                        }
                    }
                    foreach (Trait t in traitsToRemove)
                    {
                        p.story.traits.RemoveTrait(t);
                    }
                }
                if (PsychicTraitAndGeneCheckUtility.IsTranscendent(p))
                {
                    if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p))
                    {
                        Log.Error("HVT_CantGrantTrans".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve());
                        List<Trait> transesToRemove = new List<Trait>();
                        foreach (Trait t in p.story.traits.allTraits)
                        {
                            if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
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
        //Trans Ravens generate a good event when they recover from any "normal" mental state (not fleeing the map, not social fighting, not psycast-induced)
        public static void HautsTraitsRoyaltyRecoverFromStatePostfix(MentalState __instance)
        {
            if (__instance.pawn.story != null)
            {
                if (__instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRaven) && __instance.def != MentalStateDefOf.PanicFlee && __instance.def != MentalStateDefOf.SocialFighting && !__instance.causedByPsycast)
                {
                    for (int i = 0; i < Math.Min(Math.Max(1f,(int)Math.Floor(__instance.pawn.GetStatValue(StatDefOf.PsychicSensitivity))),40); i++)
                    {
                        GoodAndBadIncidentsUtility.MakeGoodEvent(__instance.pawn,0,null);
                    }
                }
            }
        }
        //Trans Shrikes have a psysens-scaling chance (capped at 15%) to create a good event on killing a humanlike pawn
        public static void HautsTraitsNotify_PawnKilledPostfix(Pawn killed, Pawn killer)
        {
            if (killer.story != null)
            {
                if (killed.RaceProps.Humanlike && killer.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitShrike) && Rand.Chance(Math.Max(0.01f * killer.GetStatValue(StatDefOf.PsychicSensitivity),0.15f)))
                {
                    GoodAndBadIncidentsUtility.MakeGoodEvent(killer,0,null);
                }
            }
        }
        /*when a Dulotic Transcendent kills a psychically sensitive pawn, it converts to the Dulotic's faction and ideo, resurrects after 0.1 hours, and gains a 3-day timed life condition.
         * To prevent infinite chaining of this effect, a pawn who already had this timed life condition is immune to this patch.*/
        public static void HautsTraitsNotify_PawnDiedPostifx(HediffSet __instance, DamageInfo? dinfo)
        {
            Thing instigator = dinfo.GetValueOrDefault().Instigator;
            if (instigator != null && instigator is Pawn p && p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDulotic) && __instance.pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
            {
                if (!__instance.pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_DulosisTimedLife) && __instance.pawn.SpawnedOrAnyParentSpawned)
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
                    HautsMiscUtility.StartDelayedResurrection(__instance.pawn, new IntRange(1, 1), "", false, false, true, HVTRoyaltyDefOf.HVT_DulosisTimedLife, 1f);
                }
            }
        }
        /*When a psylink level is gained, do the following:
         * -Awaken if this is the max psylink level.
         * -Remove the Psychic Censure (debuff from the Transcendent Erinys ability). This is a costly, but faster alternative to the normal way of getting rid of it, which is meditating for a long time.
         * -1% chance to transcend if the pawn is already awakened and did not just awaken in the last half-hour (which is what having Terminus signifies).
         * Despite the name, this no longer handles Mastery-type awakenings. That's done in its hediff.*/
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
                                AwakeningMethodsUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeMaxPsyLevel".Translate(), "HVT_WokeMaxPsyLevelFantasy".Translate(), false);
                            }
                        } else if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn) && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_LatentPsyTerminus) && Rand.Value <= 0.01f) {
                            TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_TransHighPsyLevel".Translate(), "HVT_TransHighPsyLevelFantasy".Translate(), 0.01f);
                        }
                    }
                }
            }
        }
        //Gaining a psylink (distinct from gaining a level) removes Psychic Censure
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
        /*On creating an item with quality...
         * -if it's legendary and the pawn is a Mastery-type LP, awaken.
         * -Sedge Warblers have a 50% chance to increase its quality by 1.
         * -Weaverbirds boost quality by 1.*/
        public static void HautsTraitsLPMastery_GenerateQualityCreatedByPawnPostfix(ref QualityCategory __result, Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (__result == QualityCategory.Legendary)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 1, true, "HVT_WokeLegendaryWork".Translate(), "HVT_WokeLegendaryWorkFantasy".Translate());
                }
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
        /*On having a non-psycast-induced mental break...
         * -If the pawn is a Misery-type LP, there's a chance to awaken (2%, or 50% if the break is extreme).
         * -If there's a psychic drone on the current map (or a droner affecting the current caravan), the break is mood-caused, and the pawn is woke, 4% chance to transcend.
         * -Noctules cause an eclipse.*/
        public static void HautsTraitsLPMisery_TryStartMentalStatePostfix(MentalStateHandler __instance, MentalStateDef stateDef, bool causedByMood, bool causedByPsycast)
        {
            Pawn pawn = GetInstanceField(typeof(MentalStateHandler), __instance, "pawn") as Pawn;
            if (!causedByPsycast && pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && (stateDef.IsExtreme && Rand.Value <= 0.5f) || Rand.Value <= 0.02f)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 2, true, "HVT_WokeMentalBreak".Translate(), "HVT_WokeMentalBreakFantasy".Translate());
                    return;
                }
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn) && causedByMood && Rand.Value <= 0.04f) {
                    if (pawn.MapHeld != null)
                    {
                        if (pawn.MapHeld.gameConditionManager.GetHighestPsychicDroneLevelFor(pawn.gender, pawn.MapHeld) >= PsychicDroneLevel.BadLow)
                        {
                            TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_TransBreak".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_TransBreakFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), 0.2f, false);
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
                            TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_TransBreak".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_TransBreakFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), 0.2f, false);
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
        /*On having an inspiration...
         * -Misery-type LPs with the Catharsis thought awaken.
         * -Noctules cause an eclipse.*/
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
                            AwakeningMethodsUtility.AwakenPsychicTalentCheck(__instance.pawn, 2, true, "HVT_WokeCatharsisInspiration".Translate(), "HVT_WokeCatharsisInspirationFantasy".Translate());
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
        //On gaining a romantic relationship, Love-type LPs have a chance to awaken (50/50/75/100% chance for bond/lover/fiance/spouse).
        public static void HautsTraitsLPLove_AddDirectRelationPostfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_RelationsTracker), __instance, "pawn") as Pawn;
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                AwakeningMethodsUtility.LPLoveCheckRelations(def,pawn,otherPawn,false);
            }
        }
        //When a colony child becomes an adult, any Love-type LPs that has 60+ opinion of them on the same map/caravan awakens.
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
                            AwakeningMethodsUtility.AwakenPsychicTalentCheck(recipient, 3, true, "HVT_WokeChildGrewUp".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")), "HVT_WokeChildGrewUpFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")));
                        }
                    }
                } else if (pawn.IsCaravanMember()) {
                    Caravan caravan = pawn.GetCaravan();
                    for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
                    {
                        Pawn recipient = caravan.PawnsListForReading[i];
                        if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && recipient.relations.OpinionOf(pawn) >= 60)
                        {
                            AwakeningMethodsUtility.AwakenPsychicTalentCheck(recipient, 3, true, "HVT_WokeChildGrewUp".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")), "HVT_WokeChildGrewUpFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")));
                        }
                    }
                }
            }
        }
        /*When a pawn dies, awaken any Loss-type LPs who...
         * -are a direct relation that held at least +/-60 opinon of the deceased.
         * -is on the same map/caravan and had at least 60 opinion of the deceased (50% chance to require 100 opinion instead).
         * -is on the same map/caravan and was bonded (the animal relationship) to the deceased (50% chance).
         * Also, Transcendent Dragons gain severity for their Dragonhoard condition (and gain it in the first place, if they don't currently have it) based on the psylink levels, psychic trait tree traits, and woke geens of the deceased*/
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
                        if ((Rand.Value <= 0.5f && (!pawn.RaceProps.Humanlike || recipient.relations.OpinionOf(pawn) >= 60 || recipient.relations.OpinionOf(pawn) <= -60)))
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
                                AwakeningMethodsUtility.AwakenPsychicTalentCheck(recipient, 4, true, triggerEvent, triggerEventFantasy, false, 0.5f);
                            }
                        }
                    }
                }
            }
            if (pawn.Map != null)
            {
                AwakeningMethodsUtility.LPLossNonRelationDeathCheck(pawn, pawn.Map.mapPawns.AllHumanlikeSpawned);
            } else if (pawn.IsCaravanMember()) {
                AwakeningMethodsUtility.LPLossNonRelationDeathCheck(pawn, pawn.GetCaravan().PawnsListForReading);
            }
        }
        //when a pawn is banished, each Loss-type LPs who was a direct relation with at least 60 opinion of that pawn has a 2% chance to awaken
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
                            AwakeningMethodsUtility.AwakenPsychicTalentCheck(recipient, 4, true, "HVT_WokeExile".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve(), "HVT_WokeExileFantasy".Translate().Formatted(pawn.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(pawn, "OTHER", true).Resolve());
                        }
                    }
                }
            }
        }
        //when a Life-type LP's birthing labor finishes, 33% chance to awaken
        public static void HautsTraitsLPLife_LaborPushing_PostRemovedPostfix(Hediff_LaborPushing __instance)
        {
            if (Rand.Value <= 0.33f && !__instance.pawn.Dead && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                AwakeningMethodsUtility.AwakenPsychicTalentCheck(__instance.pawn, 5, true, "HVT_WokeBirth".Translate().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve(), "HVT_WokeBirthFantasy".Translate().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve());
            }
        }
        //1% chance each birthday for a Life-type LP to awaken
        public static void HautsTraitsLPLife_TryChildGrowthMomentPostfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_AgeTracker), __instance, "pawn") as Pawn;
            if (Rand.Value <= 0.01f && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
            {
                AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 5, true, "HVT_WokeBirthday".Translate(pawn.ageTracker.AgeBiologicalYears, pawn.Name.ToStringShort, pawn.gender.GetPossessive()).Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeBirthdayFantasy".Translate(pawn.ageTracker.AgeBiologicalYears, pawn.Name.ToStringShort, pawn.gender.GetPossessive()).Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
        }
        /*Ensures awakened pawns die with full psyfocus and no neural heat. (Normally, you lose all psyfocus on death)
         * Despite the name, no longer handles Death-type LP awakening. Their hediff does that.*/
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
        //Embracing or disrupting the void awakens and transcends the activating pawn
        public static void HautsTraitsLPAny_EmbraceTheVoidPostfix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                {
                    AwakeningMethodsUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeVoidEmbrace".Translate(), "HVT_WokeVoidEmbraceFantasy".Translate());
                }
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_WokeVoidEmbrace2".Translate(), "HVT_WokeVoidEmbraceFantasy2".Translate(), 1f);
                }
            }
        }
        public static void HautsTraitsLPAny_DisruptTheLinkPostfix(Pawn pawn)
        {
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                {
                    AwakeningMethodsUtility.AwakenPsychicTalent(pawn, true, "HVT_WokeVoidClosure".Translate(), "HVT_WokeVoidClosureFantasy".Translate());
                }
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_WokeVoidClosure2".Translate(), "HVT_WokeVoidClosureFantasy2".Translate(), 1f);
                }
            }
        }
        //woke pawns can't gain genes that directly lower psysensitivity
        public static bool HautsTraitsAA_AddGenePrefix(Pawn_GeneTracker __instance, GeneDef geneDef)
        {
            if (__instance.pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(__instance.pawn,false) && PsychicTraitAndGeneCheckUtility.IsAntipsychicGene(geneDef))
            {
                return false;
            }
            return true;
        }
        /*handles woke gene effects
         * -they grant a psylink level if the pawn didn't have any
         * -they grant the AwakenedDeathTracker, just like woke traits do
         * -Erudite corresponding gene grants up to 10 psycasts and a bonus psylink level*/
        public static void HautsTraitsAA_AddGenePostfix(Pawn_GeneTracker __instance, GeneDef geneDef)
        {
            Pawn pawn = __instance.pawn;
            if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(geneDef))
            {
                if (!pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier))
                {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                }
                if (geneDef == HVTRoyaltyDefOf.HVT_AEruditeGene)
                {
                    PsychicPowerUtility.GrantEruditeEffects(pawn, 10);
                }
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker))
                {
                    Hediff tracker = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_AwakenedDeathTracker,pawn);
                    pawn.health.AddHediff(tracker, pawn.health.hediffSet.GetBrain(), null, null);
                }
            }
        }
        //Awakened Chanshi and its corresponding gene grant access to all meditation focus types
        public static void HautsTraitsAA_PawnCanUsePostfix(ref bool __result, Pawn p, MeditationFocusDef type)
        {
            if (p.story != null)
            {
                if (p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_AwakenedChanshi) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_ChanshiGene))
                {
                    __result = true;
                    return;
                }else {
                    bool hasAnyMFD = false;
                    foreach (Trait t in p.story.traits.allTraits)
                    {
                        if (!t.CurrentData.allowedMeditationFocusTypes.NullOrEmpty())
                        {
                            hasAnyMFD = true;
                            if (t.CurrentData.allowedMeditationFocusTypes.Contains(type))
                            {
                                __result = true;
                                return;
                            }
                        }
                    }
                    if (p.story.Childhood != null && (p.story.Childhood.spawnCategories.Contains("Tribal") || p.story.Childhood.spawnCategories.Contains("ChildTribal")))
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
        /*Handles the possibility of pawns gaining awakened or transcendent traits during their generation process
         * Having a royal title which affords a psylink as one of its privileges grants a chance to be awakened, scaling with its seniority
         * Faction leaders have +10% chance to be awakened
         * Pawns that are awakened but not transcendent have at least 2.5% chance to be transcendent, higher if their awakening chance exceeded 62.5%
         * Non-mutant Latent Psychics have a chance to spawn awakened instead. This chance is 0 by default, but mods that add psycasting AI in some form have xpath patches that raise this chance.*/
        public static void HautsTraitsAA_GeneratePawnPostfix(ref Pawn __result, PawnGenerationRequest request)
        {
            if (__result.story != null && __result.Faction != null && !__result.Dead && __result.DevelopmentalStage.Adult())
            {
                float awakenChance = 0f, transcendChance = 0.025f;
                if (!PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(__result))
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
                        AwakeningMethodsUtility.AwakenPsychicTalent(__result, false, "", "", true);
                    }
                    transcendChance = Math.Max(awakenChance - 0.6f, 0.025f);
                }
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(__result) && !PsychicTraitAndGeneCheckUtility.IsTranscendent(__result))
                {
                    if (Rand.Value <= transcendChance)
                    {
                        TranscendenceMethodsUtility.AchieveTranscendence(__result, "", "", 0f, true);
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
                            AwakeningMethodsUtility.AwakenPsychicTalent(__result,false,"","",true);
                        }
                    }
                }
            }
        }
        //Each awakened pawn on the same map as an anima tree when it releases its death scream has a 4% chance to transcend. Could possibly be handled via xpathing in another comp to anything that releases an anima scream on death, but that would incur a performance cost per such Thing
        public static void HautsTraitsTransPostDestroyPostfix(CompGiveThoughtToAllMapPawnsOnDestroy __instance, Map previousMap)
        {
            if (previousMap != null)
            {
                CompProperties_GiveThoughtToAllMapPawnsOnDestroy props = (CompProperties_GiveThoughtToAllMapPawnsOnDestroy)__instance.props;
                if (props.thought == DefDatabase<ThoughtDef>.GetNamed("AnimaScream"))
                {
                    foreach (Pawn p in previousMap.mapPawns.AllPawnsSpawned)
                    {
                        if (Rand.Value <= 0.04f && p.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p))
                        {
                            TranscendenceMethodsUtility.AchieveTranscendence(p, "HVT_TransAnimaScream".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVT_TransAnimaScreamFantasy".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), 0.1f);
                        }
                    }
                }
            }
        }
        //Transcendent Wraiths who've been alive for at least a day (as measured by their hediff's severity) will transfer to a new body when they die.
        public static void HautsTraitsTransDeath_KillPrefix(Pawn __instance)
        {
            Hediff wraith = __instance.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
            if (wraith != null && wraith.Severity >= 24f)
            {
                PsychicPowerUtility.WraithTransfer(__instance);
            }
        }
        //Transcendent Phoenixes automatically "cast" their resurrection ability on themselves when they die. Logically, this won't happen if the ability's on cooldown
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
        //Each woke pawn hit by the effects of a neuroquake has a 30% chance to transcend. Could theoretically be handled by an extra ability comp, but that has a performance cost per Neuroquake-knowing pawn; could be handled by a derivative comp which replaces it via xpath, but this could cause issues with mod compatibility
        public static void HautsTraitsTrans_Neuroquake_ApplyPostfix(CompAbilityEffect_Neuroquake __instance)
        {
            foreach (Pawn pawn in __instance.parent.pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!pawn.Dead && !pawn.Suspended && pawn.GetStatValue(StatDefOf.PsychicSensitivity, true, -1) > float.Epsilon && !pawn.Fogged() && pawn.Spawned && pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    if (Rand.Value <= 0.3f && !pawn.Position.InHorDistOf(__instance.parent.pawn.Position, __instance.parent.def.EffectRadius) && pawn.Position.InHorDistOf(__instance.parent.pawn.Position, __instance.Props.mentalStateRadius))
                    {
                        TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_TransNeuroquake".Translate(), "HVT_TransNeuroquakeFantasy".Translate(), 0.01f);
                    }
                }
            }
        }
        /*Transcendent Harpies fill their hunger meter on eating a psycaster. If the psycaster was higher-level, also gain a psylink. Gain one random transcendence of the ingested, and any awakened traits and archite genes of the ingested.
         * On eating a corpse, Trans Harbingers (named after the Anomaly tree) gain xp in a skill available to it and the corpse (if any), make progress to learning its RimLanguage language (if any), and have a 100% chance to remove a random
         *   bad health condition per 2 body size of the corpse. The first two effects scale with both parties' psy sensitivity; the heal is static.*/
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
                            if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                            {
                                couldGrant.Add(t.def);
                            } else if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def)) {
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
                        ModCompatibilityUtility.LearnLanguage(ingester,pawn, 0.4f * (pawn.GetStatValue(StatDefOf.PsychicSensitivity) + ingester.GetStatValue(StatDefOf.PsychicSensitivity)));
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
        //Transcendent Harpies also happen to destroy whatever they eat. Harbingers merely deal 25 points of damage to the corpse, and since corpses have 100 hp this means they're limited to four meals or less per corpse.
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
        //Transcendent Nightingales get a refund on Words' psyfocus costs
        public static void HautsTraitsTrans_TotalPsyfocusRefundPostfix(ref float __result, Pawn pawn, float psyfocusCost, bool isWord, bool isSkip)
        {
            if (isWord && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitNightingale))
            {
                __result = Math.Max(__result,Math.Max(0f,psyfocusCost - 0.1f));
            }
        }
        /*Various transcendents cause extra effects on psycasting that depend on the nature and/or position of the targets.
         * On using a skip psycast, Glowworms stuff the __state with all Things in or adjacent to a line between the target and destination (or if they're the same position, everything in psylink level-scaling radius of the target, capped at 6).
         *   Everything hostile or factionless in the __state gets hit with skipfrag damage.
         * Bellbirds' Word psycasts affect all valid targets in psylink level-scaling range of the caster.
         * Canaries inflict additional effects on the victims of their Word psycasts. This can combo with Bellbird.
         * Bouldermits have a 10% chance to create meteorites on casting any level 6+ psycast. Any psycast they use creates a chunk at the target location, and has a psyfocus cost-scaling chance to produce a stack of random metals.
         * Diaboli coat the target point in chemfuel.
         * Fireflies make their unique solar pinholes with blinding auras, or if one already exists there, refreshes its duration and converts it to their faction (thereby only blinding those their faction considers hostile).
         * Oilbirds relocate their auras to the target point.
         * Orcas create a Waterskip-like effect at the target point.
         * Termites stagger foes and deal crush damage to hostile buildings in psycast level-scaling radius around the target point.*/
        public static void HautsTraitsTrans_Psycast_ActivatePrefix(Psycast __instance, LocalTargetInfo target, LocalTargetInfo dest, out List<Thing> __state)
        {
            Pawn pawn = __instance.pawn;
            __state = new List<Thing>();
            if (target != null && pawn.story != null && target.Cell.IsValid && target.Cell.InBounds(pawn.Map))
            {
                if (__instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("Skip") && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitGlowworm))
                {
                    List<IntVec3> iv3s = new List<IntVec3>();
                    if (dest != null && dest.Cell.IsValid && dest.Cell.InBounds(pawn.Map))
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
            PsychicPowerUtility.PsycastActivationRiderEffects(__instance);
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
                        FleckMaker.Static(target.Cell, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 1.5f * pawn.GetPsylinkLevel());
                        bool canary = pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary);
                        foreach (Pawn p in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 1.5f * pawn.GetPsylinkLevel(), true).OfType<Pawn>().Distinct<Pawn>())
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
                                PsychicPowerUtility.DoCanaryEffects(pawn, p);
                            }
                        }
                    }
                    if (!didCanary && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary))
                    {
                        if (target.Thing != null && target.Thing is Pawn p)
                        {
                            PsychicPowerUtility.DoCanaryEffects(pawn,p);
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
                if (target.Cell.IsValid && target.Cell.InBounds(pawn.Map))
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
                        bool alreadyExists = false;
                        List<Thing> thingList = target.Cell.GetThingList(pawn.Map);
                        if (!thingList.NullOrEmpty())
                        {
                            foreach (Thing t in thingList)
                            {
                                if (t.def == HVTRoyaltyDefOf.HVT_FireflyLight)
                                {
                                    alreadyExists = true;
                                    CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                                    if (cdad != null)
                                    {
                                        cdad.spawnTick = Find.TickManager.TicksGame;
                                    }
                                    CompAuraEmitterHediff caeh = t.TryGetComp<CompAuraEmitterHediff>();
                                    if (caeh != null)
                                    {
                                        caeh.faction = pawn.Faction;
                                    }
                                    break;
                                }
                            }
                        }
                        if (!alreadyExists)
                        {
                            ThingWithComps firefly = (ThingWithComps)ThingMaker.MakeThing(HVTRoyaltyDefOf.HVT_FireflyLight);
                            firefly.GetComp<CompAuraEmitter>().creator = pawn;
                            GenSpawn.Spawn(firefly, target.Cell, pawn.Map, WipeMode.Vanish);
                            CompAbilityEffect_Teleport.SendSkipUsedSignal(target, pawn);
                            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(target.Cell, pawn.Map, false));
                        }
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
        //Any psycast (even GlobalTargetInfo ones like Farskip) invoke transcendent effects that don't have anything to do with the target
        public static void HautsTraitsTrans_Psycast_ActivatePostfix2(Psycast __instance)
        {
            PsychicPowerUtility.PsycastActivationRiderEffects(__instance);
        }
        //Anomaly effects that turn a pawn into fleshbeasts don't work on trans pawns
        public static bool HautsTraitsTrans_SpawnFleshbeastFromPawnPrefix(Pawn pawn)
        {
            if (PsychicTraitAndGeneCheckUtility.IsTranscendent(pawn))
            {
                Messages.Message("HVT_ImmuneToBiomutation".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
        /*"Bad events" (defined in Framework), and those that are clearly bad due to their letter def, have a 50% chance to be blocked if there is a Deimatic colonist anywhere in the world.
         * To prevent this from borking quests, any incident generated by a quest is unaffected.*/
        public static bool HautsTraitsTrans_TryExecutePrefix(IncidentWorker __instance, IncidentParms parms)
        {
            if (parms.quest == null && GoodAndBadIncidentsUtility.badEventPool.Contains(__instance.def) && !GoodAndBadIncidentsUtility.goodEventPool.Contains(__instance.def))
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
        //Transcendent Evergreens can't randomly get cancer (works independent of Biotech's cancer rate factor stat), nor do they ever get old age conditions
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
        //Transcendent Xerigiums remove a random injury or immunizable illness on tending a pawn. Also, surgeries on Fossilized Psychics always get the highest quality outcome.
        public static void HautsTraitsTrans_GetQualityPostfix(ref float __result, Pawn surgeon, Pawn patient)
        {
            if (surgeon.story != null && surgeon.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitXerigium))
            {
                __result = 1f;
                PsychicPowerUtility.XerigiumHeal(surgeon, patient);
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
                PsychicPowerUtility.XerigiumHeal(doctor, patient);
            }
        }
        //Transcendent Cassowaries gain a psysens-scaling damage multiplier when wielding persona weapons
        public static void HautsTraitsTrans_AdjustedMeleeDamageAmountPostfix(ref float __result, Tool tool, Pawn attacker, Thing equipment)
        {
            if (equipment != null && equipment.def.comps != null && attacker != null && attacker.story != null && attacker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCassowary))
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
        //Transcendent Magpies generate a bunch of items from a small random list of vanilla ThingSetMakers on completing a quest. They must be spawned for this to trigger
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
        //Transcendent Rooks get more stuff from harvesting plants/mining blocks/fishing, as well as reduce the time it takes for an animal they've gathered resources from to become gatherable again. These all scale with psysens
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
            if (pawn != null && pawn.Spawned && __instance.def.building != null && __instance.def.building.mineableThing != null && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook))
            {
                Thing thing = ThingMaker.MakeThing(__instance.def.building.mineableThing, null);
                thing.stackCount = (int)Math.Ceiling(Math.Max(1f, __instance.def.building.EffectiveMineableYield * Math.Min(1f, pawn.GetStatValue(StatDefOf.PsychicSensitivity) - 1f) / 3f));
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default);

            }
        }
        public static void HautsTraitsTrans_GetCatchesForPostfix(ref List<Thing> __result, Pawn pawn)
        {
            if (pawn != null && pawn.story != null && pawn.Spawned && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitRook))
            {
                foreach (Thing thing in __result)
                {
                    if (thing.def.thingCategories.Contains(ThingCategoryDefOf.Fish))
                    {
                        Thing thing2 = ThingMaker.MakeThing(thing.def,null);
                        thing2.stackCount = GenMath.RoundRandom((float)(thing.stackCount * (pawn.GetStatValue(StatDefOf.PsychicSensitivity) / 3f)));
                        if (pawn.Faction != Faction.OfPlayerSilentFail)
                        {
                            thing2.SetForbidden(true, true);
                        }
                        GenPlace.TryPlaceThing(thing2, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                    }
                }
            }
        }
        //destroying the mechhive in Odyssey's endgame quest transcends all woke pawns on the map
        public static void HautsTraitsTrans_DeactivateCorePrefix(CompCerebrexCore __instance, bool scavenging)
        {
            if (!scavenging)
            {
                if (__instance.parent.Map != null)
                {
                    foreach (Pawn p in __instance.parent.Map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p))
                        {
                            TranscendenceMethodsUtility.AchieveTranscendence(p, "HVT_TransCerebrexScream".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVT_TransCerebrexScreamFantasy".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), 0.1f);
                        }
                    }
                }
            }
        }
        /*Weaverbirds have a 50% chance to duplicate any item they create (obviously, any comps not covered here, e.g. those from other mods, are not likely to be perfectly replicated)
         * Rooks gain more products from butchery, psysens-scaling.*/
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
        /*Pelicans have 1.5x Verb_CastAbility and VEF Ability range*/
        public static void HautsTraitsTrans_AdjustedRangePostfix(ref float __result, Verb ownerVerb, Pawn attacker)
        {
            if (attacker != null && ownerVerb is Verb_CastAbility && __result > 0 && attacker.story != null && attacker.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPelican))
            {
                __result *= 1.5f;
            }
        }
        public static void HautsTraitsTrans_GetRangeForPawnPostfix(ref float __result, VEF.Abilities.Ability __instance)
        {
            if (__instance.pawn != null && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitPelican) && __result > 0f)
            {
                __result *= 1.5f;
            }
        }
        /*On being either party of a social interaction, Transcendent Hill Mynahs have a chance to gain a psycast that the other party knows*/
        public static void HautsTraitsTrans_TryInteractWithPostfix(Pawn_InteractionsTracker __instance, Pawn recipient)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (recipient.abilities != null && pawn.abilities != null)
            {
                PsychicPowerUtility.MynahAbilityCopy(pawn, recipient);
                PsychicPowerUtility.MynahAbilityCopy(recipient, pawn);
            }
        }
        //Handles the BloodRainImmune DME
        public static void HautsTraitsTrans_BloodRage_PostAddPostfix(Hediff_BloodRage __instance)
        {
            if (__instance.pawn.story != null)
            {
                foreach (Trait t in __instance.pawn.story.traits.TraitsSorted)
                {
                    if (t.def.HasModExtension<BloodRainImmune>())
                    {
                        __instance.pawn.health.RemoveHediff(__instance);
                        return;
                    }
                }
            }
        }
        //Transcendent Dragons cannot be directly affected by the psycasts of hostile pawns
        public static void HautsTraitsTrans_CanApplyPsycastToPostfix(ref bool __result, LocalTargetInfo target, Psycast __instance)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon) && p.HostileTo(__instance.pawn))
            {
                __result = false;
            }
        }
        //handles the LivingGravAnchor DME
        public static bool HautsTraitsTrans_GravshipUtility_AbandonMapPrefix(Map map)
        {
            List<Pawn> list = map.mapPawns.AllPawnsSpawned.ToList<Pawn>();
            foreach (Pawn p in list)
            {
                if (p.story != null)
                {
                    foreach (Trait t in p.story.traits.allTraits)
                    {
                        if (t.def.HasModExtension<LivingGravAnchor>())
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        //Using Cybernetic Organism and Neural Network's brainwash on a Latent Psychic awakens them
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
                AwakeningMethodsUtility.AwakenPsychicTalent(user, true, "HVT_WokeBrainwash".Translate().Formatted(user.Named("PAWN")).AdjustedFor(user, "PAWN", true).Resolve(), "HVT_WokeBrainwashFantasy".Translate().Formatted(user.Named("PAWN")).AdjustedFor(user, "PAWN", true).Resolve());
            }
        }
        //makes the Framework method ModCompatibilityUtility.IsAwakenedPsychic work
        public static void HautsTraitsIsAwakenedPsychicPostfix(Pawn pawn, ref bool __result)
        {
            if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
            {
                __result = true;
            }
        }
    }
}
