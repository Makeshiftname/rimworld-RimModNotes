using HautsFramework;
using HautsTraits;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace HautsTraitsRoyalty
{
    /*mastery awakening (skill or psylink level; legendary item handled via Harmony). This is a comp rather than a class so that the exact levels can be modified in XML*/
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (this.Pawn.IsHashIntervalTick(60, delta) && Current.ProgramState == ProgramState.Playing)
            {
                int gpl = this.Pawn.GetPsylinkLevel();
                if (gpl >= this.Props.psylinkLevel)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(this.Pawn, 1, true, "HVT_WokeHighPsyLevel".Translate(gpl, this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")), "HVT_WokeHighPsyLevelFantasy".Translate(gpl, this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")));
                }
                for (int i = 0; i < this.Pawn.skills.skills.Count; i++)
                {
                    if (this.Pawn.skills.skills[i].GetLevel() >= this.Props.skillLevel)
                    {
                        AwakeningMethodsUtility.AwakenPsychicTalentCheck(this.Pawn, 1, true, "HVT_WokeSkillLevel".Translate(this.Pawn.skills.skills[i].def.defName.ToLower(), this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), "HVT_WokeSkillLevelFantasy".Translate(this.Pawn.skills.skills[i].def.defName.ToLower(), this.Pawn.Name.ToStringShort, this.Pawn.gender.GetPossessive()).Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), false, 0f);
                        break;
                    }
                }
            }
        }
    }
    //love awakening (super-high opinion of others; other means handled via Harmony)
    public class Hediff_LPLoveMeter : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.didCheck = false;
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (!this.didCheck && this.pawn.relations != null && Current.ProgramState == ProgramState.Playing)
            {
                this.didCheck = true;
                foreach (DirectPawnRelation dpr in this.pawn.relations.DirectRelations)
                {
                    AwakeningMethodsUtility.LPLoveCheckRelations(dpr.def, this.pawn, dpr.otherPawn, PawnGenerator.IsBeingGenerated(this.pawn));
                }
            }
            if (this.Severity >= 500f && Current.ProgramState == ProgramState.Playing)
            {
                AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeSuperRelations".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeSuperRelationsFantasy".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), false, 0f);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.didCheck, "didCheck", true, false);
        }
        public bool didCheck = true;
    }
    //life awakening (life-threatening illness; resurrection. Other means handled via Harmony)
    public class Hediff_LPLifeMeter : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta) && Current.ProgramState == ProgramState.Playing)
            {
                foreach (Hediff h in this.pawn.health.hediffSet.hediffs)
                {
                    if (h.CurStage != null && h.CurStage.lifeThreatening && h.FullyImmune())
                    {
                        if (Rand.Value <= 0.2f)
                        {
                            AwakeningMethodsUtility.AwakenPsychicTalentCheck(this.pawn, 5, true, "HVT_WokeBeatIllness".Translate(h.Label, this.pawn.Name.ToStringShort, this.pawn.gender.GetPossessive()).Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeBeatIllnessFantasy".Translate(h.Label, this.pawn.Name.ToStringShort, this.pawn.gender.GetPossessive()).Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), false, 0f);
                            break;
                        }
                        else
                        {
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
                AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 5, true, "HVT_WokeResurrection".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_WokeResurrectionFantasy".Translate().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
            }
        }
    }
    //death awakening
    public class Hediff_LP6 : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            if (ResurrectionUtility.TryResurrect(pawnToRez))
            {
                AwakeningMethodsUtility.AwakenPsychicTalent(pawnToRez, true, "HVT_WokeDeath".Translate().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve(), "HVT_WokeDeathFantasy".Translate().Formatted(pawnToRez.Named("PAWN")).AdjustedFor(pawnToRez, "PAWN", true).Resolve());
            }
        }
    }
    //hediffs with this tag get removed on awakening. awakenChance is the chance that, when generated with a trait tagged with this, the pawn will awaken
    public class RemovedOnAwakening : DefModExtension
    {
        public RemovedOnAwakening()
        {

        }
        public float awakenChance = -1f;
    }
    public static class AwakeningMethodsUtility
    {
        //determines if a Love-type Latent Psychic should awaken from having a strong emotional bond with another pawn
        public static void LPLoveCheckRelations(PawnRelationDef prd, Pawn pawn, Pawn otherPawn, bool alreadyHappened = false)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (prd == PawnRelationDefOf.Bond && Rand.Value <= 0.5f)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeBond".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeBondFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), alreadyHappened);
                }
                else if (prd == PawnRelationDefOf.Lover && Rand.Value <= 0.5f)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeLover".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeLoverFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), alreadyHappened);
                }
                else if (prd == PawnRelationDefOf.Fiance && Rand.Value <= 0.75f)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeFiance".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeFianceFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), alreadyHappened);
                }
                else if (prd == PawnRelationDefOf.Spouse)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalentCheck(pawn, 3, true, "HVT_WokeSpouse".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), "HVT_WokeSpouseFantasy".Translate().Formatted(otherPawn.Named("OTHER"), pawn.Named("PAWN")).AdjustedFor(otherPawn, "OTHER", true).Resolve(), alreadyHappened);
                }
            }
        }
        //provided a list of pawns who were in the vicinity of a death, evaluates if each of the Loss-type Latent Psychics among them had a strong enough opinion of that pawn (and passes a chance check) to awaken from it
        public static void LPLossNonRelationDeathCheck(Pawn deceased, List<Pawn> recipients)
        {
            for (int i = 0; i < recipients.Count; i++)
            {
                Pawn recipient = recipients[i];
                if (recipient != deceased && recipient.story != null)
                {
                    if (Current.ProgramState == ProgramState.Playing && recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) && (deceased.RaceProps.Humanlike ? (recipient.relations.OpinionOf(deceased) >= 100 || (recipient.relations.OpinionOf(deceased) >= 60 && Rand.Chance(0.5f))) : (Rand.Chance(0.5f) && recipient.relations.DirectRelationExists(PawnRelationDefOf.Bond, deceased))))
                    {
                        string triggerEvent, triggerEventFantasy;
                        if (deceased.Name != null && deceased.Name.ToStringFull != null)
                        {
                            triggerEvent = "HVT_WokeNamedDeath".Translate().Formatted(deceased.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(deceased, "OTHER", true).Resolve();
                            triggerEventFantasy = "HVT_WokeNamedDeathFantasy".Translate().Formatted(deceased.Named("OTHER"), recipient.Named("PAWN")).AdjustedFor(deceased, "OTHER", true).Resolve();
                        }
                        else
                        {
                            triggerEvent = "HVT_WokeNamelessDeath".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                            triggerEventFantasy = "HVT_WokeNamelessDeathFantasy".Translate().Formatted(recipient.Named("PAWN")).AdjustedFor(recipient, "PAWN", true).Resolve();
                        }
                        AwakeningMethodsUtility.AwakenPsychicTalentCheck(recipient, 4, true, triggerEvent, triggerEventFantasy, false, 0.5f);
                    }
                    if (recipient.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon))
                    {
                        Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_DragonsHoard, recipient);
                        float victimsPsylinks = 66.67f;
                        float psyEnergy = 1f;
                        if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                        {
                            Hediff_Level psylink = (Hediff_Level)deceased.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                            if (psylink != null)
                            {
                                victimsPsylinks *= psylink.level;
                            }
                        }
                        else
                        {
                            victimsPsylinks *= deceased.GetPsylinkLevel();
                        }
                        if (deceased.story != null)
                        {
                            for (int j = 0; j < deceased.story.traits.allTraits.Count; j++)
                            {
                                if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(deceased.story.traits.allTraits[j].def))
                                {
                                    psyEnergy += 1f;
                                }
                                if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(deceased.story.traits.allTraits[j].def))
                                {
                                    psyEnergy += 3f;
                                }
                            }
                            if (deceased.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic))
                            {
                                psyEnergy += 0.5f;
                            }
                        }
                        if (ModsConfig.BiotechActive && deceased.genes != null)
                        {
                            for (int j = 0; j < deceased.genes.GenesListForReading.Count; j++)
                            {
                                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(deceased.genes.GenesListForReading[j].def))
                                {
                                    psyEnergy += 1f;
                                }
                            }
                        }
                        hediff.Severity += victimsPsylinks * psyEnergy * 10f;
                        if (hediff.Severity > 0f)
                        {
                            recipient.health.AddHediff(hediff, recipient.health.hediffSet.GetBrain());
                        }
                    }
                }
            }
        }
        /*determines if the target pawn should proceed to AwakenPsychicTalent (and also triggers Aptenodytes' man in black summoning). The pawn must have the Latent Psychic trait at the specified degree
         * any fields that have the same name as AwakenPsychicTalent fields are given to that method upon proceeding to it.
         * papillonCatalysisChance: currently unused, as the Papillon transcendence does not currently exist. It may however be brought back in the future if I ever decide to make more transes.*/
        public static void AwakenPsychicTalentCheck(Pawn pawn, int requisiteLatentPsychicDegree, bool shouldCure, string triggerEvent, string triggerEventFantasy, bool alreadyHappened = false, float papillonCatalysisChance = 0.5f)
        {
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitAptenodytes))
            {
                PsychicPowerUtility.ColonyHuddle(pawn);
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
                    TaggedString tef = triggerEventFantasy;
                    tef = tef.AdjustedFor(pawn, "PAWN", true).Resolve();
                    AwakenPsychicTalent(pawn, shouldCure, te, tef, alreadyHappened);
                } else {
                    AwakenPsychicTalent(pawn, shouldCure, "", "", alreadyHappened);
                }
            }
        }
        //either used to prevent a pawn of a 0x psysens mutant type from becoming woke or trans, or used to revert such a mutation before awakening or transing them
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
                                    }
                                    else
                                    {
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
        /*handles the actual process of awakening.
         * shouldCure: triggers PsychicPowerUtility.PsychicHeal
         * trigger strings: the fluff text in the letter you receive from the awakening
         * alreadyHappened: acts as if the pawn has had the awakening for some time already, so no awakening afterglow, no letter, and no psychic heal*/
        public static void AwakenPsychicTalent(Pawn pawn, bool shouldCure, string triggerEvent, string triggerEventFantasy, bool alreadyHappened = false)
        {
            if (AwakeningMethodsUtility.PsychicDeafMutantDeafInteraction(pawn))
            {
                return;
            }
            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitAptenodytes))
            {
                PsychicPowerUtility.ColonyHuddle(pawn);
                if (!pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitLeviathan))
                {
                    return;
                }
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
            foreach (TraitDef woke in PsychicTraitAndGeneCheckUtility.AwakenedTraitList())
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
                    explainTrait = ModCompatibilityUtility.IsHighFantasy() ? awakening.GetModExtension<SuperPsychicTrait>().descKeyFantasy.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : awakening.GetModExtension<SuperPsychicTrait>().descKey.Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                }
                pawn.story.traits.GainTrait(awakenTrait);
                if (!alreadyHappened)
                {
                    PsychicPowerUtility.PsychicHeal(pawn, shouldCure);
                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        TaggedString letterLabel;
                        letterLabel = ModCompatibilityUtility.IsHighFantasy() ? "HVT_TheWokeningFantasy".Translate(pawn.Name.ToStringShort) : "HVT_TheWokening".Translate(pawn.Name.ToStringShort);
                        TaggedString letterText;
                        if (triggerEvent != null)
                        {
                            letterText = ModCompatibilityUtility.IsHighFantasy() ? triggerEventFantasy : triggerEvent;
                        } else {
                            letterText = ModCompatibilityUtility.IsHighFantasy() ? "HVT_WokeningDefaultFantasy".Translate() : "HVT_WokeningDefault".Translate(pawn.Name.ToStringShort);
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
        private static readonly List<TraitDef> tmpAwakenings = new List<TraitDef>();
    }
}
