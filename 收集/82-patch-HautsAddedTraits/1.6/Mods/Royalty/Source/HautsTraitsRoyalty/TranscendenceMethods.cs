using HautsFramework;
using HautsTraits;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    //controls various transcendence methods that can be handled via regular hediff functions (ticking, dying, being resurrected)
    public class Hediff_Woke : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.justResurrected = false;
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            this.justResurrected = false;
            if (this.pawn.IsHashIntervalTick(60000, delta) && !this.pawn.IsMutant)
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
                    TranscendenceMethodsUtility.AchieveTranscendence(this.pawn, "HVT_WokeningDefault".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeningDefaultFantasy".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), 0.25f, false, null, true, false, false);
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            if (TranscendenceMethodsUtility.ShouldTranscend(pawn) && pawnToRez.story != null && !pawnToRez.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
            {
                if (Rand.Value <= 0.05f)
                {
                    this.justResurrected = true;
                    if (ResurrectionUtility.TryResurrect(pawnToRez))
                    {
                        TranscendenceMethodsUtility.AchieveTranscendence(pawnToRez, "HVT_TransDeath".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_TransDeathFantasy".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), 0.2f, false, ignoreGeneChanceSetting: true);
                    }
                }
                else if (ModsConfig.IdeologyActive && this.pawn.Corpse != null && this.pawn.Corpse.Map != null)
                {
                    List<Thing> archonexi = this.pawn.Corpse.Map.listerThings.ThingsOfDef(ThingDefOf.ArchonexusCore);
                    if (archonexi.Count > 0)
                    {
                        if (ResurrectionUtility.TryResurrect(pawnToRez))
                        {
                            TranscendenceMethodsUtility.AchieveTranscendence(pawnToRez, "HVT_TransArchoDeath".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_TransArchoDeathFantasy".Translate().CapitalizeFirst().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), 1f, false, ignoreGeneChanceSetting: true);
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
                TranscendenceMethodsUtility.AchieveTranscendence(pawn, "HVT_TransResurrection".Translate().CapitalizeFirst(), "HVT_TransResurrection".Translate().CapitalizeFirst(), 0.5f);
            }
        }
        private bool justResurrected;
    }
    //controls the psyfocus spent transcendence condition, and is also responsible for sending the Revelation message
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(180, delta) && !this.hasSentTransClueMessage && HVT_Mod.settings.enableTranscendenceHints && this.Pawn.Faction != null && this.Pawn.Faction == Faction.OfPlayerSilentFail && this.Pawn.story != null && !PsychicTraitAndGeneCheckUtility.IsTranscendent(this.Pawn))
            {
                if (PsychicTraitAndGeneCheckUtility.IsTranscendent(this.Pawn))
                {
                    this.hasSentTransClueMessage = true;
                }
                else
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
            }
            Pawn_PsychicEntropyTracker ppet = this.Pawn.psychicEntropy;
            if (ppet != null && ppet.IsCurrentlyMeditating)
            {
                this.StartNewTimer();
            }
            this.ticksToNextReset -= delta;
            if (this.ticksToNextReset <= 0)
            {
                this.parent.Severity = this.parent.def.minSeverity;
                return;
            }
            if (ppet != null)
            {
                if (ppet.CurrentPsyfocus != this.curPsyfocus)
                {
                    this.parent.Severity += Math.Max(0f, ppet.CurrentPsyfocus - this.curPsyfocus) * this.Props.severityPerPsyfocus.RandomInRange;
                    this.curPsyfocus = ppet.CurrentPsyfocus;
                }
            }
            if (this.parent.Severity == this.parent.def.maxSeverity)
            {
                this.ticksToNextReset = 0;
                this.parent.Severity = this.parent.def.minSeverity;
                TranscendenceMethodsUtility.AchieveTranscendence(this.Pawn, "HVT_TransPsyfocuser".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), "HVT_TransPsyfocuserFantasy".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), 1f);
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
    //apocriton shockwave exposure can transcend
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
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p) && p.Position.DistanceTo(this.parent.PositionHeld) <= this.Props.radius)
                    {
                        TranscendenceMethodsUtility.AchieveTranscendence(p, "HVT_TransApocritonBeow".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), "HVT_TransApocriton".Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve(), 1f);
                    }
                }
            }
        }
    }
    /*deprecated transcendence method: it used to be that gaining archite genes would add a hidden hediff to woke pawns which eventually transed them. this was rolled into the "MTB longass time" method, which now
     has a lower MTB the more archite genes your current genome contains, in the update in which Revelations were added. this therefore only exists for save file compatibility*/
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(60, delta) && Rand.MTBEventOccurs(this.Props.MTBDays, 60000f, 60f))
            {
                TranscendenceMethodsUtility.AchieveTranscendence(this.parent.pawn, "HVT_TransArchogeneDelay".Translate(this.parent.pawn.Name.ToStringFull), "HVT_TransArchogeneDelayFantasy".Translate(this.parent.pawn.Name.ToStringFull), 0.55f);
                this.parent.Severity -= 1;
            }
        }
    }
    public static class TranscendenceMethodsUtility
    {
        //transcendence methods
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
        public static bool ShouldTranscend(Pawn pawn, bool ignoreGeneChanceSetting = false)
        {
            if (pawn.story != null && (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn, false) || (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn) && (ignoreGeneChanceSetting || Rand.Chance(HVT_Mod.settings.wokeGeneTransSuccessChance)))))
            {
                float maxTranses = MaxTransesForPawn(pawn);
                int currentTransCount = 0;
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
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
        public static void AchieveTranscendence(Pawn pawn, string liminalEvent, string liminalEventFantasy, float additionalLevelChance, bool alreadyHappened = false, List<TraitDef> forbiddenTranses = null, bool sendLetter = true, bool mythsOnly = false, bool canRandomlyGetMythic = true, bool ignoreGeneChanceSetting = false)
        {
            if (TranscendenceMethodsUtility.ShouldTranscend(pawn))
            {
                TranscendenceMethodsUtility.AchieveTranscendenceDirect(pawn, liminalEvent, liminalEventFantasy, additionalLevelChance, alreadyHappened, forbiddenTranses, sendLetter, mythsOnly, canRandomlyGetMythic);
            }
        }
        public static void AchieveTranscendenceDirect(Pawn pawn, string liminalEvent, string liminalEventFantasy, float additionalLevelChance, bool alreadyHappened = false, List<TraitDef> forbiddenTranses = null, bool sendLetter = true, bool mythsOnly = false, bool canRandomlyGetMythic = true)
        {
            if (AwakeningMethodsUtility.PsychicDeafMutantDeafInteraction(pawn))
            {
                return;
            }
            tempTranses.Clear();
            List<TraitDef> transferTranses = new List<TraitDef>();
            if (mythsOnly)
            {
                PopulateEligibleTransList(transferTranses, PsychicTraitAndGeneCheckUtility.MythicTranscendentTraitList(), pawn);
            }
            else
            {
                PopulateEligibleTransList(transferTranses, PsychicTraitAndGeneCheckUtility.RegularTranscendentTraitList(), pawn);
                if (canRandomlyGetMythic && Rand.Value <= 0.12f)
                {
                    PopulateEligibleTransList(transferTranses, PsychicTraitAndGeneCheckUtility.MythicTranscendentTraitList(), pawn);
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
                    explainTrait = ModCompatibilityUtility.IsHighFantasy() ? transcendent.def.GetModExtension<SuperPsychicTrait>().descKeyFantasy.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : transcendent.def.GetModExtension<SuperPsychicTrait>().descKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
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
                            letterText = ModCompatibilityUtility.IsHighFantasy() ? liminalEventFantasy : liminalEvent;
                        }
                        else
                        {
                            letterText = "HVT_TransDefault".Translate();
                        }
                        letterText += ModCompatibilityUtility.IsHighFantasy() ? (" " + "HVT_TransSuffixFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() + " \n\n" + explainTrait) : (" " + "HVT_TransSuffix".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() + " \n\n" + explainTrait);
                        letterText = letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                        LookTargets toLook = new LookTargets(pawn);
                        ChoiceLetter awakenLetter = LetterMaker.MakeLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, toLook, null, null, null);
                        Find.LetterStack.ReceiveLetter(awakenLetter, null);
                    }
                }
            }
            else
            {
                Log.Error("HVT_CantGiveTrans".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
            if (pawn.psychicEntropy != null)
            {
                pawn.psychicEntropy.RechargePsyfocus();
            }
        }
        private static readonly List<TraitDef> tempTranses = new List<TraitDef>();
    }
}
