using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using RimWorld.Planet;
using HautsFramework;

namespace HautsTraits
{
    [StaticConstructorOnStartup]
    public static class HautsTraits
    {
        private static readonly Type patchType = typeof(HautsTraits);
        static HautsTraits()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautstraits.main");
            harmony.Patch(AccessTools.Method(typeof(MentalState), nameof(MentalState.RecoverFromState)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTRecoverFromStatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTTryStartMentalStatePostfix)));
            harmony.Patch(AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), nameof(PawnDiedOrDownedThoughtsUtility.GetThoughts)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTGetThoughtsPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ExpectationsUtility), nameof(ExpectationsUtility.CurrentExpectationFor), new[] { typeof(Pawn)}),
                          postfix: new HarmonyMethod(patchType, nameof(HVTCurrentExpectationsForPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtUtility), nameof(ThoughtUtility.CanGetThought)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTCanGetThoughtPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTTryInteractWithPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThoughtHandler), nameof(ThoughtHandler.OpinionOffsetOfGroup)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTOpinionOffsetOfGroupPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MentalBreakWorker_RunWild), nameof(MentalBreakWorker_RunWild.TryStart)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTAllegiantBreakCanOccurPrefix)));
            harmony.Patch(AccessTools.Method(typeof(RitualOutcomeEffectWorker), nameof(RitualOutcomeEffectWorker.MakeMemory)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTAsocialMakeMemoryPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.SocialFightChance)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTAsocialSocialFightChancePostfix)));
            harmony.Patch(AccessTools.Method(typeof(Book), nameof(Book.OnBookReadTick)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTBookwormOnBookReadTickPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTConversationalistTryInteractWithPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.OpinionOf)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTForgettableOpinionOfPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTConversationalistTryStartMentalStatePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Corpse), nameof(Corpse.GiveObservedHistoryEvent)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTGraverGiveObservedHistoryEventPrefix)));
            harmony.Patch(AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.Interacted)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTLovesickRomanceAttemptInteractedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.Interacted)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTLovesickMarriageProposalInteractedPostfix)));
            MethodInfo methodInfo = typeof(InteractionWorker_RomanceAttempt).GetMethod("TryAddCheaterThought", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo,
                          postfix: new HarmonyMethod(patchType, nameof(HVTLovesickTryAddCheaterThought)));
            harmony.Patch(AccessTools.Method(typeof(InteractionWorker_Breakup), nameof(InteractionWorker_Breakup.Interacted)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTLovesickBreakupInteractedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(SpouseRelationUtility), nameof(SpouseRelationUtility.RemoveGotMarriedThoughts)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTLovesickBreakupInteractedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTMFThoughtsFromIngestingPrefix)));
            harmony.Patch(AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.AddFoodPoisoningHediff)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTMFAddFoodPoisoningHediffPrefix)));
            harmony.Patch(AccessTools.Method(typeof(InspirationHandler), nameof(InspirationHandler.TryStartInspiration)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTRevellerTryStartInspirationPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ThingSelectionUtility), nameof(ThingSelectionUtility.SelectableByMapClick)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTSkulkerHostilesNotClickablePostfix)));
            harmony.Patch(AccessTools.Method(typeof(InteractionWorker_ConvertIdeoAttempt), nameof(InteractionWorker_ConvertIdeoAttempt.Interacted)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTIntolerantConvertIdeoAttemptInteractedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(InteractionWorker_ConvertIdeoAttempt), nameof(InteractionWorker_ConvertIdeoAttempt.CertaintyReduction)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTGenePuristCertaintyReductionPostfix)));
            if (ModsConfig.AnomalyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(MetalhorrorUtility), nameof(MetalhorrorUtility.Infect)),
                               postfix: new HarmonyMethod(patchType, nameof(HVTInfectPostfix)));
                harmony.Patch(AccessTools.Method(typeof(InteractionWorker_InhumanRambling), nameof(InteractionWorker_InhumanRambling.RandomSelectionWeight)),
                               postfix: new HarmonyMethod(patchType, nameof(HVTInhumanRambling_RandomSelectionWeightPostfix)));
            }
            if (ModsConfig.OdysseyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(FishingUtility), nameof(FishingUtility.GetNegativeFishingOutcomes)),
                               postfix: new HarmonyMethod(patchType, nameof(HVTAnglerGetNegativeFishingOutcomesPostfix)));
                harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                              postfix: new HarmonyMethod(patchType, nameof(HVTPescatarianIngestedPostfix)));
                harmony.Patch(AccessTools.Method(typeof(FishingUtility), nameof(FishingUtility.GetCatchesFor)),
                              postfix: new HarmonyMethod(patchType, nameof(HVTScavengerGetCatchesForPostfix)));
                MethodInfo methodInfoO1 = typeof(WorldComponent_GravshipController).GetMethod("LandingEnded", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfoO1,
                              prefix: new HarmonyMethod(patchType, nameof(HVTLandingEndedPrefix)));
                MethodInfo methodInfoO2 = typeof(TravellingTransporters).GetMethod("DoArrivalAction", BindingFlags.NonPublic | BindingFlags.Instance);
                harmony.Patch(methodInfoO2,
                              prefix: new HarmonyMethod(patchType, nameof(HVTDoArrivalActionPrefix)));
            }
            harmony.Patch(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new[] { typeof(Caravan), typeof(Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) }),
                          prefix: new HarmonyMethod(patchType, nameof(HautsTraitsEnterPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Building_Trap), nameof(Building_Trap.Spring)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTSkulkSpringPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GenExplosion), nameof(GenExplosion.NotifyNearbyPawnsOfDangerousExplosive)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTNotifyNearbyPawnsOfDangerousExplosivePrefix)));
            harmony.Patch(AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), nameof(CaravanArrivalAction_VisitSite.GetFloatMenuOptions)),
                          postfix: new HarmonyMethod(patchType, nameof(HVT_GetFloatMenuOptionsPostfix)));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest)}),
                           postfix: new HarmonyMethod(patchType, nameof(HVTGeneratePawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GrowthUtility), nameof(GrowthUtility.IsGrowthBirthday)),
                           postfix: new HarmonyMethod(patchType, nameof(HVTIsGrowthBirthdayPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.GrowthPointsPerDay)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HVTGrowthPointsPerDayPostfix)));
            MethodInfo methodInfo2 = typeof(Pawn_AgeTracker).GetMethod("BirthdayBiological", BindingFlags.NonPublic | BindingFlags.Instance);
            harmony.Patch(methodInfo2,
                          postfix: new HarmonyMethod(patchType, nameof(HVTBirthdayBiologicalPostfix)));
            harmony.Patch(AccessTools.Method(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.MakeChoices)),
                           prefix: new HarmonyMethod(patchType, nameof(HVTMakeChoicesPrefix)));
            harmony.Patch(AccessTools.Method(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.MakeChoices)),
                           postfix: new HarmonyMethod(patchType, nameof(HVTMakeChoicesPostfix)));
            //tranquil should conflict with all violent-requiring traits, and its dummy should conflict with all traits it's been written to conflict with in XML.
            if (HVTDefOf.HVT_Tranquil.conflictingTraits == null)
            {
                HVTDefOf.HVT_Tranquil.conflictingTraits = new List<TraitDef>();
            }
            List<TraitDef> tranquilConflicts = HVTDefOf.HVT_Tranquil.conflictingTraits;
            foreach (TraitDef td in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (!tranquilConflicts.Contains(td) && td.requiredWorkTags == WorkTags.Violent)
                {
                    tranquilConflicts.Add(td);
                }
                if (td.HasModExtension<SuperPsychicTrait>())
                {
                    TraitModExtensionUtility.AddExciseTraitExemption(td);
                }
            }
            HVTDefOf.HVT_Tranquil.conflictingTraits = tranquilConflicts;
            HVTDefOf.HVT_Tranquil0.conflictingTraits = tranquilConflicts;
            if (HVTDefOf.HVT_Tranquil0.conflictingTraits != null && !HVTDefOf.HVT_Tranquil0.conflictingTraits.Contains(HVTDefOf.HVT_Tranquil))
            {
                HVTDefOf.HVT_Tranquil0.conflictingTraits.Add(HVTDefOf.HVT_Tranquil);
            }
            Log.Message("HVT_Initialize".Translate().CapitalizeFirst());
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        internal static object GetTypeField(Type type, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(type);
        }
        //Daydreamers gain an inspiration after finishing most kinds of mental states (non-social fighting, non-damage/psycast induced). Must be of player faction since only player pawns get inspired
        public static void HVTRecoverFromStatePostfix(MentalState __instance)
        {
            if (__instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Daydreamer) && __instance.def != MentalStateDefOf.SocialFighting && __instance.pawn.Faction == Faction.OfPlayerSilentFail && (__instance.causedByMood || (!__instance.causedByDamage && !__instance.causedByPsycast)))
            {
                List<Trait> allTraits = __instance.pawn.story.traits.allTraits;
                List<InspirationDef> mentalBreakInspirationGainSet = new List<InspirationDef>();
                for (int i = 0; i < allTraits.Count; i++)
                {
                    if (allTraits[i].CurrentData.mentalBreakInspirationGainSet != null)
                    {
                        for (int j = 0; j < allTraits[i].CurrentData.mentalBreakInspirationGainSet.Count; j++)
                        {
                            mentalBreakInspirationGainSet.Add(allTraits[i].CurrentData.mentalBreakInspirationGainSet[j]);
                        }
                    }
                }
                TaggedString reasonText = "HVT_DaydreamerInspired".Translate().CapitalizeFirst().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve();
                if (mentalBreakInspirationGainSet.Count != 0)
                {
                    __instance.pawn.mindState.inspirationHandler.TryStartInspiration(mentalBreakInspirationGainSet.RandomElement<InspirationDef>(), reasonText, true);
                } else {
                    InspirationDef randomAvailableInspirationDef = __instance.pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                    if (randomAvailableInspirationDef != null)
                    {
                        __instance.pawn.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, reasonText, true);
                    }
                }
            }
        }
        /*Repressed Rage sends a letter when a pawn the game ShouldSendNotificationAbout goes berserk because of it.
         * Tranquil pawns instantly end any mental state they enter.
         * Most kinds of mental states (social fight, mood-induced, damage-induced, psycast-induced) induce a mood boost and 10% inspiration chance in Sadistics on the same map/caravan, but also give the mental stater negative opinion of the Sadistic*/
        public static void HVTTryStartMentalStatePostfix(ref bool __result, MentalStateHandler __instance, MentalStateDef stateDef, bool causedByMood, bool causedByDamage, bool causedByPsycast)
        {
            Pawn pawn = GetInstanceField(typeof(MentalStateHandler), __instance, "pawn") as Pawn;
            if (__result)
            {
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    pawn.health.hediffSet.TryGetHediff(HVTDefOf.HVT_RRUnleashed, out Hediff rage);
                    if (rage != null)
                    {
                        HediffComp_SeverityDuringSpecificMentalStates hcsdsms = rage.TryGetComp<HediffComp_SeverityDuringSpecificMentalStates>();
                        if (hcsdsms != null && ((hcsdsms.Props.mentalStates != null && hcsdsms.Props.mentalStates.Contains(stateDef)) || hcsdsms.Props.anyMentalState))
                        {
                            Messages.Message("HVT_RepressedRaging".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
                if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Tranquil))
                {
                    if (pawn.MentalState != null)
                    {
                        pawn.MentalState.RecoverFromState();
                    }
                } else if (stateDef == MentalStateDefOf.SocialFighting || causedByMood || causedByDamage || causedByPsycast) {
                    if (pawn.Map != null)
                    {
                        List<Pawn> sadists = (List<Pawn>)pawn.Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < sadists.Count; i++)
                        {
                            Pawn recipient = sadists[i];
                            if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Sadist) && pawn != recipient)
                            {
                                HVTUtility.DoSadistMoodStuff(recipient, pawn);
                            }
                        }
                    } else if (pawn.IsCaravanMember()) {
                        Caravan caravan = pawn.GetCaravan();
                        for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
                        {
                            Pawn recipient = caravan.PawnsListForReading[i];
                            if (recipient.RaceProps.Humanlike && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Sadist) && pawn != recipient)
                            {
                                HVTUtility.DoSadistMoodStuff(recipient, pawn);
                            }
                        }
                    }
                }
            }
        }
        /*when a pawn dies...
         * and it's a mechanoid, witnessing mechaphiles of the same faction get sad. If a mechaphobe was the one to kill it, they get happy
         * and it's an unnatural entity or mutant that was killed by a Monster Hunter, the MH gets happy.*/
        public static void HVTGetThoughtsPostfix(Pawn victim, DamageInfo? dinfo, PawnDiedOrDownedThoughtsKind thoughtsKind, List<IndividualThoughtToAdd> outIndividualThoughts, List<ThoughtToAddToAll> outAllColonistsThoughts)
        {
            if (thoughtsKind == PawnDiedOrDownedThoughtsKind.Died && !PawnGenerator.IsBeingGenerated(victim))
            {
                if (ModsConfig.BiotechActive && victim.RaceProps.IsMechanoid)
                {
                    if (victim.Map != null && victim.Faction != null)
                    {
                        List<Pawn> mechaphiles = victim.Map.mapPawns.SpawnedPawnsInFaction(victim.Faction);
                        for (int i = 0; i < mechaphiles.Count; i++)
                        {
                            Pawn recipient = mechaphiles[i];
                            if (!recipient.IsMutant && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Mechaphile) && ThoughtUtility.Witnessed(recipient, victim))
                            {
                                for (int j = 0; j < victim.GetStatValue(StatDefOf.BandwidthCost); j++)
                                {
                                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_MechaphileWitnessedMechDeath, null);
                                }
                            }
                        }
                    } else if (victim.IsCaravanMember()) {
                        Caravan caravan = victim.GetCaravan();
                        for (int i = 0; i < caravan.PawnsListForReading.Count; i++)
                        {
                            Pawn recipient = caravan.PawnsListForReading[i];
                            if (recipient.Faction != null && victim.Faction == recipient.Faction && !recipient.IsMutant && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Mechaphile) && ThoughtUtility.Witnessed(recipient, victim))
                            {
                                for (int j = 0; j < victim.GetStatValue(StatDefOf.BandwidthCost); j++)
                                {
                                    recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_MechaphileWitnessedMechDeath, null);
                                }
                            }
                        }
                    }
                }
                if (dinfo != null && dinfo.Value.Def.ExternalViolenceFor(victim) && dinfo.Value.Instigator != null && dinfo.Value.Instigator is Pawn p && !p.Dead && p.needs.mood != null && p.story != null && !p.IsSubhuman)
                {
                    if (ModsConfig.BiotechActive && victim.RaceProps.IsMechanoid)
                    {
                        for (int i = 0; i < victim.GetStatValue(StatDefOf.BandwidthCost); i++)
                        {
                            outIndividualThoughts.Add(new IndividualThoughtToAdd(HVTDefOf.HVT_MechaphobeKilledMech, p, null, 1f, 1f));
                        }
                    } else if (ModsConfig.AnomalyActive && (victim.RaceProps.IsAnomalyEntity || victim.IsMutant)) {
                        for (int i = 0; i < Math.Floor(victim.BodySize); i++)
                        {
                            outIndividualThoughts.Add(new IndividualThoughtToAdd(HVTDefOf.HVT_MonsterHunterWorld, p, null, 1f, 1f));
                        }
                    }
                }
            }
        }
        //Prideful/Entitled pawns have minimum High expectations. Humble pawns have max Low expectations, or max Moderate if they have a royal title or ideo role.
        public static void HVTCurrentExpectationsForPostfix(ref ExpectationDef __result, Pawn p)
        {
            if (p.story != null)
            {
                if (p.MapHeld != null && p.story.traits.HasTrait(HVTDefOf.HVT_Prideful))
                {
                    if (__result.order < 4)
                    {
                        __result = ExpectationDefOf.High;
                    }
                } else if (p.story.traits.HasTrait(HVTDefOf.HVT_Humble)) {
                    if ((p.royalty != null && p.royalty.AllTitlesForReading.Count > 0) || (ModsConfig.IdeologyActive && p.ideo != null && p.Ideo.GetRole(p) != null) && __result.order > 3)
                    {
                        __result = ExpectationDefOf.Moderate;
                    } else if (__result.order > 2) {
                        __result = ExpectationDefOf.Low;
                    }
                }
            }
        }
        //Because the "expectation traits" have different expectation levels, their personal expectation levels should supersede colony expectation level when determining if they can have thoughts requiring a minExpectation
        public static void HVTCanGetThoughtPostfix(ref bool __result, Pawn pawn, ThoughtDef def)
        {
            if (__result == true && def.minExpectation != null && pawn.story != null && (pawn.story.traits.HasTrait(HVTDefOf.HVT_Humble) || pawn.story.traits.HasTrait(HVTDefOf.HVT_Prideful)))
            {
                ExpectationDef expectationDef = ExpectationsUtility.CurrentExpectationFor(pawn);
                if (expectationDef != null && expectationDef.order < def.minExpectation.order)
                {
                    __result = false;
                }
            }
        }
        /*Conversationalists grant a stacking mood buff to anyone they interact with, provided that interaction isn't a slight or insult.
         * Curmudgeons lose mood and opinion when interacting with someone who is Content or Happy, mood-wise.
         * This also handles Mentors granting skills to whoever they interact with.
         *   It has to be a skill they both have access to, and is preferably a shared passion, one of the mentee's passions, or one of the Mentor's passions, in that order. Scales with Mentor's instructive ability*/
        public static void HVTTryInteractWithPostfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (pawn.story != null)
            {
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Conversationalist) && recipient.needs.mood != null)
                {
                    if (intDef != InteractionDefOf.Insult && intDef != DefDatabase<InteractionDef>.GetNamed("Slight"))
                    {
                        recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_StimulatingConversation, null);
                    }
                }
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Curmudgeon) && (recipient.story == null || !recipient.story.traits.HasTrait(HVTDefOf.HVT_Curmudgeon)))
                {
                    HVTUtility.DoCurmudgeonPenalties(pawn, recipient);
                } else if (recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Curmudgeon) && !pawn.story.traits.HasTrait(HVTDefOf.HVT_Curmudgeon)) {
                    HVTUtility.DoCurmudgeonPenalties(recipient, pawn);
                }
                if (ModsConfig.BiotechActive && pawn.story.traits.HasTrait(HVTDefOf.HVT_Mentor) && recipient.skills != null)
                {
                    SkillDef skillToTeach = null;
                    List<SkillDef> tutorableSkills = new List<SkillDef>();
                    foreach (SkillDef s in DefDatabase<SkillDef>.AllDefsListForReading)
                    {
                        if (!pawn.skills.GetSkill(s).TotallyDisabled && !recipient.skills.GetSkill(s).TotallyDisabled)
                        {
                            tutorableSkills.Add(s);
                        }
                    }
                    if (tutorableSkills.Count > 0)
                    {
                        List<SkillDef> mentorInterests = new List<SkillDef>();
                        List<SkillDef> menteeInterests = new List<SkillDef>();
                        List<SkillDef> anyMutualInterests = new List<SkillDef>();
                        foreach (SkillDef s in tutorableSkills)
                        {
                            if (pawn.skills.GetSkill(s).passion > Passion.None)
                            {
                                mentorInterests.Add(s);
                                if (recipient.skills.GetSkill(s).passion > Passion.None)
                                {
                                    anyMutualInterests.Add(s);
                                }
                            }
                            if (recipient.skills.GetSkill(s).passion > Passion.None)
                            {
                                menteeInterests.Add(s);
                            }
                        }
                        if (anyMutualInterests.Count > 0)
                        {
                            skillToTeach = anyMutualInterests.RandomElement<SkillDef>();
                        } else if (menteeInterests.Count > 0) {
                            skillToTeach = menteeInterests.RandomElement<SkillDef>();
                        } else if (mentorInterests.Count > 0) {
                            skillToTeach = mentorInterests.RandomElement<SkillDef>();
                        }
                        if (skillToTeach != null)
                        {
                            float xpBase = pawn.GetStatValue(HautsDefOf.Hauts_InstructiveAbility) * pawn.skills.GetSkill(skillToTeach).LearnRateFactor(true) * recipient.skills.GetSkill(skillToTeach).LearnRateFactor(false);
                            recipient.skills.Learn(skillToTeach, (67f + (Rand.Value * 267f)) * xpBase);
                        }
                    }
                }
            }
        }
        //Judgementals have double magnitude of opinion maluses. Tolerants have halved ideoligiously-caused opinion maluses of pawns from different ideos
        public static void HVTOpinionOffsetOfGroupPostfix(ref int __result, ThoughtHandler __instance, ISocialThought group, Pawn otherPawn)
        {
            if (__instance.pawn.story != null)
            {
                if (__result < 0f)
                {
                    if (ModsConfig.IdeologyActive)
                    {
                        Thought thought = (Thought)group;
                        if (thought.sourcePrecept != null && __instance.pawn.ideo != null && otherPawn.ideo != null && __instance.pawn.Ideo != otherPawn.Ideo && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Tolerant))
                        {
                            __result /= 2;
                        }
                    }
                    if (__instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Judgemental))
                    {
                        __result *= 2;
                    }
                }
            }
        }
        //allegiants cannot run wild. This has to be handled via Harmony since traits can only restrict mental states, but Run Wild is a mental break WITHOUT a mental state
        public static bool HVTAllegiantBreakCanOccurPrefix(Pawn pawn, bool causedByMood)
        {
            if (causedByMood && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Allegiant))
            {
                return false;
            }
            return true;
        }
        //Asocials do not get mood from attending rituals. They do not get provoked into social fights by slights or insults, as an extension of their inability to be affected by those interactions
        public static void HVTAsocialMakeMemoryPostfix(ref Thought_Memory __result, Pawn p)
        {
            if (p.story.traits.HasTrait(HVTDefOf.HVT_Asocial))
            {
                __result.moodPowerFactor = 0f;
            }
        }
        public static void HVTAsocialSocialFightChancePostfix(ref float __result, Pawn_InteractionsTracker __instance, InteractionDef interaction)
        {
            if (interaction == DefDatabase<InteractionDef>.GetNamed("Slight") || interaction == InteractionDefOf.Insult)
            {
                Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
                if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Asocial))
                {
                    __result = 0f;
                }
            }
        }
        //bookworms gain a small but stacking and enduring mood buff for book reading
        public static void HVTBookwormOnBookReadTickPostfix(Pawn pawn, int delta)
        {
            if (pawn.IsHashIntervalTick(750, delta) && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Bookworm) && pawn.needs.mood != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_Bibliophilia);
            }
        }
        //non-kind conversationalists going on a non-social fight, non-humanity break mental break have a 25% chance to go on an insulting spree, if possible
        public static void HVTConversationalistTryStartMentalStatePrefix(MentalStateHandler __instance, bool causedByMood, bool causedByDamage, ref MentalStateDef stateDef)
        {
            Pawn pawn = GetInstanceField(typeof(MentalStateHandler), __instance, "pawn") as Pawn;
            if (stateDef != MentalStateDefOf.SocialFighting && (!ModsConfig.AnomalyActive || stateDef != MentalStateDefOf.HumanityBreak) && (causedByDamage || causedByMood) && pawn.story != null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) && pawn.story.traits.HasTrait(HVTDefOf.HVT_Conversationalist) && !pawn.story.traits.HasTrait(TraitDefOf.Kind))
            {
                List<Pawn> candidates = new List<Pawn>();
                InsultingSpreeMentalStateUtility.GetInsultCandidatesFor(pawn, candidates, false);
                if (candidates.Any<Pawn>())
                {
                    float chance = Rand.Value;
                    if (chance <= 0.25f)
                    {
                        if (candidates.Count >= 2)
                        {
                            stateDef = DefDatabase<MentalStateDef>.GetNamed("InsultingSpree");
                        }
                    } else if (chance <= 0.5f) {
                        stateDef = DefDatabase<MentalStateDef>.GetNamed("TargetedInsultingSpree");
                    }
                }
            }
        }
        //Asocial pawns' chitchat attempts never fire (but count as if they did for the purpose of interaction cooldown), and non-Kind Conversationalists below major mb threshold can only slight or insult
        public static bool HVTConversationalistTryInteractWithPrefix(ref bool __result, Pawn_InteractionsTracker __instance, ref InteractionDef intDef)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_InteractionsTracker), __instance, "pawn") as Pawn;
            if (pawn.story != null)
            {
                if (intDef == InteractionDefOf.Chitchat && pawn.story.traits.HasTrait(HVTDefOf.HVT_Asocial))
                {
                    __result = true;
                    return false;
                } else if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Conversationalist) && pawn.mindState.mentalBreaker.CurMood < pawn.mindState.mentalBreaker.BreakThresholdMajor) {
                    if (!pawn.story.traits.HasTrait(TraitDefOf.Kind) && intDef != InteractionDefOf.Insult)
                    {
                        if (Rand.Value < 0.65f)
                        {
                            intDef = DefDatabase<InteractionDef>.GetNamed("Slight");
                        } else {
                            intDef = InteractionDefOf.Insult;
                        }
                    }
                }
            }
            return true;
        }
        //other pawns have 0.65x the opinion they normally would have of a Forgettable
        public static void HVTForgettableOpinionOfPostfix(Pawn other, ref int __result)
        {
            if (other.story != null && other.story.traits.HasTrait(HVTDefOf.HVT_Forgettable))
            {
                __result = (int)(__result*0.65f);
            }
        }
        //gravers love lookin at corpses
        public static void HVTGraverGiveObservedHistoryEventPrefix(Corpse __instance, Pawn observer)
        {
            if (__instance.InnerPawn.RaceProps.Humanlike && __instance.StoringThing() == null && observer.story != null && observer.story.traits.HasTrait(HVTDefOf.HVT_Graver))
            {
                Thought_MemoryObservation thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(HVTDefOf.HVT_ObservedLayingCorpseGraver);
                thought_MemoryObservation.Target = __instance;
                observer.needs.mood.thoughts.memories.TryGainMemory(thought_MemoryObservation);
            }
        }
        //lovesicks inflict a mood debuff on pawns who reject their advances/marriage proposals, as well as on those who cheat on/break up with them
        public static void HVTLovesickRomanceAttemptInteractedPostfix(Pawn initiator, Pawn recipient)
        {
            if (initiator.relations.GetDirectRelation(PawnRelationDefOf.Lover,recipient) == null && initiator.story != null && initiator.story.traits.HasTrait(HVTDefOf.HVT_Lovesick))
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_RebuffedALovesick, null, null);
            }
        }
        public static void HVTLovesickMarriageProposalInteractedPostfix(Pawn initiator, Pawn recipient)
        {
            if (initiator.relations.GetDirectRelation(PawnRelationDefOf.Fiance, recipient) == null && initiator.story != null && initiator.story.traits.HasTrait(HVTDefOf.HVT_Lovesick))
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_LovesickLetdown, null, null);
            }
        }
        public static void HVTLovesickTryAddCheaterThought(Pawn pawn, Pawn cheater)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Lovesick))
            {
                cheater.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_LovesickLetdown, null, null);
            }
        }
        public static void HVTLovesickBreakupInteractedPostfix(Pawn initiator, Pawn recipient)
        {
            if (recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Lovesick))
            {
                initiator.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_LovesickLetdown, null, null);
            }
        }
        //Metafreaks can't get thoughts (except for ate raw food or ate corpse) or food poisoning from food
        public static bool HVTMFThoughtsFromIngestingPrefix(ref List<FoodUtility.ThoughtFromIngesting> __result, Pawn ingester, ThingDef foodDef)
        {
            if (ingester.story != null && ingester.story.traits.HasTrait(HVTDefOf.HVT_MetabolicFreak) && foodDef.ingestible.tasteThought != ThoughtDefOf.AteRawFood && foodDef.ingestible.tasteThought != ThoughtDefOf.AteCorpse)
            {
                List<FoodUtility.ThoughtFromIngesting> ingestThoughts = new List<FoodUtility.ThoughtFromIngesting>();
                __result = ingestThoughts;
                return false;
            }
            return true;
        }
        public static bool HVTMFAddFoodPoisoningHediffPrefix(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_MetabolicFreak))
            {
                /* && ingestible.def.ingestible.tasteThought != ThoughtDefOf.AteRawFood && ingestible.def.ingestible.tasteThought != ThoughtDefOf.AteCorpse*/
                return false;
            }
            return true;
        }
        //Revellers call a party on being inspired
        public static void HVTRevellerTryStartInspirationPostfix(ref bool __result, InspirationHandler __instance)
        {
            if (__result && __instance.pawn.Map != null && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Reveller))
            {
                GatheringDefOf.Party.Worker.TryExecute(__instance.pawn.Map, __instance.pawn);
            }
        }
        //hostile, non-downed, non-captured skulkers cannot be selected, just like what Vanilla... Armor(?) Expanded's ghillie suits do
        public static void HVTSkulkerHostilesNotClickablePostfix(ref bool __result, Thing t)
        {
            Pawn pawn;
            if ((pawn = (t as Pawn)) != null && (pawn.Faction == null || pawn.HostileTo(Faction.OfPlayer)) && (pawn.story != null) && (!pawn.Downed) && (!pawn.IsPrisoner) && (!pawn.IsSlave))
            {
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    __result = false;
                }
            }
        }
        //intolerants always get a mood malus for being the recipient of a conversion attempt
        public static void HVTIntolerantConvertIdeoAttemptInteractedPostfix(Pawn initiator, Pawn recipient)
        {
            if (recipient.needs.mood != null && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Intolerant))
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FailedConvertIdeoAttemptResentment, initiator, null);
            }
        }
        //Xenotypists' certainty loss during a converison attempt is modified by whether or not they're being converted towards or away from an ideo that approves or disapproves of their xenotype
        public static void HVTGenePuristCertaintyReductionPostfix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (recipient.story != null && ModsConfig.BiotechActive && recipient.story.traits.HasTrait(HVTDefOf.HVT_GenePurist))
            {
                int initiatorDisagreeableGenes = 0;
                int recipientDisagreeableGenes = 0;
                if (initiator.Ideo.PreferredXenotypes != null || initiator.Ideo.PreferredCustomXenotypes != null)
                {
                    if (initiator.Ideo.IsPreferredXenotype(recipient))
                    {
                        initiatorDisagreeableGenes -= 2;
                    }
                    foreach (XenotypeDef x in initiator.Ideo.PreferredXenotypes)
                    {
                        initiatorDisagreeableGenes++;
                    }
                }
                if (recipient.Ideo.PreferredXenotypes != null || recipient.Ideo.PreferredCustomXenotypes != null)
                {
                    if (recipient.Ideo.IsPreferredXenotype(recipient))
                    {
                        recipientDisagreeableGenes -= 2;
                    }
                    foreach (XenotypeDef x in recipient.Ideo.PreferredXenotypes)
                    {
                        recipientDisagreeableGenes++;
                    }
                }
                if (initiatorDisagreeableGenes < 0)
                {
                    __result *= 2;
                } else if (initiatorDisagreeableGenes > 0) {
                    __result /= 1 + initiatorDisagreeableGenes;
                }
                if (recipientDisagreeableGenes < 0)
                {
                    __result /= 2;
                } else if (recipientDisagreeableGenes > 0) {
                    __result *= 1 + recipientDisagreeableGenes;
                }
            }
        }
        //Monster Hunters have a chance to avoid being infected by metalhorrors, scaling with sight, hearing, Medical skill, and Int skill. This causes the infecter's horror to emerge in response
        public static void HVTInfectPostfix(Pawn pawn, Pawn source)
        {
            if (source != null && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_MonsterHunter))
            {
                float chance = 0.05f*pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
                if (pawn.skills != null)
                {
                    chance *= Math.Max(pawn.skills.GetSkill(SkillDefOf.Medicine).Level, pawn.skills.GetSkill(SkillDefOf.Intellectual).Level);
                }
                if (Rand.Chance(chance))
                {
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    for (int i = hediffs.Count - 1; i >= 0; i--)
                    {
                        if (hediffs[i].def == HediffDefOf.MetalhorrorImplant)
                        {
                            pawn.health.RemoveHediff(hediffs[i]);
                            MetalhorrorUtility.TryEmerge(source, "HVT_HunterVsHorror".Translate(source.Named("INFECTED")), false);
                            continue;
                        }
                    }
                }
            }
        }
        //Regular humanity breaks force Inhuman Rambling as the predominant social interaction, and since we have a faux leather Humanity Break (whose only front-end diff is that it isn't ideoligious in nature) we need to patch it to do the same
        public static void HVTInhumanRambling_RandomSelectionWeightPostfix(Pawn initiator, ref float __result)
        {
            if (initiator.MentalStateDef != null && initiator.MentalStateDef == HVTDefOf.HVT_HumanityBreak)
            {
                __result = 999f;
            }
        }
        //anglers gain mood and psyfocus from completing any fishing attempt. This patch also handles their coin flip chance to negate a negative fishing outcome
        public static void HVTAnglerGetNegativeFishingOutcomesPostfix(ref List<NegativeFishingOutcomeDef> __result, Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Angler) && pawn.needs.mood != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_FishinsReelyFun);
                if (ModsConfig.RoyaltyActive && pawn.psychicEntropy != null)
                {
                    pawn.psychicEntropy.OffsetPsyfocusDirectly(0.03f);
                }
                if (Rand.Chance(0.5f) && !__result.NullOrEmpty())
                {
                    __result.Clear();
                }
            }
        }
        //governs pescatarians gaining mood from eating food that is or has a fish ingredient
        public static void HVTPescatarianIngestedPostfix(Thing __instance, Pawn ingester)
        {
            if (ingester.story != null && ingester.story.traits.HasTrait(HVTDefOf.HVT_Pescatarian) && ingester.needs.mood != null)
            {
                bool addThought = false;
                CompIngredients compIngredients = __instance.TryGetComp<CompIngredients>();
                if (compIngredients == null)
                {
                    if (HVTUtility.IsThisFoodFish(__instance.def))
                    {
                        addThought = true;
                    }
                } else if (!compIngredients.ingredients.NullOrEmpty<ThingDef>()) {
                    for (int i = 0; i < compIngredients.ingredients.Count; i++)
                    {
                        if (HVTUtility.IsThisFoodFish(compIngredients.ingredients[i]))
                        {
                            addThought = true;
                            break;
                        }
                    }
                }
                if (addThought)
                {
                    Thought_Memory thought_Memory = ThoughtMaker.MakeThought(HVTDefOf.HVT_TheSnackThatSmilesBack, null);
                    ingester.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, null);
                }
            }
        }
        //treasure seekers have a bonus chance for a rare catch, which still respects the lastRareCatchTick timer
        public static void HVTScavengerGetCatchesForPostfix(ref List<Thing> __result, Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Scavenger))
            {
                if (pawn.Map.Biome.fishTypes.rareCatchesSetMaker != null && (DebugSettings.alwaysRareCatches || pawn.Map.waterBodyTracker.lastRareCatchTick == 0 || GenTicks.TicksGame - pawn.Map.waterBodyTracker.lastRareCatchTick > 300000) && Rand.Chance(0.015f))
                {
                    List<Thing> sweetCatch = pawn.Map.Biome.fishTypes.rareCatchesSetMaker.root.Generate();
                    __result.AddRange(sweetCatch);
                    pawn.Map.waterBodyTracker.lastRareCatchTick = Find.TickManager.TicksGame;
                    Find.LetterStack.ReceiveLetter("LetterLabelRareCatch".Translate(), "LetterTextRareCatch".Translate(pawn.Named("PAWN")) + ":\n" + sweetCatch.Select((Thing x) => x.LabelCap).ToLineList("  - ", false), LetterDefOf.PositiveEvent, sweetCatch, null, null, null, null, 0, true);
                }
            }
        }
        //apply the mood changes (and grav nausea in the latter case) of Skybound and Earthborne after a gravship landing or transporter (e.g. drop pod) landing
        public static void HVTLandingEndedPrefix(WorldComponent_GravshipController __instance)
        {
            Gravship gship = GetInstanceField(typeof(WorldComponent_GravshipController), __instance, "gravship") as Gravship;
            if (gship != null)
            {
                foreach (Pawn pawn in gship.Pawns)
                {
                    HVTUtility.DoAerospaceFlyingThoughts(pawn);
                }
            }
        }
        public static void HVTDoArrivalActionPrefix(TravellingTransporters __instance)
        {
            List<ActiveTransporterInfo> atis = GetInstanceField(typeof(TravellingTransporters), __instance, "transporters") as List<ActiveTransporterInfo>;
            if (atis != null)
            {
                foreach (ActiveTransporterInfo ati in atis)
                {
                    foreach (Thing thing in ati.innerContainer)
                    {
                        if (thing is Pawn pawn)
                        {
                            HVTUtility.DoAerospaceFlyingThoughts(pawn);
                        }
                    }
                }
            }
        }
        //skulkers in a caravan turn invisible on entering a new map. This invisibility is (Ex), not (Su), and so upon using a verb (which would logically get you noticed), it ends; that part is, however, handled by a hediff comp and not this patch.
        public static void HautsTraitsEnterPrefix(Caravan caravan, CaravanEnterMode enterMode)
        {
            if (enterMode == CaravanEnterMode.Edge || enterMode == CaravanEnterMode.None)
            {
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    if (p.story != null && p.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HVTDefOf.HVT_SkulkerSurpriseStealth, p, null);
                        p.health.AddHediff(hediff, p.health.hediffSet.GetBrain(), null, null);
                    }
                }
            }
        }
        //the Sabotage skulker raid variant deploys unique IEDs which take hours to go off, but they should still go off nigh-instantly when stepped on to add some threat. This patch handles that.
        public static void HVTSkulkSpringPostfix(Building_Trap __instance)
        {
            if (__instance.Faction != Faction.OfPlayer)
            {
                if (__instance.def.HasModExtension<SabotageExplosive>())
                {
                    CompExplosive compExplosive = __instance.GetComp<CompExplosive>();
                    if (compExplosive.wickStarted && compExplosive.wickTicksLeft > compExplosive.Props.wickTicks.max )
                    {
                        compExplosive.wickTicksLeft = compExplosive.Props.wickTicks.max;
                    }
                }
            }
        }
        //to prevent the saboteurs who spawned with the aforementioned IEDs from running around in a panic because of a nearby lit fuse, this patch restricts such a reaction to (non-drafted) player pawns only.
        public static void HVTNotifyNearbyPawnsOfDangerousExplosivePrefix(Thing exploder, ref Faction onlyFaction)
        {
            if (exploder.Faction != Faction.OfPlayer)
            {
                if (exploder.def.HasModExtension<SabotageExplosive>())
                {
                    onlyFaction = Faction.OfPlayerSilentFail;
                }
            }
        }
        //patches in the ability for Skulkers to check out a site for threat point-requiring or hidden parts without the caravan actually entering the site
        public static IEnumerable<FloatMenuOption> HVT_GetFloatMenuOptionsPostfix(IEnumerable<FloatMenuOption>  __result, Caravan caravan, Site site)
        {
            foreach (FloatMenuOption fmo in __result)
            {
                yield return fmo;
            }
            if (HVTUtility.HasASkulker(caravan))
            {
                if (site.Faction == null || site.Faction != Faction.OfPlayer)
                {
                    yield return HVTUtility.GoScoutForAmbushes(caravan, site);
                }
            }
        }
        /*the following all handle growth moments
         * GeneratePawnPostfix determines a random number of traits a newly generated pawn should have (within the min to max ranges specified by the mod settings, and subject to how many growth moments the pawn could logically have had at its age).
         *   It adds traits until that random number is reached. ExciseTraitExempt traits and sexuality traits don't count towards the limit
         * IsGrowthBirthdayPostfix handles the interaction between the max traits mod setting and when and how many growth moments a pawn should have. See that setting's tooltip for breakdown.
         * GrowthPointsPerDayPostfix is the compensatory mechanism for a higher density of growth moments resulting in shorter periods to accrue growth points. The increases are proportional to the reduction in time between growth moments.
         *   This way, it is still feasible to achieve growth tier 8 even in the case of a max trait setting of 5 (growth moments at 5, 7, 9, 11, and 13, so just two years to accrue points each time; necessitates a +50% boost).
         * BirthdayBiologicalPostfix is for max traits set to 6 or higher. Since these require ludicrous amounts of growth moments (and they'd have to be jarringly distributed across years 4-13), instead some or all of them
         *   place TWO (or in 9mts' case, THREE) growth moments in one year. These "bonus growth moments" do not grant passions to avoid a scenario where a pawn can reach so many passions they CAN'T select new ones, and therefore
         *   can't finalize their Growth Moment choices.
         * MakeChoicesPrefix/Postfix conserve growth points between uses of a conventional growth moment and bonus growth moments.*/
        public static void HVTGeneratePawnPostfix(ref Pawn __result, PawnGenerationRequest request)
        {
            if (__result.story != null)
            {
                int traitCount = __result.story.traits.allTraits.Count;
                foreach (Trait t in __result.story.traits.allTraits)
                {
                    if (t.def.exclusionTags.Contains("SexualOrientation") || TraitModExtensionUtility.IsExciseTraitExempt(t.def))
                    {
                        traitCount--;
                    }
                }
                int ageBiologicalYears = __result.ageTracker.AgeBiologicalYears;
                int ageDependentMax = 0;
                switch ((int)HVT_Mod.settings.traitsMax)
                {
                    case 3:
                        if (ageBiologicalYears > 13) {
                            ageDependentMax = 3;
                        } else if (ageBiologicalYears > 10) {
                            ageDependentMax = 2;
                        } else if (ageBiologicalYears > 7) {
                            ageDependentMax = 1;
                        }
                        break;
                    case 4:
                        if (ageBiologicalYears > 13) {
                            ageDependentMax = 4;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 3;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 2;
                        } else if (ageBiologicalYears > 6) {
                            ageDependentMax = 1;
                        }
                        break;
                    case 5:
                        if (ageBiologicalYears > 13) {
                            ageDependentMax = 5;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 4;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 3;
                        } else if (ageBiologicalYears > 7) {
                            ageDependentMax = 2;
                        } else if (ageBiologicalYears > 5) {
                            ageDependentMax = 1;
                        }
                        break;
                    case 6:
                        if (ageBiologicalYears > 13) {
                            ageDependentMax = 6;
                        } else if (ageBiologicalYears > 10) {
                            ageDependentMax = 4;
                        } else if (ageBiologicalYears > 7) {
                            ageDependentMax = 2;
                        }
                        break;
                    case 7:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 7;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 5;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 3;
                        } else if (ageBiologicalYears > 6) {
                            ageDependentMax = 1;
                        }
                        break;
                    case 8:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 8;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 6;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 4;
                        } else if (ageBiologicalYears > 6) {
                            ageDependentMax = 2;
                        }
                        break;
                    case 9:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 9;
                        } else if (ageBiologicalYears > 10) {
                            ageDependentMax = 6;
                        } else if (ageBiologicalYears > 7) {
                            ageDependentMax = 3;
                        }
                        break;
                    case 10:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 10;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 8;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 6;
                        } else if (ageBiologicalYears > 7) {
                            ageDependentMax = 4;
                        } else if (ageBiologicalYears > 5) {
                            ageDependentMax = 2;
                        }
                        break;
                    case 11:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 11;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 8;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 5;
                        } else if (ageBiologicalYears > 6) {
                            ageDependentMax = 2;
                        }
                        break;
                    case 12:
                        if (ageBiologicalYears > 13)
                        {
                            ageDependentMax = 12;
                        } else if (ageBiologicalYears > 11) {
                            ageDependentMax = 9;
                        } else if (ageBiologicalYears > 9) {
                            ageDependentMax = 6;
                        } else if (ageBiologicalYears > 6) {
                            ageDependentMax = 3;
                        }
                        break;
                    default:
                        ageDependentMax = (int)HVT_Mod.settings.traitsMax;
                        break;
                }
                //Be assigned additional traits until you reach a random number within the min and max range. Min always no larger than max
                int ageDependentMin = Math.Min((int)HVT_Mod.settings.traitsMin,ageDependentMax);
                int howManyTraits = Rand.RangeInclusive(ageDependentMin, ageDependentMax);
                int num = 10;
                while (traitCount < howManyTraits && num > 0)
                {
                    num--;
                    Trait trait = PawnGenerator.GenerateTraitsFor(__result, 1, new PawnGenerationRequest?(request), true).FirstOrFallback(null);
                    if (trait != null)
                    {
                        __result.story.traits.GainTrait(trait, false);
                        traitCount++;
                    }
                }
            }
        }
        public static void HVTIsGrowthBirthdayPostfix(ref bool __result, int age)
        {
            __result = false;
            if (age == 13)
            {
                __result = true;
            }
            int traitsMax = (int)HVT_Mod.settings.traitsMax;
            if ((traitsMax % 3) == 0 && traitsMax != 12 && (age == 10 || age == 7))
            {
                __result = true;
            } else if ((traitsMax == 4 || traitsMax == 7 || traitsMax == 8 || traitsMax >= 11) && (age == 11 || age == 9 || age == 6)) {
                __result = true;
            } else if ((traitsMax == 5 || traitsMax == 10) && (age == 11 || age == 9 || age == 7 || age == 5)) {
                __result = true;
            }
        }
        public static void HVTGrowthPointsPerDayPostfix(ref float __result, Pawn_AgeTracker __instance)
        {
            if (__result != 0f)
            {
                Pawn pawn = GetInstanceField(typeof(Pawn_AgeTracker), __instance, "pawn") as Pawn;
                if (pawn != null)
                {
                    int traitsMax = (int)HVT_Mod.settings.traitsMax;
                    if (traitsMax == 4 || traitsMax == 7 || traitsMax == 8 || traitsMax >= 11) {
                        if ((float)__instance.AgeBiologicalYearsFloat < 7f)
                        {
                            __result /= 0.75f;
                        }
                        if ((float)__instance.AgeBiologicalYearsFloat > 9f)
                        {
                            __result *= 1.5f;
                        }
                    } else if (traitsMax == 5 || traitsMax == 10) {
                        if ((float)__instance.AgeBiologicalYearsFloat < 7f)
                        {
                            __result /= 0.75f;
                        }
                        __result *= 1.5f;
                    }
                }
            }
        }
        public static void HVTBirthdayBiologicalPostfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = GetInstanceField(typeof(Pawn_AgeTracker), __instance, "pawn") as Pawn;
            int age = pawn.ageTracker.AgeBiologicalYears;
            int traitsMax = (int)HVT_Mod.settings.traitsMax;
            switch (traitsMax)
            {
                case 6:
                    if (age == 7 || age == 10 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 7:
                    if (age == 9 || age == 11 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 8:
                    if (age == 6 || age == 9 || age == 11 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 9:
                    if (age == 7 || age == 10 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 10:
                    if (age == 5 || age == 7 || age == 9 || age == 11 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 11:
                    if (age == 6)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    if (age == 9 || age == 11 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                case 12:
                    if (age == 6 || age == 9 || age == 11 || age == 13)
                    {
                        HVTUtility.DoBonusGrowthMoment(pawn);
                        HVTUtility.DoBonusGrowthMoment(pawn);
                    }
                    break;
                default:
                    break;
            }
        }
        public static void HVTMakeChoicesPrefix(ref float __state, ChoiceLetter_GrowthMoment __instance)
        {
            __state = __instance.pawn.ageTracker.growthPoints;
        }
        public static void HVTMakeChoicesPostfix(float __state, ChoiceLetter_GrowthMoment __instance)
        {
            bool refundGrowthPoints = false;
            Pawn pawn = __instance.pawn;
            int age = pawn.ageTracker.AgeBiologicalYears;
            int traitsMax = (int)HVT_Mod.settings.traitsMax;
            if ((traitsMax == 4 || traitsMax == 7 || traitsMax == 8 || traitsMax >= 11) && (age == 10 || age == 7))
            {
                refundGrowthPoints = true;
            } else if ((traitsMax == 5 || traitsMax == 10) && age == 10) {
                refundGrowthPoints = true;
            }
            if (refundGrowthPoints)
            {
                pawn.ageTracker.growthPoints = __state;
            }
        }
    }

}