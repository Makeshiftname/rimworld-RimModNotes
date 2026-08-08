using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsTraits
{

    [StaticConstructorOnStartup]
    public class HVTIcons
    {
        [MayRequireRoyalty]
        public static readonly Texture2D PulverizationBeam = ContentFinder<Texture2D>.Get("PsychicTraits/Abilities/HVT_PkPulverization", true);
    }
    public static class HVTUtility
    {
        //determine if there is a proximate wild or aggro-mental-state animal. Used for Agrizoophobe's mood malus and random mental breaking
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
        //handles Curmudgeons losing mood, and opinion of the other party, on social interaction (provided that the other party has at least 65% mood)
        public static void DoCurmudgeonPenalties(Pawn curmudgeon, Pawn other)
        {
            if (curmudgeon.needs.mood != null && other.needs.mood != null && other.needs.mood.CurLevel >= 0.65f)
            {
                curmudgeon.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_CurmudgeonlyMood, null);
                if (RelationsUtility.PawnsKnowEachOther(curmudgeon, other))
                {
                    curmudgeon.needs.mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_CurmudgeonlyDislike, other);
                }
            }
        }
        //grants mood and possibly an inspiration to the recipient (the Sadistic pawn). The other pawn (which should be the pawn who mentally broke down) gains an opinion malus of the Sadistic, but obviously only if they know the Sadistic.
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
        //a Harmony patch uses this to determine if a thing (or one of its ingredients) is of the Fish category, which determines whether Pescatarians get a mood bonus for eating the thing
        public static bool IsThisFoodFish(ThingDef foodDef)
        {
            if (!foodDef.thingCategories.NullOrEmpty() && foodDef.thingCategories.Contains(ThingCategoryDefOf.Fish))
            {
                return true;
            }
            return false;
        }
        //gravships and transporters (e.g. drop pods or Vehicle Framework's flying vehicles) invoke this to make Skybound pawns happy and Earthborne pawns upset and grav-nauseous. Can also be invoked elsewhere
        public static void DoAerospaceFlyingThoughts(Pawn pawn)
        {
            if (pawn.story != null)
            {
                Need_Mood mood = pawn.needs.mood;
                if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Earthborne))
                {
                    Pawn_HealthTracker health = pawn.health;
                    if (health != null)
                    {
                        health.AddHediff(HediffDefOf.GravNausea, null, null, null);
                    }
                    if (mood != null)
                    {
                        mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_IHateFlying);
                    }
                }
                else if (pawn.story.traits.HasTrait(HVTDefOf.HVT_Skybound))
                {
                    if (mood != null)
                    {
                        mood.thoughts.memories.TryGainMemory(HVTDefOf.HVT_ILoveFlying);
                    }
                }
            }
        }
        //ewisott
        public static bool HasASkulker(Caravan caravan)
        {
            foreach (Pawn p in caravan.PawnsListForReading)
            {
                if (p.Faction == Faction.OfPlayerSilentFail && p.story != null && p.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    return true;
                }
            }
            return false;
        }
        /*determines the chance of success when using Skulkers on your caravan to check for hidden or threat-requiring parts on a site being visited.
         * Sight, Moving, and to a lesser extent Hearing all play a role. As the caravan arrival action multiplies this result by 0.4x, having >=100% in all these capacities guarantees success*/
        public static float ScoutingSkulkerPower(Caravan caravan)
        {
            float totalSkulkerPower = 0f;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn.Faction == caravan.Faction && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    totalSkulkerPower += (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) + (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) / 2)) * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
                }
            }
            return totalSkulkerPower;
        }
        //float menu option for aforementioned site-checking mechanic.
        public static FloatMenuOption GoScoutForAmbushes(Caravan caravan, Site site)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = CaravanArrivalAction_VisitSite.CanVisit(caravan, site);
            if (floatMenuAcceptanceReport.Accepted || !floatMenuAcceptanceReport.FailReason.NullOrEmpty() || !floatMenuAcceptanceReport.FailMessage.NullOrEmpty())
            {
                if (!floatMenuAcceptanceReport.FailReason.NullOrEmpty())
                {
                    return new FloatMenuOption("HVT_SFAIcon".Translate() + " (" + floatMenuAcceptanceReport.FailReason + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                }
                else
                {
                    Action action = delegate
                    {
                        if (floatMenuAcceptanceReport.Accepted)
                        {
                            caravan.pather.StartPath(site.Tile, new CaravanArrivalAction_ScoutForAmbushes(site), true, true);
                            return;
                        }
                        if (!floatMenuAcceptanceReport.FailMessage.NullOrEmpty())
                        {
                            Messages.Message(floatMenuAcceptanceReport.FailMessage, site, MessageTypeDefOf.RejectInput, false);
                        }
                    };
                    return new FloatMenuOption("HVT_SFALabel".Translate(Math.Min(100f, HVTUtility.ScoutingSkulkerPower(caravan) * 100f)), action, MenuOptionPriority.Default, null, null, 0f, null, site, true, 0);
                }
            }
            return new FloatMenuOption("HVT_SFAIcon".Translate() + " (x)", null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
        }
        /*generates a letter very akin to a regular growth moment. The crucial difference: it does not grant any passions, because if a pawn has so many passions that they can't select as many passion improvements
         * as the Growth Moment letter wants them to select, it won't let them finalize their decisions. This is almost never a problem in conventional play (or if you have Vanilla Skills Ex),
         * but it can be a problem in this instance. This method is invoked on birthdays 6, 9, and 13 (the regular growth moments) if you have the Max Traits mod setting set to 6 or 9, since
         * those just "stack" growth moments on the same birthdays instead of inducing a different spread over the 4-13 age range. If this granted the normal number of passions, a pawn with 9
         * growth moments would run out of passion improvements to take and be unable to complete their last Growth Moment.*/
        public static void DoBonusGrowthMoment(Pawn pawn)
        {
            if (pawn.story == null)
            {
                return;
            }
            if (Faction.OfPlayer == null || pawn.Faction != Faction.OfPlayer)
            {
                SkillDef sd = ChoiceLetter_GrowthMoment.PassionOptions(pawn, 2, true).FirstOrFallback(null);
                if (sd != null && pawn.skills != null)
                {
                    SkillRecord skill = pawn.skills.GetSkill(sd);
                    if (skill != null)
                    {
                        skill.passion = skill.passion.IncrementPassion();
                    }
                }
                Trait t = PawnGenerator.GenerateTraitsFor(pawn, 1, null, true).FirstOrFallback(null);
                if (t != null)
                {
                    pawn.story.traits.GainTrait(t);
                    TraitUtility.ApplySkillGainFromTrait(pawn, t);
                }
                return;
            }
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
}
