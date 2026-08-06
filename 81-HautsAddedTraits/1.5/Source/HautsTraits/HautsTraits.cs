using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using HarmonyLib;
using RimWorld.Planet;
using VFECore.Shields;
using HautsFramework;
using System.Linq.Expressions;
using Verse.Noise;
using RimWorld.QuestGen;

namespace HautsTraits
{

    /*Log.Warning(string.Concat(new object[]
    {
        "Pawn Name Is: ",
        pawn.Name.ToStringShort
    }));*/
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
            if (ModsConfig.RoyaltyActive || ModsConfig.AnomalyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), new[] { typeof(Caravan), typeof(Map), typeof(CaravanEnterMode), typeof(CaravanDropInventoryMode), typeof(bool), typeof(Predicate<IntVec3>) }),
                              prefix: new HarmonyMethod(patchType, nameof(HautsTraitsEnterPrefix)));
            }
            harmony.Patch(AccessTools.Method(typeof(Building_Trap), nameof(Building_Trap.Spring)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTSkulkSpringPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GenExplosion), nameof(GenExplosion.NotifyNearbyPawnsOfDangerousExplosive)),
                          prefix: new HarmonyMethod(patchType, nameof(HVTNotifyNearbyPawnsOfDangerousExplosivePrefix)));
            harmony.Patch(AccessTools.Method(typeof(Caravan), nameof(Caravan.GetGizmos)),
                          postfix: new HarmonyMethod(patchType, nameof(HVTGetGizmosPostfix)));
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
                InspirationDef randomAvailableInspirationDef = __instance.pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                TaggedString reasonText = "HVT_DaydreamerInspired".Translate().CapitalizeFirst().Formatted(__instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn, "PAWN", true).Resolve();
                if (mentalBreakInspirationGainSet.Count != 0)
                {
                    __instance.pawn.mindState.inspirationHandler.TryStartInspiration(mentalBreakInspirationGainSet.RandomElement<InspirationDef>(), reasonText, true);
                }
                else
                {
                    __instance.pawn.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, reasonText, true);
                }
            }
        }
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
                if (dinfo != null && dinfo.Value.Def.ExternalViolenceFor(victim) && dinfo.Value.Instigator != null && dinfo.Value.Instigator is Pawn p && !p.Dead && p.needs.mood != null && p.story != null && !p.IsMutant)
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
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Mentor) && recipient.skills != null)
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
                            float xp = (200f + (Rand.Value * 800f)) * pawn.skills.GetSkill(skillToTeach).LearnRateFactor(true) * recipient.skills.GetSkill(skillToTeach).LearnRateFactor(false);
                            recipient.skills.Learn(skillToTeach, xp);
                        }
                    }
                }
            }
        }
        public static void HVTOpinionOffsetOfGroupPostfix(ref int __result, ThoughtHandler __instance, ISocialThought group, Pawn otherPawn)
        {
            if (__instance.pawn.story != null)
            {
                if (__result < 0f && ModsConfig.IdeologyActive)
                {
                    Thought thought = (Thought)group;
                    if (thought.sourcePrecept != null && __instance.pawn.ideo != null && otherPawn.ideo != null && __instance.pawn.Ideo != otherPawn.Ideo && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Tolerant))
                    {
                        __result /= 2;
                    }
                }
                if (__result < 0 && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Judgemental))
                {
                    __result *= 2;
                }
            }
        }
        public static bool HVTAllegiantBreakCanOccurPrefix(Pawn pawn, bool causedByMood)
        {
            if (causedByMood && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Allegiant))
            {
                return false;
            }
            return true;
        }
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
        public static void HVTBookwormOnBookReadTickPostfix(Pawn pawn)
        {
            if (pawn.IsHashIntervalTick(750) && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Bookworm) && pawn.needs.mood != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_Bibliophilia);
            }
        }
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
        public static void HVTGraverGiveObservedHistoryEventPrefix(Corpse __instance, Pawn observer)
        {
            if (__instance.InnerPawn.RaceProps.Humanlike && __instance.StoringThing() == null && observer.story != null && observer.story.traits.HasTrait(HVTDefOf.HVT_Graver))
            {
                Thought_MemoryObservation thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(HVTDefOf.HVT_ObservedLayingCorpseGraver);
                thought_MemoryObservation.Target = __instance;
                observer.needs.mood.thoughts.memories.TryGainMemory(thought_MemoryObservation);
            }
        }
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
        public static void HVTRevellerTryStartInspirationPostfix(ref bool __result, InspirationHandler __instance)
        {
            if (__result && __instance.pawn.Map != null && __instance.pawn.story != null && __instance.pawn.story.traits.HasTrait(HVTDefOf.HVT_Reveller))
            {
                GatheringDefOf.Party.Worker.TryExecute(__instance.pawn.Map, __instance.pawn);
            }
        }
        public static void HVTSkulkerHostilesNotClickablePostfix(ref bool __result, Thing t)
        {
            Pawn pawn;
            if ((pawn = (t as Pawn)) != null && (pawn.Faction == null || (pawn.Faction != null && pawn.HostileTo(Faction.OfPlayer))) && (pawn.story != null) && (!pawn.Downed) && (!pawn.IsPrisoner) && (!pawn.IsSlave))
            {
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    __result = false;
                }
            }
        }
        public static void HVTIntolerantConvertIdeoAttemptInteractedPostfix(Pawn initiator, Pawn recipient)
        {
            if (recipient.needs.mood != null && recipient.story != null && recipient.story.traits.HasTrait(HVTDefOf.HVT_Intolerant))
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FailedConvertIdeoAttemptResentment, initiator, null);
            }
        }
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
        public static void HVTInhumanRambling_RandomSelectionWeightPostfix(Pawn initiator, ref float __result)
        {
            if (initiator.MentalStateDef != null && initiator.MentalStateDef == HVTDefOf.HVT_HumanityBreak)
            {
                __result = 999f;
            }
        }
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
        public static IEnumerable<Gizmo> HVTGetGizmosPostfix(IEnumerable<Gizmo> __result, Caravan __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }
            bool hasSkulker = false;
            foreach (Pawn p in __instance.PawnsListForReading)
            {
                if (p.Faction != null && p.Faction == Faction.OfPlayerSilentFail && p.story != null && p.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    hasSkulker = true;
                    break;
                }
            }
            if (hasSkulker)
            {
                if (Find.WorldObjects.AnySiteAt(__instance.Tile))
                {
                    Site site = Find.WorldObjects.SiteAt(__instance.Tile);
                    if (site.Faction == null && site.Faction != Faction.OfPlayer)
                    {
                        yield return (new Command_Action
                        {
                            icon = Settlement.AttackCommand,
                            defaultLabel = "HVT_SFAIcon".Translate(),
                            defaultDesc = "HVT_SFATooltip".Translate(),
                            action = delegate ()
                            {
                                HVTUtility.ScoutForAmbushes(__instance, site);
                            }
                        });
                    }
                }
                if (Find.WorldObjects.AnySettlementAt(__instance.Tile))
                {
                    Settlement settlement = Find.WorldObjects.SettlementAt(__instance.Tile);
                    if (settlement.Faction != __instance.Faction && settlement.trader != null)
                    {
                        yield return (new Command_Action
                        {
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true),
                            defaultLabel = "HVT_BurgleIcon".Translate(),
                            defaultDesc = "HVT_BurgleTooltip".Translate(),
                            action = delegate ()
                            {
                                HVTUtility.Burgle(__instance, settlement);
                            }
                        });
                    }
                }
            }
        }
        public static void HVTGeneratePawnPostfix(ref Pawn __result, PawnGenerationRequest request)
        {
            if (__result.story != null)
            {
                int traitCount = __result.story.traits.allTraits.Count;
                foreach (Trait t in __result.story.traits.allTraits)
                {
                    if (t.def.exclusionTags.Contains("SexualOrientation") || HautsUtility.IsExciseTraitExempt(t.def) || HautsUtility.IsOtherDisallowedTrait(t.def))
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
                    default:
                        ageDependentMax = (int)HVT_Mod.settings.traitsMax;
                        break;
                }
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
            if ((traitsMax%3) == 0 && (age == 10 || age == 7))
            {
                __result = true;
            } else if ((traitsMax == 4 || traitsMax == 7 || traitsMax == 8) && (age == 11 || age == 9 || age == 6)) {
                __result = true;
            } else if (traitsMax == 5 && (age == 11 || age == 9 || age == 7 || age == 5)) {
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
                    if (traitsMax == 4 || traitsMax == 7 || traitsMax == 8) {
                        if ((float)__instance.AgeBiologicalYearsFloat < 7f)
                        {
                            __result /= 0.75f;
                        }
                        if ((float)__instance.AgeBiologicalYearsFloat > 9f)
                        {
                            __result *= 1.5f;
                        }
                    } else if (traitsMax == 5) {
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
            if ((traitsMax == 4 || traitsMax == 7 || traitsMax == 8) && (age == 10 || age == 7))
            {
                refundGrowthPoints = true;
            } else if (traitsMax == 5 && age == 10) {
                refundGrowthPoints = true;
            }
            if (refundGrowthPoints)
            {
                pawn.ageTracker.growthPoints = __state;
            }
        }
    }
    [DefOf]
    public static class HVTDefOf
    {
        static HVTDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVTDefOf));
        }

        public static TraitDef HVT_Aestheticist;
        public static TraitDef HVT_Agrizoophobe;
        public static TraitDef HVT_Allegiant;
        public static TraitDef HVT_Asocial;
        public static TraitDef HVT_Bookworm;
        public static TraitDef HVT_Champion;
        public static TraitDef HVT_Conversationalist;
        public static TraitDef HVT_Daydreamer;
        public static TraitDef HVT_Graver;
        public static TraitDef HVT_Hedonist;
        public static TraitDef HVT_Judgemental;
        public static TraitDef HVT_Lech;
        public static TraitDef HVT_Lovesick;
        public static TraitDef HVT_MetabolicFreak;
        public static TraitDef HVT_Outdoorsy;
        public static TraitDef HVT_RepressedRage;
        public static TraitDef HVT_Reveller;
        public static TraitDef HVT_Sadist;
        public static TraitDef HVT_Skulker;
        public static TraitDef HVT_Sniper;
        public static TraitDef HVT_Staid;
        public static TraitDef HVT_Tempestophile;
        public static TraitDef HVT_Tranquil0;
        public static TraitDef HVT_Tranquil;
        public static TraitDef HVT_Vain;
        public static TraitDef HVT_Winsome;
        public static TraitDef HVT_Strong;
        public static TraitDef HVT_Humble;
        public static TraitDef HVT_Prideful;
        [MayRequireIdeology]
        public static TraitDef HVT_Doubtful;
        [MayRequireIdeology]
        public static TraitDef HVT_Intolerant;
        [MayRequireIdeology]
        public static TraitDef HVT_Tolerant;
        [MayRequireIdeology]
        public static TraitDef HVT_Subjugator;
        [MayRequireIdeology]
        public static TraitDef HVT_Conformist;
        [MayRequireBiotech]
        public static TraitDef HVT_Caretaker;
        [MayRequireBiotech]
        public static TraitDef HVT_Misopedist;
        [MayRequireBiotech]
        public static TraitDef HVT_Mechaphile;
        [MayRequireBiotech]
        public static TraitDef HVT_Mechaphobe;
        [MayRequireBiotech]
        public static TraitDef HVT_Environmentalist;
        [MayRequireBiotech]
        public static TraitDef HVT_GenePurist;
        [MayRequireBiotech]
        public static TraitDef HVT_Mentor;
        [MayRequireAnomaly]
        public static TraitDef HVT_MonsterHunter;
        [MayRequireAnomaly]
        public static TraitDef HVT_MonsterLover;
        [MayRequireAnomaly]
        public static TraitDef HVT_Twisted;

        public static ThoughtDef HVT_Bibliophilia;
        public static ThoughtDef HVT_StimulatingConversation;
        public static ThoughtDef HVT_ObservedLayingCorpseGraver;
        public static ThoughtDef HVT_LovesickLetdown;
        public static ThoughtDef HVT_RebuffedALovesick;
        public static ThoughtDef HVT_SadistSawMentalBreak;
        public static ThoughtDef HVT_SadistBad;
        [MayRequireBiotech]
        public static ThoughtDef HVT_MechaphileWitnessedMechDeath;
        [MayRequireBiotech]
        public static ThoughtDef HVT_MechaphobeKilledMech;
        [MayRequireAnomaly]
        public static ThoughtDef HVT_MonsterHunterWorld;

        public static JobDef HVT_UseTraitGiverSerum;

        [MayRequireAnomaly]
        public static MentalStateDef HVT_HumanityBreak;

        public static HediffDef HVT_RRUnleashed;
        public static HediffDef HVT_SkulkerSurpriseStealth;
        public static HediffDef HVT_BurgleCooldown;
        [MayRequireIdeology]
        public static HediffDef HVT_RadThinkerBuff;
        [MayRequireBiotech]
        public static HediffDef HVT_DoubleGrowthMoments;

        public static ThingDef Hauts_SabotageIED_HighExplosive;
        public static ThingDef Hauts_SabotageIED_AntigrainWarhead;

        public static PawnsArrivalModeDef HVT_SkulkIn;
        public static PawnsArrivalModeDef HVT_SkulkInBaseCluster;
        public static PawnsArrivalModeDef HVT_SkulkInBaseSplitUp;
        public static PawnsArrivalModeDef HVT_Assassins;
        public static PawnsArrivalModeDef HVT_SabotagePAM;
    }
    public class SpecificPNFChargeCost : DefModExtension
    {
        public SpecificPNFChargeCost()
        {

        }
        public Dictionary<int,int> chargeCosts;
    }
    public class ThoughtWorker_Agrizoophobia : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.story == null || !p.story.traits.HasTrait(HVTDefOf.HVT_Agrizoophobe) || !HVTUtility.NearWildAnimal(p))
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(0);
        }
    }
    public class ThoughtWorker_Skullspike : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.surroundings == null)
            {
                return false;
            }
            int val = p.surroundings.NumSkullspikeSightings();
            if (val > 8) {
                return ThoughtState.ActiveAtStage(2);
            } if (val > 3) {
                return ThoughtState.ActiveAtStage(1);
            } if (val > 0) {
                return ThoughtState.ActiveAtStage(0);
            }
            return false;
        }
    }
    public class Thought_ContagiousHysteria : Thought_Memory
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = this.CurStage.baseMoodEffect;
                if (!this.pawn.Spawned || (this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) < 0.2f && this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) < 0.2f))
                {
                    num = 0;
                }
                return num;
            }
        }
    }
    public class ThoughtWorker_SickForLove : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(p, false);
            if (directPawnRelation == null)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            Pawn other = directPawnRelation.otherPawn;
            if (other.Faction != p.Faction || (p.Spawned && !other.Spawned) || p.Tile != other.Tile || !directPawnRelation.otherPawn.relations.everSeenByPlayer)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return ThoughtState.ActiveAtStage(2);
        }
    }
    public class ThoughtWorker_UrMyEverything : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            foreach (DirectPawnRelation dpr in LovePartnerRelationUtility.ExistingLovePartners(pawn))
            {
                if (dpr.otherPawn == other)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public class ThoughtWorker_CreptOn : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Lech) || !RelationsUtility.AttractedToGender(other, pawn.gender) || pawn.ageTracker.AgeBiologicalYearsFloat < 16f)
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_ManYouCreepin : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!RelationsUtility.AttractedToGender(pawn, other.gender) || other.ageTracker.AgeBiologicalYearsFloat < 16f)
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_ILoveWater : ThoughtWorker
    {
        public override float MoodMultiplier(Pawn p)
        {
            if (p.Position.GetTerrain(p.Map).IsWater)
            {
                return 3f * base.MoodMultiplier(p);
            }
            if (!p.Position.Roofed(p.Map))
            {
                return this.rainCurve.Evaluate(p.Map.weatherManager.CurWeatherLerped.rainRate);
            }
            return 0f;
        }
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Spawned)
            {
                if (p.Position.GetTerrain(p.Map).IsWater)
                {
                    return ThoughtState.ActiveDefault;
                }
                if (!p.Position.Roofed(p.Map) && p.Map.weatherManager.curWeather.rainRate >= 0.5f)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
        protected SimpleCurve rainCurve = new SimpleCurve(new CurvePoint[]
		{
			new CurvePoint(0.5f, 1f),
			new CurvePoint(1f, 3f),
			new CurvePoint(3f, 5f)
        });
    }
    public class ThoughtWorker_InCaravan : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.IsFormingCaravan())
            {
                return ThoughtState.ActiveAtStage(0);
            }
            if (p.IsCaravanMember())
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return ThoughtState.Inactive;
        }
    }
    public class Thought_Lovefool : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(pawn);
                float howImThoughtOf = 0f;
                foreach (Pawn p in list)
                {
                    if (p.relations != null)
                    {
                        howImThoughtOf += p.relations.OpinionOf(this.pawn);
                    }
                }
                return Math.Min(20f,Math.Max(-20f,this.CurStage.baseMoodEffect* howImThoughtOf));
            }
        }
    }
    public class ThoughtWorker_HVT_SkulkerIsInvisible : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.IsPsychologicallyInvisible())
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    public class ThoughtWorker_SniperRangedWeapon : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveDefault;
        }
    }
    public class Thought_TempestWeather : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                if (this.pawn.Spawned && this.pawn.Map != null && this.pawn.Position.GetRoof(this.pawn.Map) == null)
                {
                    for (int i = 0; i < this.pawn.Map.GameConditionManager.ActiveConditions.Count; i++)
                    {
                        GameCondition gc = this.pawn.Map.GameConditionManager.ActiveConditions[i];
                        if (gc.def.HasModExtension<TempestophileLikedCondition>())
                        {
                            num += 10;
                        }
                    }
                    if (!this.pawn.Map.weatherManager.curWeather.HasModExtension<TempestophileDisLikedCondition>())
                    {
                        num += 4;
                    }
                }
                return num;
            }
        }
    }
    public class ThoughtWorker_ClothistVsSelf : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p.apparel.PsychologicallyNude;
        }
    }
    public class ThoughtWorker_BroYourGroinOrChestIsUncovered : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!p.story.CaresAboutOthersAppearance)
            {
                return false;
            }
            return ThoughtWorker_Precept_GroinOrChestUncovered.HasUncoveredGroinOrChest(otherPawn);
        }
    }
    public class ThoughtWorker_VainApparelQuality : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            int stage = -1;
            for (int i = 0; i < p.apparel.WornApparelCount; i++)
            {
                Apparel apparel = p.apparel.WornApparel[i];
                if (apparel.TryGetQuality(out QualityCategory cat))
                {
                    if (cat == QualityCategory.Awful)
                    {
                        return ThoughtState.ActiveAtStage(0);
                    } else if (cat == QualityCategory.Poor) {
                        stage = 1;
                    } else if (cat == QualityCategory.Normal && stage != 1) {
                        stage = 2;
                    }
                }
            }
            if (stage == -1)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(stage);
        }
    }
    public class Thought_VanityBeautyAndGreatItems : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = Math.Max(-40f,Math.Min(40f,5f * pawn.GetStatValue(StatDefOf.PawnBeauty, true, -1)));
                for (int i = 0; i < this.pawn.apparel.WornApparelCount; i++)
                {
                    Apparel apparel = this.pawn.apparel.WornApparel[i];
                    if (apparel.TryGetQuality(out QualityCategory cat))
                    {
                        if (cat == QualityCategory.Masterwork)
                        {
                            num += 3f;
                        }
                        else if (cat == QualityCategory.Legendary)
                        {
                            num += 6f;
                        }
                    }
                }
                return num;
            }
        }
    }
    public class ThoughtWorker_Winsome : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Winsome))
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_NeedComfortPerceptive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.needs.comfort == null)
            {
                return ThoughtState.Inactive;
            }
            switch (p.needs.comfort.CurCategory)
            {
                case ComfortCategory.Uncomfortable:
                    return ThoughtState.ActiveAtStage(0);
                case ComfortCategory.Normal:
                    return ThoughtState.ActiveAtStage(1);
                case ComfortCategory.Comfortable:
                    return ThoughtState.Inactive;
                case ComfortCategory.VeryComfortable:
                    return ThoughtState.ActiveAtStage(2);
                case ComfortCategory.ExtremelyComfortable:
                    return ThoughtState.ActiveAtStage(3);
                case ComfortCategory.LuxuriantlyComfortable:
                    return ThoughtState.ActiveAtStage(4);
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public class ThoughtWorker_FaithVDoubt : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Doubtful))
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_OfOtherIdeo : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.IdeologyActive || other.Ideo == null || !RelationsUtility.PawnsKnowEachOther(pawn, other) || pawn.Ideo == null)
            {
                return false;
            }
            if (other.Ideo != pawn.Ideo)
            {
                return true;
            }
            return false;
        }
    }
    public class ThoughtWorker_CravingLiberation : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Faction faction = p.HostFaction ?? p.Faction;
            if (faction == null)
            {
                return ThoughtState.Inactive;
            }
            if (FactionUtility.GetSlavesInFactionCount(faction) > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(p);
            foreach (Pawn other in list)
            {
                if (other.GuestStatus == GuestStatus.Prisoner)
                {
                    return ThoughtState.Inactive;
                }
            }
            if (p.IsPrisoner || p.IsSlave)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
    public class Thought_CravingLiberation : Thought_Situational
    {
        protected override float BaseMoodOffset {
            get
            {
                Faction faction = this.pawn.HostFaction ?? this.pawn.Faction;
                return (faction != null ? this.CurStage.baseMoodEffect * Math.Max((float)FactionUtility.GetSlavesInFactionCount(faction),1f):this.CurStage.baseMoodEffect);
            }
        }
    }
    public class Thought_CravingSubjugation : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (!this.pawn.IsSlave)
            {
                if (FactionUtility.GetSlavesInFactionCount(this.pawn.Faction) > 0)
                {
                    return 2f * (float)FactionUtility.GetSlavesInFactionCount(this.pawn.Faction);
                } else if ((this.pawn.Ideo != null && this.pawn.Ideo.GetRole(this.pawn) != null) || this.pawn.royalty.AllTitlesForReading.Count > 0) {
                    return 0f;
                }
            }
            return -10f;
        }
    }
    public class ThoughtWorker_Child : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || other.DevelopmentalStage == DevelopmentalStage.Adult || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.DevelopmentalStage == DevelopmentalStage.Baby || other.DevelopmentalStage == DevelopmentalStage.Child)
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class Thought_Situational_HateChildrenInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.DevelopmentalStage != DevelopmentalStage.Adult)
                        {
                            num--;
                        }
                    }
                }
            } else if (pawn.GetCaravan() != null) {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.RaceProps.Humanlike && p.DevelopmentalStage != DevelopmentalStage.Adult)
                    {
                        num--;
                    }
                }
            }
            if (num <= -15)
            {
                num = -15;
            }
            return num;
        }
    }
    public class Thought_Situational_NeedChildrenInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (this.pawn.Faction == null)
            {
                return -10f;
            }
            int num = 0;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.DevelopmentalStage != DevelopmentalStage.Adult)
                        {
                            num++;
                        }
                    }
                }
            } else if (pawn.GetCaravan() != null) {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.RaceProps.Humanlike && p.DevelopmentalStage != DevelopmentalStage.Adult)
                    {
                        num++;
                    }
                }
            }
            if (num == 0)
            {
                num = -10;
            } else if (num > 15) {
                num = 15;
            }
            return num;
        }
    }
    public class ThoughtWorker_Pregnant : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.health.hediffSet.HasPregnancyHediff())
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_IsPregnant : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.BiotechActive && p.story != null && p.health.hediffSet.HasPregnancyHediff())
            {
                if (p.story.traits.HasTrait(HVTDefOf.HVT_Misopedist))
                {
                    return ThoughtState.ActiveAtStage(0);
                } else if (p.story.traits.HasTrait(HVTDefOf.HVT_Caretaker)) {
                    return ThoughtState.ActiveAtStage(1);
                }
            }
            return ThoughtState.Inactive;
        }
    }
    public class ThoughtWorker_VsMechanitor : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || other.mechanitor == null || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            return ThoughtState.ActiveDefault;
        }
    }
    public class ThoughtWorker_IsMechanitor : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.BiotechActive || p.mechanitor != null)
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    public class Thought_Situational_MechsInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            float num = 0f;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.mechanitor != null)
                        {
                            num += enumerator.Current.mechanitor.UsedBandwidthFromSubjects;
                        }
                    }
                }
            } else if (pawn.GetCaravan() != null) {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.mechanitor != null)
                    {
                        num += p.mechanitor.UsedBandwidthFromSubjects;
                    }
                }
            }
            num *= this.BaseMoodOffset;
            num = Math.Max(Math.Min(num,20f),-40f);
            return num;
        }
    }
    public class Thought_Situational_PollutionOnTile : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            PollutionLevel pollution;
            if (this.pawn.Map != null)
            {
                pollution = Find.WorldGrid[this.pawn.Map.Tile].PollutionLevel();
            } else if (pawn.GetCaravan() != null) {
                pollution = Find.WorldGrid[pawn.GetCaravan().GetTileCurrentlyOver()].PollutionLevel();
            } else {
                return 0f;
            }
            if (pollution == PollutionLevel.Light)
            {
                num -= 15;
            } else if (pollution == PollutionLevel.Moderate) {
                num -= 26;
            } else if (pollution == PollutionLevel.Extreme) {
                num -= 37;
            }
            return this.BaseMoodOffset + num;
        }
    }
    public class ThoughtWorker_GenePurism : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.kindDef.race == pawn.kindDef.race)
            {
                if (pawn.genes == null)
                {
                    if (other.genes != null)
                    {
                        return ThoughtState.ActiveDefault;
                    }
                    return ThoughtState.Inactive;
                } else if (other.genes != null) {
                    if (other.genes.hybrid)
                    {
                        return ThoughtState.ActiveDefault;
                    }
                    if (pawn.genes.CustomXenotype != null)
                    {
                        if (other.genes.CustomXenotype == null)
                        {
                            return ThoughtState.ActiveDefault;
                        } else if (other.genes.CustomXenotype != pawn.genes.CustomXenotype) {
                            return ThoughtState.ActiveDefault;
                        }
                    } else {
                        if (pawn.genes.Xenotype != other.genes.Xenotype)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                        if (pawn.genes.xenotypeName != other.genes.xenotypeName)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    }
                } else {
                    return ThoughtState.ActiveDefault;
                }
            } else if (HVT_Mod.settings.genePuristsHateAliens) {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    public class ThoughtWorker_AnomalyActivityLevel : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.AnomalyActive) {
                if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
                {
                    return ThoughtState.ActiveDefault;
                }
                if (Find.Anomaly.Level > 0 && Find.Anomaly.Level != 6)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
        public override float MoodMultiplier(Pawn p)
        {
            if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
            {
                return base.MoodMultiplier(p);
            }
            return base.MoodMultiplier(p) * Math.Min(Find.Anomaly.Level,4f);
        }
    }
    public class ThoughtWorker_Planetkiller : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            foreach (GameCondition gc in Find.World.GameConditionManager.ActiveConditions)
            {
                if (gc is GameCondition_Planetkiller gcp && gcp.TicksLeft <= 36000000)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
    }
    public class ThoughtWorker_ThourtInhumanized : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!otherPawn.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            return otherPawn.Inhumanized() ? ThoughtState.ActiveAtStage(1) : ThoughtState.ActiveAtStage(0);
        }
    }
    public class ThoughtWorker_TwistedYearning : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.AnomalyActive)
            {
                return ThoughtState.Inactive;
            }
            return p.Inhumanized() ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(0);
        }
    }
    public class Thought_Planetkiller : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                foreach (GameCondition gc in Find.World.GameConditionManager.ActiveConditions)
                {
                    if (gc is GameCondition_Planetkiller gcp && gcp.TicksLeft <= 36000000)
                    {
                        num = Math.Max(10f - Mathf.RoundToInt(gcp.TicksLeft/3600000),0f);
                        break;
                    }
                }
                return num;
            }
        }
    }
    public class Thought_CatastrophicClimate : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                if (this.pawn.Spawned && this.pawn.Map != null)
                {
                    for (int i = 0; i < this.pawn.Map.GameConditionManager.ActiveConditions.Count; i++)
                    {
                        GameCondition gc = this.pawn.Map.GameConditionManager.ActiveConditions[i];
                        if (gc.def.HasModExtension<CatastrophistLikedCondition>())
                        {
                            num += 3;
                        }
                    }
                }
                return num;
            }
        }
    }
    public class ThoughtWorker_AnomalyInactive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.AnomalyActive && ((Find.Storyteller.difficulty.AnomalyPlaystyleDef.generateMonolith && (Find.Anomaly.Level == 0 || Find.Anomaly.Level == 6)) || !Find.Storyteller.difficulty.AnomalyPlaystyleDef.enableAnomalyContent) && p.story != null)
            {
                if(p.story.traits.HasTrait(HVTDefOf.HVT_MonsterHunter))
                {
                    return ThoughtState.ActiveAtStage(0);
                } else if (p.story.traits.HasTrait(HVTDefOf.HVT_MonsterLover)) {
                    return ThoughtState.ActiveAtStage(1);
                }
            }
            return ThoughtState.Inactive;
        }
    }
    public class ThoughtWorker_PseudoMonsterLove : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.kindDef.race == pawn.kindDef.race)
            {
                if (ModsConfig.BiotechActive && HVT_Mod.settings.monsterLoveForXenos)
                {
                    if (pawn.genes == null)
                    {
                        if (other.genes != null)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    } else if (other.genes != null)
                    {
                        if (other.genes.hybrid)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                        if (pawn.genes.CustomXenotype != null)
                        {
                            if (other.genes.CustomXenotype == null || other.genes.CustomXenotype != pawn.genes.CustomXenotype)
                            {
                                return ThoughtState.ActiveDefault;
                            }
                        } else if (pawn.genes.Xenotype != other.genes.Xenotype || pawn.genes.xenotypeName != other.genes.xenotypeName) {
                            return ThoughtState.ActiveDefault;
                        }
                    } else {
                        return ThoughtState.Inactive;
                    }
                }
            } else if (HVT_Mod.settings.monsterLoveForAliens) {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    /*public class RitualOutcomeComp_SpecificTrait : RitualOutcomeComp_Quality
    {
        public RitualOutcomeComp_SpecificTrait(TraitDef trait, SimpleCurve curve, string label)
        {
            this.trait = trait;
            this.curve = curve;
            this.label = label;
        }
        public override RitualOutcomeComp_Data MakeData()
        {
            return new RitualOutcomeComp_DataThingPresence();
        }
        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            int num = 0;
            RitualOutcomeComp_DataThingPresence ritualOutcomeComp_DataThingPresence = (RitualOutcomeComp_DataThingPresence)data;
            float num2 = (ritual.DurationTicks != 0) ? ((float)ritual.DurationTicks) : ritual.TicksPassedWithProgress;
            foreach (KeyValuePair<Thing, float> keyValuePair in ritualOutcomeComp_DataThingPresence.presentForTicks)
            {
                Pawn p = (Pawn)keyValuePair.Key;
                if (this.Counts(ritual.assignments, p) && keyValuePair.Value >= num2 / 2f)
                {
                    num++;
                }
            }
            return (float)((this.curve != null) ? ((int)Math.Min((float)num, this.curve.Points[this.curve.PointsCount - 1].x)) : num);
        }
        protected bool Counts(RitualRoleAssignments assignments, Pawn p)
        {
            if (p.story != null && p.RaceProps.Humanlike)
            {
                if (p.story.traits.HasTrait(this.trait))
                {
                    return true;
                }
            }
            return false;
        }
        public TraitDef trait;
    }*/
    public class MentalState_PanicFleeAnimals : MentalState
    {
        protected override bool CanEndBeforeMaxDurationNow
        {
            get
            {
                return false;
            }
        }
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
        public override void MentalStateTick()
        {
            base.MentalStateTick();
            if (this.pawn.IsHashIntervalTick(30))
            {
                if (this.lastWASeenTick < 0 || HVTUtility.NearWildAnimal(this.pawn))
                {
                    this.lastWASeenTick = Find.TickManager.TicksGame;
                }
                if (this.lastWASeenTick >= 0 && Find.TickManager.TicksGame >= this.lastWASeenTick + this.def.minTicksBeforeRecovery)
                {
                    base.RecoverFromState();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.lastWASeenTick, "lastWASeenTick", -1, false);
        }
        private int lastWASeenTick = -1;
    }
    public class MentalStateWorker_PanicFleeAnimals : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (pawn.Spawned && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Agrizoophobe))
            {
                if (pawn.Faction != null && pawn.Faction != Faction.OfPlayerSilentFail && Rand.Chance(0.9f))
                {
                    return false;
                }
            }
            return base.StateCanOccur(pawn) && HVTUtility.NearWildAnimal(pawn);
        }
    }
    public class MentalStateWorker_Eureka : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_Eureka : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            CompProperties_Book cpb = ThingDefOf.Schematic.GetCompProperties<CompProperties_Book>();
            if (cpb != null)
            {
                foreach (ReadingOutcomeProperties rop in cpb.doers)
                {
                    if (rop is BookOutcomeProperties_GainResearch bopgr)
                    {
                        List<ResearchTabDef> tabs = new List<ResearchTabDef>();
                        if (bopgr.tab != null)
                        {
                            tabs.Add(bopgr.tab);
                        }
                        if (bopgr.tabs != null)
                        {
                            foreach (BookOutcomeProperties_GainResearch.BookTabItem bti in bopgr.tabs)
                            {
                                if (!tabs.Contains(bti.tab))
                                {
                                    tabs.Add(bti.tab);
                                }
                            }
                        }
                        bool doNovel = true;
                        foreach (ResearchProjectDef rpd in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                        {
                            if (!rpd.IsFinished && tabs.Contains(rpd.tab) && rpd.techprintCount == 0 && (bopgr.exclude.Count == 0 || !bopgr.exclude.ContainsAny((BookOutcomeProperties_GainResearch.BookResearchItem i) => i.project == rpd)))
                            {
                                doNovel = false;
                                break;
                            }
                        }
                        if (doNovel)
                        {
                            this.GenerateBook(ThingDefOf.Novel, "HVT_VisionaryNovelTitle", "HVT_VisionaryNovelDesc");
                            return;
                        }
                        break;
                    }
                }
            }
            this.GenerateBook(ThingDefOf.Schematic, "HVT_VisionarySchematicTitle","HVT_VisionarySchematicDesc");
        }
        private void GenerateBook(ThingDef bookDef, string titleKey, string descKey)
        {
            Book book = (Book)ThingMaker.MakeThing(bookDef);
            CompQuality compQuality = book.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory q = QualityUtility.GenerateQualityCreatedByPawn(this.pawn, SkillDefOf.Intellectual);
                compQuality.SetQuality(q, null);
            }
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, true);
            typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, titleKey.Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve());
            typeof(Book).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, descKey.Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve());
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, false);
            typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, null);
            this.pawn.inventory.innerContainer.TryAdd(book, true);
            if (this.pawn.skills != null)
            {
                this.pawn.skills.Learn(SkillDefOf.Intellectual, this.pawn.skills.GetSkill(SkillDefOf.Intellectual).XpRequiredForLevelUp / 10f, true, true);
            }
        }
    }
    public class MentalStateWorker_RadThinker : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_RadThinker : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            if (ModsConfig.IdeologyActive && !this.pawn.Ideo.classicMode)
            {
                this.oldIdeo = this.pawn.Ideo;
                foreach (MemeDef m in DefDatabase<MemeDef>.AllDefsListForReading)
                {
                    if (m.disagreeableTraits != null && m.disagreeableTraits.Count > 0)
                    {
                        bool addToList = true;
                        foreach (TraitRequirement t in m.disagreeableTraits)
                        {
                            if (this.pawn.story.traits.HasTrait(t.def))
                            {
                                addToList = false;
                                this.disagreedMemes.Add(m);
                                break;
                            }
                        }
                        if (!addToList)
                        {
                            continue;
                        }
                    }
                    if (m.agreeableTraits != null && m.agreeableTraits.Count > 0)
                    {
                        foreach (TraitRequirement t in m.agreeableTraits)
                        {
                            if (this.pawn.story.traits.HasTrait(t.def))
                            {
                                this.agreedMemes.Add(m);
                            }
                        }
                    }
                }
                IdeoGenerationParms parms;
                List<MemeDef> forcedMeme = new List<MemeDef>();
                if (this.agreedMemes.Count > 0 && Rand.Value <= 0.66f)
                {
                    forcedMeme.Add(this.agreedMemes.RandomElement<MemeDef>());
                    parms = new IdeoGenerationParms(Faction.OfPlayer.def, false, null, this.disagreedMemes, forcedMeme);
                }
                else
                {
                    parms = new IdeoGenerationParms(Faction.OfPlayer.def, false, null, this.disagreedMemes);
                }
                this.newIdeo = IdeoGenerator.MakeIdeo(DefDatabase<IdeoFoundationDef>.AllDefs.RandomElement<IdeoFoundationDef>());
                this.newIdeo.culture = this.oldIdeo.culture;
                this.newIdeo.foundation.RandomizePlace();
                this.newIdeo.memes.Clear();
                this.newIdeo.memes.AddRange(IdeoUtility.GenerateRandomMemes(parms));
                this.newIdeo.SortMemesInDisplayOrder();
                this.newIdeo.classicExtraMode = parms.classicExtra;
                IdeoFoundation_Deity ideoFoundation_Deity;
                if ((ideoFoundation_Deity = (this.newIdeo.foundation as IdeoFoundation_Deity)) != null)
                {
                    ideoFoundation_Deity.GenerateDeities();
                }
                this.newIdeo.foundation.GenerateTextSymbols();
                this.newIdeo.foundation.GenerateLeaderTitle();
                this.newIdeo.foundation.RandomizeIcon();
                this.newIdeo.foundation.RandomizePrecepts(true, parms);
                this.newIdeo.RegenerateDescription(true);
                this.newIdeo.foundation.RandomizeStyles();
                this.pawn.ideo.SetIdeo(this.newIdeo);
                Find.IdeoManager.Add(this.newIdeo);
                Hediff hediff = HediffMaker.MakeHediff(HVTDefOf.HVT_RadThinkerBuff, this.pawn);
                this.pawn.health.AddHediff(hediff, this.pawn.health.hediffSet.GetBrain());
            }
        }
        private Ideo oldIdeo;
        private Ideo newIdeo;
        private readonly List<MemeDef> agreedMemes = new List<MemeDef>();
        private readonly List<MemeDef> disagreedMemes = new List<MemeDef>();
    }
    public class MentalStateWorker_SightstealerAttack : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!ModsConfig.AnomalyActive || !base.StateCanOccur(pawn) || (pawn.Faction !=null && (pawn.Faction == Faction.OfEntities || pawn.Faction == Faction.OfHoraxCult)) || !pawn.SpawnedOrAnyParentSpawned || pawn.GetStatValue(StatDefOf.PsychicSensitivity) < float.Epsilon)
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_SightstealerAttack : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            if (this.pawn.MapHeld != null)
            {
                Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_SightstealerAssault(), this.pawn.MapHeld, null);
                float num = StorytellerUtility.DefaultThreatPointsNow(this.pawn.MapHeld) * new FloatRange(0.2f, 0.55f).RandomInRange;
                num = Mathf.Max(Faction.OfEntities.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Sightstealers, null), num);
                List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                {
                    faction = Faction.OfEntities,
                    groupKind = PawnGroupKindDefOf.Sightstealers,
                    points = num,
                    tile = this.pawn.MapHeld.Tile
                }, true).ToList<Pawn>();
                foreach (Pair<List<Pawn>, IntVec3> pair in PawnsArrivalModeWorkerUtility.SplitIntoRandomGroupsNearMapEdge(list, this.pawn.MapHeld, false))
                {
                    foreach (Thing newThing in pair.First)
                    {
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(pair.Second, this.pawn.MapHeld, 8, null);
                        GenSpawn.Spawn(newThing, loc, this.pawn.MapHeld, WipeMode.Vanish);
                    }
                }
                foreach (Pawn p in list)
                {
                    lord.AddPawn(p);
                }
                SoundDefOf.Sightstealer_SummonedHowl.PlayOneShot(this.pawn);
            }
        }
    }
    public class PawnsArrivalModeWorker_SkulkIn : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            IntVec3 loc;
            Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
            IEnumerable<Thing> enumerable = map.mapPawns.FreeHumanlikesSpawnedOfFaction(hostFaction).Cast<Thing>();
            if (hostFaction == Faction.OfPlayer)
            {
                enumerable = enumerable.Concat(map.listerBuildings.allBuildingsColonist.Cast<Thing>());
            }
            else
            {
                enumerable = enumerable.Concat(from x in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                                               where x.Faction == hostFaction
                                               select x);
            }
            int num = 0;
            float num2 = 65f;
            IntVec3 intVec;
            for (; ; )
            {
                intVec = CellFinder.RandomCell(map);
                num++;
                if (!intVec.Fogged(map))
                {
                    if (num > 300)
                    {
                        break;
                    }
                    num2 -= 0.2f;
                    bool flag = false;
                    foreach (Thing thing in enumerable)
                    {
                        if ((float)(intVec - thing.Position).LengthHorizontalSquared < num2 * num2)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag && map.reachability.CanReachFactionBase(intVec, hostFaction))
                    {
                        loc = intVec;
                    }
                }
            }
            loc = intVec;
            GenSpawn.Spawn(pawns[0], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            if (pawns.Count > 1)
            {
                for (int i = 1; i < pawns.Count; i++)
                {
                    IntVec3 loc2 = CellFinder.RandomClosewalkCellNear(loc, map, 3, null);
                    GenSpawn.Spawn(pawns[i], loc2, map, parms.spawnRotation, WipeMode.Vanish, false);
                }
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            if (HVT_Mod.settings.disableStealthRaids)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
            }
            Map map = (Map)parms.target;
            if (parms.attackTargets != null && parms.attackTargets.Count > 0)
            {
                CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => !map.roofGrid.Roofed(p) && p.Walkable(map), map, CellFinder.EdgeRoadChance_Hostile, out parms.spawnCenter);
            }
            if (!parms.spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Hostile, false, null))
            {
                return false;
            }
            parms.spawnRotation = Rot4.FromAngleFlat((map.Center - parms.spawnCenter).AngleFlat);
            return true;
        }
    }
    public class PawnsArrivalModeWorker_SkulkInBaseCluster : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 3, null);
                while (!loc.Walkable(map))
                {
                    loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 3, null);
                }
                GenSpawn.Spawn(pawns[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || flag || map.listerBuildings.allBuildingsColonist.Count == 0)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                do
                {
                    if (map.listerBuildings.allBuildingsColonist.Count > 0)
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        toSpawn = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, null);
                    } else {
                        CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => !map.roofGrid.Roofed(p) && p.Walkable(map), map, CellFinder.EdgeRoadChance_Hostile, out toSpawn);
                    }
                } while (!toSpawn.Walkable(map));
                parms.spawnCenter = toSpawn;
            }
            return true;
        }
    }
    public class PawnsArrivalModeWorker_SkulkInBaseSplitUp : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            GenSpawn.Spawn(pawns[0], parms.spawnCenter, map, parms.spawnRotation, WipeMode.Vanish, false);
            if (pawns.Count > 1)
            {
                for (int i = 1; i < pawns.Count; i++)
                {
                    IntVec3 loc2;
                    do
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        loc2 = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        loc2 = CellFinder.RandomClosewalkCellNear(loc2, map, 2, null);
                    } while (!loc2.Walkable(map));
                    GenSpawn.Spawn(pawns[i], loc2, map, parms.spawnRotation, WipeMode.Vanish, false);
                }
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || flag || map.listerBuildings.allBuildingsColonist.Count == 0)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                do
                {
                    if (map.listerBuildings.allBuildingsColonist.Count > 0)
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        toSpawn = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, null);
                    } else {
                        CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => !map.roofGrid.Roofed(p) && p.Walkable(map), map, CellFinder.EdgeRoadChance_Hostile, out toSpawn);
                    }
                } while (!toSpawn.Walkable(map));
                parms.spawnCenter = toSpawn;
            }
            return true;
        }
    }
    public class PawnsArrivalModeWorker_SabotagePAM : PawnsArrivalModeWorker_SkulkInBaseSplitUp
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            base.Arrive(pawns, parms);
            Map map = (Map)parms.target;
            int timer = 10000 + (2500*(int)Math.Ceiling(Rand.Value * 5));
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawns[i].Position, map, 6, null);
                while (loc == parms.spawnCenter)
                {
                    loc = CellFinder.RandomClosewalkCellNear(pawns[i].Position, map, 6, null);
                }
                Building_TrapExplosive ied;
                if (Rand.Value <= 0.01f)
                {
                    ied = (Building_TrapExplosive)ThingMaker.MakeThing(HVTDefOf.Hauts_SabotageIED_AntigrainWarhead, null);
                } else {
                    ied = (Building_TrapExplosive)ThingMaker.MakeThing(HVTDefOf.Hauts_SabotageIED_HighExplosive, null);
                }
                ied.SetFactionDirect(parms.faction);
                ied.HitPoints = ied.def.BaseMaxHitPoints;
                GenSpawn.Spawn(ied, loc, map, WipeMode.Vanish);
                ied.GetComp<CompExplosive>().StartWick(null);
                ied.GetComp<CompExplosive>().wickTicksLeft = timer;
            }
        }
    }
    public class PawnsArrivalModeWorker_Assassins : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            Map map = (Map)parms.target;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                while (loc == parms.spawnCenter)
                {
                    loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                }
                GenSpawn.Spawn(pawns[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || flag || map.mapPawns.FreeColonistsAndPrisoners.Count == 0)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                int randomCell = (int)(Rand.Value * map.mapPawns.FreeColonistsAndPrisoners.Count);
                toSpawn = map.mapPawns.FreeColonistsAndPrisoners[randomCell].Position;
                toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, null);
                parms.spawnCenter = toSpawn;
            }
            return true;
        }
    }
    public class LordToil_Burgle : LordToil_DoOpportunisticTaskOrCover
    {
        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }
        protected override DutyDef DutyDef
        {
            get
            {
                return DutyDefOf.Steal;
            }
        }
        public override bool AllowSelfTend
        {
            get
            {
                return false;
            }
        }
        public override void Notify_PawnDamaged(Pawn victim, DamageInfo dinfo)
        {
            base.Notify_PawnDamaged(victim, dinfo);
            LordJob_StealFromColony sfc = (LordJob_StealFromColony)this.lord.LordJob;
            if (sfc != null)
            {
                sfc.maxPawnsAttackedBeforeRaid--;
                if (!victim.Dead)
                {
                    this.lord.RemovePawn(victim);
                    Lord lord = LordMaker.MakeNewLord(victim.Faction, new LordJob_AssaultColony(victim.Faction, true, true, false, false, false, false, true), victim.Map, null);
                    if (victim.carryTracker.CarriedThing != null)
                    {
                        victim.carryTracker.TryDropCarriedThing(victim.Position, ThingPlaceMode.Near, out Thing thing, null);
                    }
                    lord.AddPawn(victim);
                    victim.mindState.duty.def = DutyDefOf.AssaultColony;
                }
            }
        }
        protected override bool TryFindGoodOpportunisticTaskTarget(Pawn pawn, out Thing target, List<Thing> alreadyTakenTargets)
        {
            if (pawn.mindState.duty != null && pawn.mindState.duty.def == this.DutyDef && pawn.carryTracker.CarriedThing != null)
            {
                target = pawn.carryTracker.CarriedThing;
                return true;
            }
            return StealAIUtility.TryFindBestItemToSteal(pawn.Position, pawn.Map, 7f, out target, pawn, alreadyTakenTargets);
        }
        public override void UpdateAllDuties()
        {
            List<Thing> list = null;
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                Pawn pawn = this.lord.ownedPawns[i];
                Thing item = null;
                if (!this.cover || (this.TryFindGoodOpportunisticTaskTarget(pawn, out item, list) && !GenAI.InDangerousCombat(pawn)))
                {
                    if (pawn.mindState.duty == null || pawn.mindState.duty.def != this.DutyDef)
                    {
                        pawn.mindState.duty = new PawnDuty(this.DutyDef);
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                    }
                    if (list == null)
                    {
                        list = new List<Thing>();
                    }
                    list.Add(item);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Steal);
                }
            }
        }
        public override void LordToilTick()
        {
            if (this.cover && Find.TickManager.TicksGame % 181 == 0)
            {
                List<Thing> list = null;
                for (int i = 0; i < this.lord.ownedPawns.Count; i++)
                {
                    Pawn pawn = this.lord.ownedPawns[i];
                    if (!pawn.Downed && pawn.mindState.duty.def == DutyDefOf.AssaultColony)
                    {
                        if (this.TryFindGoodOpportunisticTaskTarget(pawn, out Thing thing, list) && !base.Map.reservationManager.IsReservedByAnyoneOf(thing, this.lord.faction) && !GenAI.InDangerousCombat(pawn))
                        {
                            pawn.mindState.duty = new PawnDuty(this.DutyDef);
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                            if (list == null)
                            {
                                list = new List<Thing>();
                            }
                            list.Add(thing);
                        }
                    }
                }
            }
        }
        public new bool cover = true;
    }
    public class LordJob_Assassinate : LordJob_AssaultColony
    {
        public LordJob_Assassinate()
        {
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
    }
    public class LordToil_HalfRunHalfFight : LordToil
    {
        public override bool ForceHighStoryDanger
        {
            get
            {
                return true;
            }
        }
        public LordToil_HalfRunHalfFight(bool isEspionage = false, bool useAvoidGridSmart = false)
        {
            espionage = isEspionage;
            useAvoidGrid = useAvoidGridSmart;
        }
        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }
        public override void Init()
        {
            base.Init();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
        }
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                this.lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest)
                {
                    attackDownedIfStarving = false,
                    pickupOpportunisticWeapon = false
                };
                if (espionage)
                {
                    Hediff hediff = HediffMaker.MakeHediff(HautsDefOf.HVT_Spy, this.lord.ownedPawns[i], null);
                    this.lord.ownedPawns[i].health.AddHediff(hediff, null, null, null);
                }
            }
            if (this.lord.ownedPawns.Count > 1)
            {
                Lord lordStayBehindAndFight = LordMaker.MakeNewLord(this.lord.faction, new LordJob_AssaultColony(this.lord.faction, true, true, false, true), this.lord.Map, null);
                List<Pawn> thoseStayingBehind = new List<Pawn>();
                for (int i = 1; i < this.lord.ownedPawns.Count; i++)
                {
                    if (Rand.Value < 0.5f)
                    {
                        thoseStayingBehind.Add(this.lord.ownedPawns[i]);
                    }
                }
                for (int i = 1; i < thoseStayingBehind.Count; i++)
                {
                    this.lord.RemovePawn(thoseStayingBehind[i]);
                    lordStayBehindAndFight.AddPawn(thoseStayingBehind[i]);
                    thoseStayingBehind[i].mindState.duty.def = DutyDefOf.AssaultColony;
                }
            }
        }
        readonly bool espionage = false;
    }
    public class LordJob_Espionage : LordJob
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_Espionage()
        {
        }
        public LordJob_Espionage(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_Espionage(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_HalfRunHalfFight lordToil_Espionage = new LordToil_HalfRunHalfFight(true, useAvoidGridSmart);
            stateGraph.AddToil(lordToil_Espionage);
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }
        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
    }
    public class LordJob_StealFromColony : LordJob
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_StealFromColony()
        {
        }
        public LordJob_StealFromColony(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_StealFromColony(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Burgle lordToil_StealCover = new LordToil_Burgle
            {
                //lordToil_StealCover.cover = false;
                useAvoidGrid = useAvoidGridSmart
            };
            stateGraph.AddToil(lordToil_StealCover);
            LordToil_Burgle lordToil_StealCover2 = new LordToil_Burgle
            {
                cover = false,
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_StealCover2);
            Transition transition = new Transition(lordToil_StealCover, lordToil_StealCover2, false, true);
            transition.AddTrigger(new Trigger_TicksPassedAndNoRecentHarm(1200));
            stateGraph.AddTransition(transition, false);
            LordToil_ExitMapAndDefendSelf lordToil_ExitMap = new LordToil_ExitMapAndDefendSelf
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_ExitMap);
            if (this.canTimeoutOrFlee)
            {
                Transition transition2 = new Transition(lordToil_StealCover, lordToil_ExitMap, false, true);
                transition2.AddSources(lordToil_StealCover2);
                transition2.AddTrigger(new Trigger_TicksPassed(BurgleTimeBeforeGiveUp.RandomInRange));
                transition2.AddTrigger(new Trigger_PawnHarmed(1, true, null));
                stateGraph.AddTransition(transition2, false);
                //currently need to find a way for individual hit pawns to go aggro - dissect triggers to find how get indiv pawn in group
                //also need to find a way to iterate for 3 pawns hit for the rest to go aggro
            }
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }

        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
        public int maxPawnsAttackedBeforeRaid = 2;
        private static readonly IntRange BurgleTimeBeforeGiveUp = new IntRange(26000, 38000);
    }
    public class LordJob_Sabotage : LordJob_AssaultColony
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_Sabotage()
        {
        }
        public LordJob_Sabotage(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_Sabotage(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil assaultColony = new LordToil_AssaultColony(false,canPickUpOpportunisticWeapons);
            if (this.useAvoidGridSmart)
            {
                assaultColony.useAvoidGrid = true;
            }
            stateGraph.AddToil(assaultColony);
            LordToil_ExitMapAndDefendSelf exitMapAndDefendSelf = new LordToil_ExitMapAndDefendSelf
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(exitMapAndDefendSelf);
            Transition transition = new Transition(assaultColony, exitMapAndDefendSelf,false,true);
            transition.AddTrigger(new Trigger_TicksPassed(new IntRange(1250, 2500).RandomInRange));
            stateGraph.AddTransition(transition);
            LordToil_ExitMap exitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
            stateGraph.AddToil(exitMap);
            Transition transition2 = new Transition(exitMapAndDefendSelf, exitMap, false, true);
            transition.AddTrigger(new Trigger_TicksPassed(new IntRange(3500, 5000).RandomInRange));
            stateGraph.AddTransition(transition2);
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }

        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
        public int maxPawnsAttackedBeforeRaid = 2;
    }
    public class RaidStrategyWorker_Burglary : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_StealFromColony(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Espionage : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Espionage(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Assassinate : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Assassinate();
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Sabotage : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Sabotage(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind) && parms.faction.def.techLevel >= TechLevel.Industrial;
        }
    }
    public class Hediff_HulkSmash : HediffWithComps
    {
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if ((this.pawn.InMentalState || (Rand.Value < 0.05f && this.pawn.needs != null && this.pawn.needs.mood != null && this.pawn.needs.mood.CurLevel <= this.pawn.mindState.mentalBreaker.BreakThresholdMinor)))
            {
                this.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "HVT_RROnDamage".Translate().CapitalizeFirst(), true, true, false, null, false, true);
            }
        }
    }
    public class Hediff_TYNAN : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.story != null)
            {
                if ((this.pawn.Faction != null && this.pawn.Faction != Faction.OfPlayerSilentFail && (this.pawn.Faction.HostileTo(Faction.OfPlayerSilentFail) || (this.pawn.equipment != null && this.pawn.equipment.Primary != null)))  || this.pawn.kindDef.requiredWorkTags == WorkTags.Violent)
                {
                    if (this.pawn.story.traits.allTraits.Count == 1)
                    {
                        this.pawn.story.traits.GainTrait(new Trait(TraitDefOf.Kind),true);
                    }
                } else {
                    this.pawn.story.traits.GainTrait(new Trait(HVTDefOf.HVT_Tranquil));
                }
                for (int i = this.pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.story.traits.allTraits[i].def == HVTDefOf.HVT_Tranquil0)
                    {
                        this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.allTraits[i]);
                        break;
                    }
                }
            }
        }
    }
    public class Hediff_Pride : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.MapHeld != null)
            {
                if (ExpectationsUtility.CurrentExpectationFor(this.pawn.MapHeld).order >= ExpectationsUtility.CurrentExpectationFor(this.pawn).order)
                {
                    this.Severity = 1.001f;
                } else {
                    this.Severity = 0.001f;
                }
            }
        }
    }
    public class Hediff_Conform : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(120000) && this.pawn.Faction != null && ModsConfig.IdeologyActive && !this.pawn.IsMutant)
            {
                HVTUtility.ConformistConversion(this.pawn, this.pawn.Faction.ideos.PrimaryIdeo, "HVT_PeriodicConformation".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve());
            }
        }
    }
    public class HediffCompProperties_IdeoMajoritySeverity : HediffCompProperties
    {
        public HediffCompProperties_IdeoMajoritySeverity()
        {
            this.compClass = typeof(HediffComp_IdeoMajoritySeverity);
        }
        public float severityWhileInMajority = 0.001f;
        public float severityWhileInMinority = 1f;
    }
    public class HediffComp_IdeoMajoritySeverity : HediffComp
    {
        public HediffCompProperties_IdeoMajoritySeverity Props
        {
            get
            {
                return (HediffCompProperties_IdeoMajoritySeverity)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if ((!ModsConfig.IdeologyActive || this.Pawn.ideo == null) && !this.Pawn.IsMutant)
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                this.Pawn.health.RemoveHediff(hediff);
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.Faction != null && this.Pawn.ideo != null && this.Pawn.Ideo != null && this.Pawn.Ideo == this.Pawn.Faction.ideos.PrimaryIdeo)
            {
                this.parent.Severity = this.Props.severityWhileInMajority;
            } else {
                this.parent.Severity = this.Props.severityWhileInMinority;
            }
        }
    }
    public class HediffCompProperties_FastHealPermanentWounds : HediffCompProperties
    {
        public HediffCompProperties_FastHealPermanentWounds()
        {
            this.compClass = typeof(HediffComp_FastHealPermanentWounds);
        }
    }
    public class HediffComp_FastHealPermanentWounds : HediffComp
    {
        public HediffCompProperties_FastHealPermanentWounds Props
        {
            get
            {
                return (HediffCompProperties_FastHealPermanentWounds)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.ResetTicksToHeal();
        }
        private void ResetTicksToHeal()
        {
            this.ticksToHeal = Rand.Range(5, 10) * 60000;
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            this.ticksToHeal--;
            if (this.ticksToHeal <= 0)
            {
                HediffComp_HealPermanentWounds.TryHealRandomPermanentWound(base.Pawn, this.parent.LabelCap);
                this.ResetTicksToHeal();
            }
        }
        public static void TryHealRandomPermanentWound(Pawn pawn, string cause)
        {
            Hediff hediff;
            if (!(from hd in pawn.health.hediffSet.hediffs
                  where hd.IsPermanent() || hd.def.chronic
                  select hd).TryRandomElement(out hediff))
            {
                return;
            }
            HealthUtility.Cure(hediff);
            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("MessagePermanentWoundHealed".Translate(cause, pawn.LabelShort, hediff.Label, pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent, true);
            }
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.ticksToHeal, "ticksToHeal", 0, false);
        }
        public override string CompDebugString()
        {
            return "ticksToHeal: " + this.ticksToHeal;
        }
        private int ticksToHeal;
    }
    public class HediffCompProperties_ManchurianCandidacy : HediffCompProperties
    {
        public HediffCompProperties_ManchurianCandidacy()
        {
            this.compClass = typeof(HediffComp_ManchurianCandidacy);
        }
        public int periodicity;
        public float range;
        public float chance;
        public FactionDef faction;
        public string onFactionChangeMessage;
    }
    public class HediffComp_ManchurianCandidacy : HediffComp
    {
        public HediffCompProperties_ManchurianCandidacy Props
        {
            get
            {
                return (HediffCompProperties_ManchurianCandidacy)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity) && this.Pawn.Spawned && (this.Pawn.Faction == null || this.Pawn.Faction.def != this.Props.faction) && (this.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight) || this.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)))
            {
                foreach (Pawn p in this.Pawn.Map.mapPawns.AllHumanlikeSpawned)
                {
                    if (p.Faction != null && p.Faction.def == this.Props.faction && p.Position.DistanceTo(this.Pawn.Position) <= this.Props.range && GenSight.LineOfSight(p.Position,this.Pawn.Position,this.Pawn.Map) && Rand.Chance(this.Props.chance))
                    {
                        if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                        {
                            Messages.Message(this.Props.onFactionChangeMessage.Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.NegativeEvent, true);
                        }
                        this.Pawn.SetFaction(p.Faction,p);
                        break;
                    }
                }
            }
        }
    }
    public class HediffCompProperties_SuperSpreader : HediffCompProperties
    {
        public HediffCompProperties_SuperSpreader()
        {
            this.compClass = typeof(HediffComp_SuperSpreader);
        }
        public float mtbDays;
    }
    public class HediffComp_SuperSpreader : HediffComp
    {
        public HediffCompProperties_SuperSpreader Props
        {
            get
            {
                return (HediffCompProperties_SuperSpreader) this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Pawn.IsHashIntervalTick(60) && Rand.MTBEventOccurs(this.Props.mtbDays, 60000f, 60f))
            {
                BiomeDef biome;
                if (this.Pawn.Tile != -1)
                {
                    biome = Find.WorldGrid[this.Pawn.Tile].biome;
                } else {
                    biome = DefDatabase<BiomeDef>.GetRandom();
                }
                IncidentDef incidentDef = DefDatabase<IncidentDef>.AllDefs.Where((IncidentDef d) => d.category == IncidentCategoryDefOf.DiseaseHuman).RandomElementByWeightWithFallback((IncidentDef d) => biome.CommonalityOfDisease(d), null);
                if (incidentDef != null)
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.DiseaseHuman, this.Pawn.Map ?? Find.Maps.Where((Map x) => x.IsPlayerHome).RandomElement<Map>());
                    incidentDef.Worker.TryExecute(parms);
                }
            }
        }
    }
    public class CompProperties_TargetEffectGiveTrait : CompProperties
    {
        public CompProperties_TargetEffectGiveTrait()
        {
            this.compClass = typeof(CompTargetEffect_GiveTrait);
        }
    }
    public class CompTargetEffect_GiveTrait : CompTargetEffect
    {
        public CompProperties_TargetEffectGiveTrait Props
        {
            get
            {
                return (CompProperties_TargetEffectGiveTrait)this.props;
            }
        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            if (!user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
            {
                return;
            }
            Pawn pawn = target as Pawn;
            if (pawn != null)
            {
                if (pawn.story == null || pawn.story.traits == null)
                {
                    return;
                }
                Job job = JobMaker.MakeJob(HVTDefOf.HVT_UseTraitGiverSerum, pawn, this.parent);
                job.count = 1;
                user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }
        }
    }
    public class SabotageExplosive : DefModExtension
    {
        public SabotageExplosive()
        {

        }
    }
    public class CompProperties_SabExp : CompProperties_Explosive
    {
        public CompProperties_SabExp()
        {
            this.compClass = typeof(CompSabExp);
        }
    }
    public class CompSabExp : CompExplosive
    {
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.Faction != null && this.parent.Faction == Faction.OfPlayerSilentFail && this.wickTicksLeft > this.Props.wickTicks.max)
            {
                this.StopWick();
            }
        }
    }
    public class CompProperties_PNF : CompProperties_ItemCharged
    {
        public CompProperties_PNF()
        {
            this.compClass = typeof(Comp_PNF);
        }
    }
    public class Comp_PNF : Comp_ItemCharged
    {
        public new CompProperties_PNF Props
        {
            get
            {
                return this.props as CompProperties_PNF;
            }
        }
        public override int InitialCharges()
        {
            return Math.Min(base.InitialCharges(),(int)HVT_Mod.settings.pnfStartingCharges);
        }
    }
    public class JobDriver_UseTraitGiverSerum : JobDriver
    {
        private Pawn Pawn
        {
            get
            {
                return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Thing Item
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Item, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(25, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate ()
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Pawn, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new Action(this.DoTraitWindow));
            yield break;
        }
        private void DoTraitWindow()
        {
            Comp_ItemCharged chargeSource = this.Item.TryGetComp<Comp_ItemCharged>();
            if (chargeSource != null)
            {
                TraitSerumWindow window = new TraitSerumWindow(this.Pawn, chargeSource);
                Find.WindowStack.Add(window);
            }
        }
        private Mote warmupMote;
    }
    public class GrantableTrait
    {
        public GrantableTrait(TraitDef traitDef, int degree, Pawn pawn)
        {
            this.traitDef = traitDef;
            this.degree = degree;
            TraitDegreeData tdd = traitDef.DataAtDegree(degree);
            this.chargeCost = this.GetChargeCost(traitDef,degree,pawn.gender);
            this.displayText = tdd.LabelCap + "(" + this.chargeCost + ")";
            this.tooltip = tdd.description.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
        }
        public int GetChargeCost(TraitDef t, int degree, Gender gender)
        {
            SpecificPNFChargeCost spnfcc = t.GetModExtension<SpecificPNFChargeCost>();
            if (spnfcc != null && spnfcc.chargeCosts.ContainsKey(degree))
            {
                return spnfcc.chargeCosts.TryGetValue(degree);
            }
            return (int)Math.Min(Math.Max(10 * (1.1 - (t.GetGenderSpecificCommonality(gender) / t.degreeDatas.Count)), 1), 10);
        }
        public TraitDef traitDef;
        public int degree;
        public int chargeCost;
        public string displayText;
        public string tooltip;
    }
    public class PersoneuroformatterScrambler : DefModExtension
    {
        public PersoneuroformatterScrambler()
        {

        }
    }
    public class TraitSerumWindow : Window
    {
        public override void PreOpen()
        {
            base.PreOpen();
            this.grantableTraits.Clear();
            foreach (TraitDef t in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (t.GetGenderSpecificCommonality(this.pawn.gender) > 0f && !isOtherDisallowedTrait(t) && !this.pawn.WorkTagIsDisabled(t.requiredWorkTags))
                {
                    if (t.HasModExtension<PersoneuroformatterScrambler>())
                    {
                        if (this.pawn.story.traits.GetTrait(t) == null)
                        {
                            GrantableTrait newGT = new GrantableTrait(t, t.degreeDatas.RandomElement().degree, this.pawn);
                            if (newGT.chargeCost <= this.remainingCharges)
                            {
                                grantableTraits.Add(newGT);
                            }
                        }
                    } else {
                        foreach (TraitDegreeData td in t.degreeDatas)
                        {
                            if (this.pawn.story.traits.GetTrait(t, td.degree) == null)
                            {
                                GrantableTrait newGT = new GrantableTrait(t, td.degree, this.pawn);
                                if (newGT.chargeCost <= this.remainingCharges)
                                {
                                    grantableTraits.Add(newGT);
                                }
                            }
                        }
                    }
                }
            }
            this.grantableTraits.SortBy((GrantableTrait g) => g.chargeCost, (GrantableTrait g) => g.displayText);
        }
        public int GetChargeCost(TraitDef t, int degree)
        {
            SpecificPNFChargeCost spnfcc = t.GetModExtension<SpecificPNFChargeCost>();
            if (spnfcc != null && spnfcc.chargeCosts.ContainsKey(degree))
            {
                return spnfcc.chargeCosts.TryGetValue(degree);
            }
            return (int)Math.Min(Math.Max(10 * (1.1 - (t.GetGenderSpecificCommonality(this.pawn.gender) / t.degreeDatas.Count)), 1), 10);
        }
        public TraitSerumWindow(Pawn pawn, Comp_ItemCharged chargeSource)
        {
            this.pawn = pawn;
            this.forcePause = true;
            this.remainingCharges = chargeSource.RemainingCharges;
            this.chargeSource = chargeSource;
        }
        private float Height
        {
            get
            {
                return CharacterCardUtility.PawnCardSize(this.pawn).y + Window.CloseButSize.y + 4f + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(500f, this.Height);
            }
        }
        public static bool isOtherDisallowedTrait(TraitDef t)
        {
            return HautsUtility.IsOtherDisallowedTrait(t);
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width*0.7f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "HVT_TraitSerumLabel".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), default(TipSignal));
            num += 14f;
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (GrantableTrait gt in this.grantableTraits)
            {
                bool flag = this.chosenTrait == gt.traitDef && this.chosenTraitDegree == gt.degree;
                bool flag2 = flag;
                listing_Standard.CheckboxLabeled(gt.displayText, ref flag, gt.tooltip);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.chosenTrait = gt.traitDef;
                        this.chosenTraitDegree = gt.degree;
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (!acceptanceReport.Accepted)
            {
                TextAnchor anchor = Text.Anchor;
                GameFont font = Text.Font;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Rect rect3 = rect;
                rect3.xMax = rect2.xMin - 4f;
                Widgets.Label(rect3, acceptanceReport.Reason.Colorize(ColoredText.WarningColor));
                Text.Font = font;
                Text.Anchor = anchor;
            }
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    Trait trait = new Trait(this.chosenTrait,this.chosenTraitDegree);
                    this.pawn.story.traits.GainTrait(trait,true);
                    for (int i = 0; i < this.GetChargeCost(trait.def,trait.Degree); i++)
                    {
                        this.chargeSource.UsedOnce();
                    }
                    if (trait.def.DataAtDegree(trait.Degree).skillGains != null)
                    {
                        for (int i = 0; i < trait.def.DataAtDegree(trait.Degree).skillGains.Count; i++)
                        {
                            SkillDef toBoost = trait.def.DataAtDegree(trait.Degree).skillGains[i].skill;
                            this.pawn.skills.GetSkill(toBoost).Level += trait.def.DataAtDegree(trait.Degree).skillGains[i].amount;
                        }
                    }
                    this.Close(true);
                    SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(this.pawn, MaintenanceType.None));
                    Messages.Message("HVT_TraitSerumSuccess".Translate(this.chosenTrait.DataAtDegree(chosenTraitDegree).GetLabelFor(this.pawn).CapitalizeFirst(),this.pawn.Named("PAWN")), this.pawn, MessageTypeDefOf.PositiveEvent, true);
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            if (this.chosenTrait == null)
            {
                return "HVT_TraitSerumNeedsChoice".Translate();
            } else if (isBadTraitCombo(this.chosenTrait, this.pawn)) {
                return "HVT_TraitSerumLPWokeException".Translate();
            }
            return AcceptanceReport.WasAccepted;
        }
        public bool isBadTraitCombo(TraitDef t, Pawn pawn)
        {
            return false;
        }
        private Pawn pawn;
        private int remainingCharges;
        private TraitDef chosenTrait = null;
        private int chosenTraitDegree = 0;
        private float scrollHeight;
        private Comp_ItemCharged chargeSource;
        private Vector2 scrollPosition;
        private List<GrantableTrait> grantableTraits = new List<GrantableTrait>();
    }
    public class BurgleWindow : Window
    {
        public BurgleWindow(Caravan caravan, List<Pawn> burglars, Settlement settlement, float burglaryMaxValue, float burglaryMaxWeight, float successChance)
        {
            this.burglars.Clear();
            this.thingsStolen.Clear();
            this.targetedThingCategories.Clear();
            this.categories.Clear();
            this.goodsList.Clear();
            this.caravan = caravan;
            this.burglars = burglars;
            this.forcePause = true;
            this.settlement = settlement;
            this.burglaryMaxValue = burglaryMaxValue;
            this.burglaryMaxWeight = burglaryMaxWeight;
            this.valueRemaining = burglaryMaxValue;
            this.weightRemaining = burglaryMaxWeight;
            this.successChance = successChance;
            this.goodsList = this.settlement.Goods.ToList<Thing>();
            foreach (Thing t in goodsList)
            {
                if (t.def.thingCategories != null)
                {
                    foreach (ThingCategoryDef d in t.def.thingCategories)
                    {
                        if (!this.categories.Contains(d))
                        {
                            this.categories.Add(d);
                        }
                    }
                }
            }
        }
        private float Height
        {
            get
            {
                return 459f + Window.CloseButSize.y + this.Margin * 2f;
            }
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000f, this.Height);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMax -= 4f + Window.CloseButSize.y;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width * 0.8f, this.scrollHeight);
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, viewRect, true);
            float num = 0f;
            Widgets.Label(0f, ref num, viewRect.width, "HVT_BurgleWindow1".Translate((int)this.burglaryMaxValue,this.settlement.Name,this.burglaryMaxWeight,(this.successChance*100f)), default(TipSignal));
            num += 14f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(0f, ref num, viewRect.width, "HVT_BurgleWindow2".Translate(), default(TipSignal));
            Text.Font = GameFont.Small;
            Widgets.Label(0f, ref num, viewRect.width, "HVT_BurgleWindow3".Translate(), default(TipSignal));
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect rect = new Rect(0f, num, inRect.width - 30f, 99999f);
            listing_Standard.Begin(rect);
            foreach (ThingCategoryDef t in this.categories)
            {
                bool flag = this.targetedThingCategories.Contains(t);
                bool flag2 = flag;
                listing_Standard.CheckboxLabeled("HVT_BurgleWindowCategories".Translate(t.label), ref flag, 15f);
                if (flag != flag2)
                {
                    if (flag)
                    {
                        this.targetedThingCategories.Add(t);
                    } else {
                        this.targetedThingCategories.Remove(t);
                    }
                }
            }
            listing_Standard.End();
            num += listing_Standard.CurHeight + 10f + 4f;
            if (Event.current.type == EventType.Layout)
            {
                this.scrollHeight = Mathf.Max(num, inRect.height);
            }
            Widgets.EndScrollView();
            Rect rect2 = new Rect(0f, inRect.yMax + 4f, inRect.width, Window.CloseButSize.y);
            AcceptanceReport acceptanceReport = this.CanClose();
            if (Widgets.ButtonText(rect2, "OK".Translate(), true, true, true, null))
            {
                if (acceptanceReport.Accepted)
                {
                    List<Thing> settlementGoods = new List<Thing>();
                    foreach (Thing t in this.goodsList)
                    {
                        if (this.targetedThingCategories.Any<ThingCategoryDef>())
                        {
                            foreach (ThingCategoryDef d in this.targetedThingCategories)
                            {
                                if (t.HasThingCategory(d))
                                {
                                    settlementGoods.Add(t);
                                }
                            }
                        } else {
                            settlementGoods = this.settlement.Goods.ToList<Thing>();
                        }
                    }
                    while (this.weightRemaining > 0f && this.valueRemaining > 0f && settlementGoods.Count > 0)
                    {
                        int triesRemaining = 30;
                        while (triesRemaining > 0)
                        {
                            triesRemaining--;
                            Thing t = settlementGoods.RandomElement<Thing>();
                            int lowerBoundStack = Math.Min(t.def.stackLimit, t.stackCount);
                            float stackMarketValue = lowerBoundStack * t.MarketValue;
                            float stackMass = lowerBoundStack * t.GetStatValue(StatDefOf.Mass);
                            if (stackMarketValue <= this.valueRemaining && stackMass <= this.weightRemaining)
                            {
                                this.valueRemaining -= stackMarketValue;
                                this.weightRemaining -= stackMass;
                                if (lowerBoundStack < t.stackCount)
                                {
                                    this.thingsStolen.Add(t.SplitOff(lowerBoundStack));
                                }
                                else
                                {
                                    this.thingsStolen.Add(t);
                                    settlementGoods.Remove(t);
                                }
                                break;
                            }
                        }
                        if (triesRemaining <= 0)
                        {
                            break;
                        }
                    }
                    foreach (Pawn p in this.burglars)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HVTDefOf.HVT_BurgleCooldown, p, null);
                        p.health.AddHediff(hediff, p.health.hediffSet.GetBrain(), null, null);
                    }
                    this.Close(true);
                    if (Rand.Value <= this.successChance)
                    {
                        if (this.thingsStolen.Count == 0)
                        {
                            TaggedString message = "HVT_BurgleOutcome1".Translate();
                            LookTargets toLook = new LookTargets(this.caravan);
                            ChoiceLetter tieLetter = LetterMaker.MakeLetter("HVT_BurgleLetter1".Translate(), message, LetterDefOf.NeutralEvent, toLook, null, null, null);
                            Find.LetterStack.ReceiveLetter(tieLetter, null);
                        } else {
                            TaggedString message = "HVT_BurgleOutcome2".Translate();
                            foreach (Thing t in this.thingsStolen)
                            {
                                //Log.Message(t.Label + " x" + t.stackCount);
                                this.settlement.trader.GetDirectlyHeldThings().Remove(t);
                                CaravanInventoryUtility.GiveThing(this.caravan, t);
                            }
                            LookTargets toLook = new LookTargets(this.caravan);
                            ChoiceLetter winLetter = LetterMaker.MakeLetter("HVT_BurgleLetter2".Translate(), message, LetterDefOf.PositiveEvent, toLook, null, null, null);
                            Find.LetterStack.ReceiveLetter(winLetter, null);
                        }
                    } else {
                        int lostGoodwill = -1*(int)((this.burglaryMaxValue - this.valueRemaining)/40f);
                        TaggedString message = "HVT_BurgleOutcome3".Translate(this.settlement.Faction,lostGoodwill);
                        LookTargets toLook = new LookTargets(this.caravan);
                        ChoiceLetter sadLetter = LetterMaker.MakeLetter("HVT_BurgleLetter3".Translate(), message, LetterDefOf.NegativeEvent, toLook, null, null, null);
                        Find.LetterStack.ReceiveLetter(sadLetter, null);
                        this.caravan.Faction.TryAffectGoodwillWith(this.settlement.Faction, lostGoodwill);
                    }
                    this.thingsStolen.Clear();
                    this.targetedThingCategories.Clear();
                    this.burglars.Clear();
                    this.categories.Clear();
                    this.goodsList.Clear();
                } else {
                    Messages.Message(acceptanceReport.Reason, null, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        private AcceptanceReport CanClose()
        {
            return AcceptanceReport.WasAccepted;
        }
        private Caravan caravan;
        private List<Pawn> burglars = new List<Pawn>();
        private Settlement settlement;
        private float scrollHeight;
        private float burglaryMaxWeight;
        private float burglaryMaxValue;
        private float successChance;
        private float weightRemaining;
        private float valueRemaining;
        private List<ThingCategoryDef> targetedThingCategories = new List<ThingCategoryDef>();
        private List<Thing> thingsStolen = new List<Thing>();
        List<ThingCategoryDef> categories = new List<ThingCategoryDef>();
        List<Thing> goodsList = new List<Thing>();
        private Vector2 scrollPosition;
    }
    public class SuperPsychicTrait : DefModExtension
    {
        public SuperPsychicTrait()
        {

        }
        public string descKey;
        public string descKeyFantasy;
        public string category;
    }
    public class SuperPsychicGene : DefModExtension
    {
        public SuperPsychicGene()
        {
        }
        public string category;
        public TraitDef correspondingTrait;
    }
    public class TempestophileLikedCondition : DefModExtension
    {
        public TempestophileLikedCondition()
        {
        }
    }
    public class TempestophileDisLikedCondition : DefModExtension
    {
        public TempestophileDisLikedCondition()
        {
        }
    }
    public class CatastrophistLikedCondition : DefModExtension
    {
        public CatastrophistLikedCondition()
        {
        }
    }
    public class AnarchistHatedFaction : DefModExtension
    {
        public AnarchistHatedFaction()
        {
        }
    }
    public static class HVTUtility
    {
        public static bool NearWildAnimal(Pawn pawn)
        {
            if (pawn.SpawnedOrAnyParentSpawned)
            {
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(pawn.PositionHeld, pawn.MapHeld, 12.9f, true).OfType<Pawn>().Distinct<Pawn>())
                {
                    if (p.RaceProps.Animal && (p.Faction == null || p.InAggroMentalState))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void DoSadistMoodStuff(Pawn recipient, Pawn pawn)
        {
            recipient.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_SadistSawMentalBreak, null);
            if (Rand.Value <= 0.1f)
            {
                InspirationDef randomAvailableInspirationDef = recipient.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                if (randomAvailableInspirationDef != null)
                {
                    TaggedString message = "HVT_SadistInspiration".Translate(pawn.Name.ToStringShort, recipient.Name.ToStringShort);
                    recipient.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, message, true);
                }
            }
            if (RelationsUtility.PawnsKnowEachOther(pawn, recipient))
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_SadistBad, recipient);
            }
        }
        public static bool StaidOverriden(Pawn pawn)
        {
            return false;
        }
        public static void ConformistConversion(Pawn pawn, Ideo ideo, TaggedString message)
        {
            bool changedIdeo = false;
            if (pawn.ideo.Ideo != ideo)
            {
                changedIdeo = true;
            }
            pawn.ideo.SetIdeo(ideo);
            if (Current.ProgramState == ProgramState.Playing && changedIdeo && pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message(message, pawn, MessageTypeDefOf.PositiveEvent, true);
            }
        }
        public static void ScoutForAmbushes(Caravan caravan, Site site)
        {
            float totalSkulkerPower = 0f;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn.Faction != null && pawn.Faction == caravan.Faction && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    totalSkulkerPower += (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) + (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing)/2)) * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
                }
            }
            int ambushCount = 0;
            foreach (SitePart part in site.parts)
            {
                if (part.def.defaultHidden && part.def.wantsThreatPoints && Rand.Value < totalSkulkerPower*0.4f)
                {
                    ambushCount++;
                }
            }
            TaggedString letterLabel;
            TaggedString letterText;
            LetterDef letterDef;
            if (ambushCount == 0)
            {
                letterLabel = "HVT_SFAletter1".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome1".Translate(site.LabelCap);
                letterDef = LetterDefOf.NeutralEvent;
            } else if (ambushCount == 1) {
                letterLabel = "HVT_SFAletter2".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome2".Translate(site.LabelCap);
                letterDef = LetterDefOf.ThreatSmall;
            } else {
                letterLabel = "HVT_SFAletter3".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome3".Translate(ambushCount,site.LabelCap);
                letterDef = LetterDefOf.ThreatSmall;
            }
            ChoiceLetter notification = LetterMaker.MakeLetter(
            letterLabel, letterText, letterDef, new LookTargets(site), null, null, null);
            Find.LetterStack.ReceiveLetter(notification, null);
        }
        public static void Burgle(Caravan caravan, Settlement settlement)
        {
            if (settlement.trader == null)
            {
                TaggedString message = "HVT_NotBurglable".Translate();
                Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                return;
            } else {
                float burglaryMaxWeight = 0f;
                float burglaryMaxValue = 0f;
                float successChance = 0f;
                List<Pawn> skulkersInCaravan = new List<Pawn>();
                foreach (Pawn p in caravan.PawnsListForReading)
                {
                    if (p.story != null && p.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                    {
                        skulkersInCaravan.Add(p);
                    }
                }
                float kleptoFactor = 1f, meleeDmgFactor, burgleCooldown;
                foreach (Pawn p in skulkersInCaravan)
                {
                    burglaryMaxWeight += MassUtility.Capacity(p,null) - MassUtility.GearAndInventoryMass(p);
                    if (p.health.hediffSet.HasHediff(HVTDefOf.HVT_BurgleCooldown))
                    {
                        Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(HVTDefOf.HVT_BurgleCooldown);
                        if (hediff.Severity >= 6.001f)
                        {
                            burgleCooldown = -200f;
                        } else {
                            burgleCooldown = 0.5f;
                        }
                    } else {
                        burgleCooldown = 1f;
                    }
                    if (ModsConfig.IsActive("VanillaExpanded.VanillaTraitsExpanded"))
                    {
                        kleptoFactor = p.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail("VTE_Kleptomaniac")) ? 1.3f : 1f;
                    }
                    meleeDmgFactor = p.GetStatValue(StatDefOf.MeleeDamageFactor)+p.GetStatValue(VFECore.VFEDefOf.VEF_MeleeAttackDamageFactor)-1f;
                    burglaryMaxValue += 100f*(p.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation)+(p.GetStatValue(StatDefOf.MoveSpeed)/4.6f)*(p.health.capacities.GetLevel(PawnCapacityDefOf.Sight)+(p.health.capacities.GetLevel(PawnCapacityDefOf.Hearing)/2))*meleeDmgFactor)*kleptoFactor;
                    successChance += burgleCooldown*(p.GetStatValue(StatDefOf.MoveSpeed) / 4.6f) * (p.health.capacities.GetLevel(PawnCapacityDefOf.Sight) + (p.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) / 2) + ((1 -p.GetStatValue(HautsDefOf.Hauts_TrackSize))/5))/(kleptoFactor*p.GetStatValue(StatDefOf.PawnTrapSpringChance)*1.9f);
                }
                successChance /= skulkersInCaravan.Count;
                if (skulkersInCaravan.Count == 0)
                {
                    TaggedString message = "HVT_BurglaryNoSkulkers".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                }
                if (burglaryMaxWeight <= 0f)
                {
                    TaggedString message = "HVT_BurglaryNoCarryCap".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                }
                else if (burglaryMaxValue <= 0f)
                {
                    TaggedString message = "HVT_BurglaryTooWeak".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                }
                else if (successChance <= 0f)
                {
                    TaggedString message = "HVT_BurglaryTooConspicuous".Translate();
                    Messages.Message(message, settlement, MessageTypeDefOf.RejectInput, true);
                    return;
                } else {
                    Find.WindowStack.Add(new BurgleWindow(caravan, skulkersInCaravan, settlement, burglaryMaxValue, burglaryMaxWeight, successChance));
                }
            }
        }
        /*public static List<Trait> GenerateTraitsForVFEESafe(Pawn pawn, int traitCount, PawnGenerationRequest? req = null, bool growthMomentTrait = false)
        {
            List<Trait> list = pawn.story.traits.allTraits;
            if (pawn.story != null)
            {
                int num = 0;
                while (list.Count < traitCount && ++num < traitCount + 500)
                {
                    IEnumerable<TraitDef> allDefsListForReading = DefDatabase<TraitDef>.AllDefsListForReading;
                    Func<TraitDef, float> func = (TraitDef tr) => tr.GetGenderSpecificCommonality(pawn.gender);
                    TraitDef newTraitDef = allDefsListForReading.RandomElementByWeight(func);
                    foreach (Trait t in list)
                    {
                        if (t.def == newTraitDef)
                        {
                            continue;
                        }
                    }
                    if ((newTraitDef != TraitDefOf.Gay || (!LovePartnerRelationUtility.HasAnyLovePartnerOfTheOppositeGender(pawn) && !LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn))) && (!growthMomentTrait || !ModsConfig.BiotechActive || newTraitDef.exclusionTags.Contains("SexualOrientation")))
				    {
                        if (req != null)
                        {
                            PawnGenerationRequest value = req.Value;
                            if (value.KindDef.disallowedTraits.NotNullAndContains(newTraitDef) || (value.KindDef.disallowedTraitsWithDegree != null && value.KindDef.disallowedTraitsWithDegree.Any((TraitRequirement t) => t.def == newTraitDef && t.degree == null)) || (value.KindDef.requiredWorkTags != WorkTags.None && (newTraitDef.disabledWorkTags & value.KindDef.requiredWorkTags) != WorkTags.None) || (newTraitDef == TraitDefOf.Gay && !value.AllowGay) || (value.ProhibitedTraits != null && value.ProhibitedTraits.Contains(newTraitDef)) || (value.Faction != null && Faction.OfPlayerSilentFail != null && (value.Faction.RelationWith(Faction.OfPlayer) == null || value.Faction.HostileTo(Faction.OfPlayer)) && !newTraitDef.allowOnHostileSpawn))
						    {
                                continue;
                            }
                        }
                        if (!pawn.story.traits.allTraits.Any((Trait tr) => newTraitDef.ConflictsWith(tr)) && (newTraitDef.requiredWorkTypes == null || !pawn.OneOfWorkTypesIsDisabled(newTraitDef.requiredWorkTypes)) && !pawn.WorkTagIsDisabled(newTraitDef.requiredWorkTags))
					    {
                            if (newTraitDef.forcedPassions != null && pawn.workSettings != null)
						    {
                                if (newTraitDef.forcedPassions.Any((SkillDef p) => p.IsDisabled(pawn.story.DisabledWorkTagsBackstoryTraitsAndGenes, pawn.GetDisabledWorkTypes(true))))
                                {
                                    continue;
                                }
                            }
                            int num2 = PawnGenerator.RandomTraitDegree(newTraitDef);
                            if ((pawn.story.Childhood == null || !pawn.story.Childhood.DisallowsTrait(newTraitDef, num2)) && (pawn.story.Adulthood == null || !pawn.story.Adulthood.DisallowsTrait(newTraitDef, num2)))
                            {
                                Trait trait = new Trait(newTraitDef, num2, false);
                                if ((pawn.kindDef.disallowedTraitsWithDegree.NullOrEmpty<TraitRequirement>() || !pawn.kindDef.disallowedTraitsWithDegree.Any((TraitRequirement t) => t.Matches(trait))) && (pawn.mindState == null || pawn.mindState.mentalBreaker == null || (pawn.mindState.mentalBreaker.BreakThresholdMinor + trait.OffsetOfStat(StatDefOf.MentalBreakThreshold)) * trait.MultiplierOfStat(StatDefOf.MentalBreakThreshold) <= 0.5f))
							    {
                                    list.Add(trait);
                                }
                            }
                        }
                    }
                }
                if (num >= traitCount + 500)
                {
                    Log.Warning(string.Format("Tried to generate {0} traits for {1} over {2} extra times and failed.", traitCount, pawn, 500));
                }
            }
			return list;
		}*/
        public static void DoBonusGrowthMoment(Pawn pawn)
        {
            //Hediff hediff = HediffMaker.MakeHediff(HVTDefOf.HVT_DoubleGrowthMoments, pawn, null);
            //pawn.health.AddHediff(hediff, null, null, null);
            pawn.ageTracker.TryChildGrowthMoment(pawn.ageTracker.AgeBiologicalYears, out int passionChoiceCount, out int num, out int num2);
            List<LifeStageWorkSettings> lifeStageWorkSettings = pawn.RaceProps.lifeStageWorkSettings;
            List<WorkTypeDef> tmpEnabledWorkTypes = new List<WorkTypeDef>();
            for (int i = 0; i < lifeStageWorkSettings.Count; i++)
            {
                if (lifeStageWorkSettings[i].minAge == pawn.ageTracker.AgeBiologicalYears)
                {
                    tmpEnabledWorkTypes.Add(lifeStageWorkSettings[i].workType);
                }
            }
            List<string> enabledWorkTypes = (from w in tmpEnabledWorkTypes
                                             select w.labelShort.CapitalizeFirst()).ToList<string>();
            ChoiceLetter_GrowthMoment choiceLetter_GrowthMoment = (ChoiceLetter_GrowthMoment)LetterMaker.MakeLetter(LetterDefOf.ChildBirthday);
            choiceLetter_GrowthMoment.ConfigureGrowthLetter(pawn, 0, num, 0, enabledWorkTypes, pawn.Name);
            choiceLetter_GrowthMoment.Label = ("HVT_BonusGrowthMoment".Translate(pawn.Name.ToStringShort));
            choiceLetter_GrowthMoment.StartTimeout(120000);
            pawn.ageTracker.canGainGrowthPoints = false;
            Find.LetterStack.ReceiveLetter(choiceLetter_GrowthMoment, null);
        }
    }
    [StaticConstructorOnStartup]
    public class HVTIcons
    {
        [MayRequireRoyalty]
        public static readonly Texture2D PulverizationBeam = ContentFinder<Texture2D>.Get("PsychicTraits/Abilities/HVT_PkPulverization", true);
    }
    public class HVT_Settings : ModSettings
    {
        public bool disableStealthRaids = false;
        public bool disableHardStealthRaids = true;
        public float pnfStartingCharges = 10;
        public float maxTranscendences = 2f;
        public float wokeGeneTransSuccessChance = 0.6f;
        public bool visibleTransEffect = true;
        public bool enableTranscendenceHints = true;
        public int MAX_TRAITS = -1;
        public float traitsMin = 1f;
        public float traitsMax = 3f;
        public bool genePuristsHateAliens = false;
        public bool monsterLoveForXenos = false;
        public bool monsterLoveForAliens = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableStealthRaids, "disableStealthRaids", false);
            Scribe_Values.Look(ref disableHardStealthRaids, "disableHardStealthRaids", true);
            Scribe_Values.Look(ref pnfStartingCharges, "pnfStartingCharges", 10f);
            Scribe_Values.Look(ref maxTranscendences, "maxTranscendences", 2f);
            Scribe_Values.Look(ref wokeGeneTransSuccessChance, "wokeGeneTransSuccessChance", 0.6f);
            Scribe_Values.Look(ref visibleTransEffect, "visibleTransEffect", true);
            Scribe_Values.Look(ref enableTranscendenceHints, "enableTranscendenceHints", true);
            Scribe_Values.Look(ref traitsMin, "traitsMin", 1);
            if (traitsMax < traitsMin)
            {
                traitsMax = traitsMin;
            }
            Scribe_Values.Look(ref traitsMax, "traitsMax", 1);
            Scribe_Values.Look(ref genePuristsHateAliens, "genePuristsHateAliens", false);
            Scribe_Values.Look(ref monsterLoveForXenos, "monsterLoveForXenos", false);
            Scribe_Values.Look(ref monsterLoveForAliens, "monsterLoveForAliens", false);
            base.ExposeData();
        }
    }
    public class HVT_Mod : Mod
    {
        public HVT_Mod(ModContentPack content) : base(content)
        {
            HVT_Mod.settings = GetSettings<HVT_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            //skulker event settings
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("HVT_SettingSkulkerEvents".Translate(), ref settings.disableStealthRaids, "HVT_TooltipSkulkerEvents".Translate());
            listingStandard.CheckboxLabeled("HVT_SettingSkulkerEvents2".Translate(), ref settings.disableHardStealthRaids, "HVT_TooltipSkulkerEvents2".Translate());
            if (ModsConfig.BiotechActive)
            {
                listingStandard.CheckboxLabeled("HVT_GenePuristsHAR".Translate(), ref settings.genePuristsHateAliens, "HVT_GenePuristsHARTooltip".Translate());
                if (ModsConfig.AnomalyActive)
                {
                    listingStandard.CheckboxLabeled("HVT_MonsterLoveXeno".Translate(), ref settings.monsterLoveForXenos, "HVT_MonsterLoveXenoTooltip".Translate());
                }
            }
            if (ModsConfig.AnomalyActive)
            {
                listingStandard.CheckboxLabeled("HVT_MonsterLoveHAR".Translate(), ref settings.monsterLoveForAliens, "HVT_MonsterLoveHARTooltip".Translate());
            }
            listingStandard.End();
            //traits-per-pawn settings
            if (settings.MAX_TRAITS == -1)
            {
                settings.MAX_TRAITS = DefDatabase<TraitDef>.AllDefsListForReading.Count();
            }
            displayMin = ((int)settings.traitsMin).ToString();
            displayMax = ((int)settings.traitsMax).ToString();
            float x = inRect.xMin, y = inRect.yMin+125, halfWidth = inRect.width * 0.5f;
            float orig = settings.traitsMin;
            Rect traitsMinRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.traitsMin = Widgets.HorizontalSlider(traitsMinRect, settings.traitsMin, 1f, 3f, true, "HVT_SettingMinTraits".Translate(), "1", "3", 1f);
            TooltipHandler.TipRegion(traitsMinRect.LeftPart(1f), "HVT_TooltipMinTraits".Translate());
            if (orig != settings.traitsMin)
            {
                displayMin = ((int)settings.traitsMin).ToString();
            }
            y += 32;
            string origString = displayMin;
            displayMin = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayMin);
            if (!displayMin.Equals(origString))
            {
                this.ParseInput(displayMin, settings.traitsMin, 3, out settings.traitsMin);
            }
            if (settings.traitsMin > settings.traitsMax)
            {
                settings.traitsMax = settings.traitsMin;
                displayMax = ((int)settings.traitsMax).ToString();
            }
            y -= 32;
            orig = settings.traitsMax;
            Rect traitsMaxRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.traitsMax = Widgets.HorizontalSlider(traitsMaxRect, settings.traitsMax, 3f, 9f, true, "HVT_SettingMaxTraits".Translate(), "3", "9", 1f);
            TooltipHandler.TipRegion(traitsMaxRect.LeftPart(1f), "HVT_TooltipMaxTraits".Translate());
            if (orig != settings.traitsMax)
            {
                displayMax = ((int)settings.traitsMax).ToString();
            }
            y += 32;
            origString = displayMax;
            displayMax = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayMax);
            if (!displayMax.Equals(origString))
            {
                this.ParseInput(displayMax, settings.traitsMax, 6, out settings.traitsMax);
            }
            if (settings.traitsMax < settings.traitsMin)
            {
                settings.traitsMin = settings.traitsMax;
                displayMin = ((int)settings.traitsMin).ToString();
            }
            y += 32;
            Rect pnfRect = new Rect(x + 10, y, halfWidth - 15, 32);
            displayPNF = ((int)settings.pnfStartingCharges).ToString();
            float origPNF = settings.pnfStartingCharges;
            settings.pnfStartingCharges = Widgets.HorizontalSlider(pnfRect, settings.pnfStartingCharges, 1f, 10f, true, "HVT_SettingPNFStartingCharges".Translate(), "1", "10", 1f);
            TooltipHandler.TipRegion(pnfRect.LeftPart(1f), "HVT_TooltipPNFStartingCharges".Translate());
            if (origPNF != settings.pnfStartingCharges)
            {
                displayPNF = ((int)settings.pnfStartingCharges).ToString();
            }
            y += 32;
            string origStringPNF = displayPNF;
            displayPNF = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayPNF);
            if (!displayPNF.Equals(origString))
            {
                this.ParseInput(displayPNF, settings.pnfStartingCharges, 10, out settings.pnfStartingCharges);
            }
            //transcendence settings
            y += 70;
            if (ModsConfig.RoyaltyActive)
            {
                displayTransMax = ((int)settings.maxTranscendences).ToString();
                displayWokeGeneChance = (settings.wokeGeneTransSuccessChance).ToString();
                float origR = settings.maxTranscendences;
                Rect transMaxRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.maxTranscendences = Widgets.HorizontalSlider(transMaxRect, settings.maxTranscendences, 1f, 5f, true, "HVT_SettingMaxTrans".Translate(), "1", "5", 1f);
                TooltipHandler.TipRegion(transMaxRect.LeftPart(1f), "HVT_TooltipMaxTrans".Translate());
                if (origR != settings.maxTranscendences)
                {
                    displayTransMax = ((int)settings.maxTranscendences).ToString();
                }
                y += 32;
                string origStringR = displayTransMax;
                displayTransMax = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayTransMax);
                if (!displayTransMax.Equals(origStringR))
                {
                    this.ParseInput(displayTransMax, settings.maxTranscendences, 5,out settings.maxTranscendences);
                }
                y += 35;
                Rect inRect2 = new Rect(x, y, inRect.width, 65);
                Listing_Standard l2 = new Listing_Standard();
                l2.Begin(inRect2);
                l2.CheckboxLabeled("HVT_SettingVisibleFXTrans".Translate(), ref settings.visibleTransEffect, "HVT_TooltipVisibleFXTrans".Translate());
                l2.CheckboxLabeled("HVT_SettingTransHints".Translate(), ref settings.enableTranscendenceHints, "HVT_TooltipTransHints".Translate());
                l2.End();
                y += 70;
                if (ModsConfig.BiotechActive)
                {
                    origR = settings.wokeGeneTransSuccessChance;
                    Rect wokeGeneRect = new Rect(x + 10, y, halfWidth - 15, 32);
                    settings.wokeGeneTransSuccessChance = Widgets.HorizontalSlider(wokeGeneRect, settings.wokeGeneTransSuccessChance, 0f, 1f, true, "HVT_SettingWokeGenes".Translate(), "0%", "100%");
                    TooltipHandler.TipRegion(wokeGeneRect.LeftPart(1f), "HVT_TooltipWokeGenes".Translate());
                    if (origR != settings.wokeGeneTransSuccessChance)
                    {
                        displayWokeGeneChance = ((int)settings.wokeGeneTransSuccessChance).ToString();
                    }
                    y += 32;
                    origStringR = displayWokeGeneChance;
                    displayWokeGeneChance = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayWokeGeneChance);
                    if (!displayWokeGeneChance.Equals(origStringR))
                    {
                        this.ParseInput(displayWokeGeneChance, settings.wokeGeneTransSuccessChance, 100, out settings.wokeGeneTransSuccessChance);
                    }
                }
            }
            base.DoSettingsWindowContents(inRect);
        }
        private void ParseInput(string buffer, float origValue, float maxValue, out float newValue)
        {
            if (!float.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < 0 || newValue > maxValue)
                newValue = origValue;
        }
        public override string SettingsCategory()
        {
            return "Hauts' Added Traits";
        }
        public static HVT_Settings settings;
        public string displayMin, displayMax, displayPNF, displayTransMax, displayWokeGeneChance;
    }
}