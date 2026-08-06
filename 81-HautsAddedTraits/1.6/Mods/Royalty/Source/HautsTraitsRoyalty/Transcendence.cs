using HautsFramework;
using HautsTraits;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI.Group;

namespace HautsTraitsRoyalty
{
    //transcendent warble. debating whether i should make it a per-pawn toggle, like auras, instead of a mod setting
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(2500, delta) && this.Props.mustBeTranscendent && !PsychicTraitAndGeneCheckUtility.IsTranscendent(this.Pawn))
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    //the buff all transcendents have (2x consciousness, warble, etc.) also handles Anomaly mutant reversion, and instantly adds itself back if removed from a trans pawn
    public class Hediff_TransEffect : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(250, delta) && ModsConfig.AnomalyActive && this.pawn.IsMutant && !this.pawn.Downed)
            {
                Pawn_MutantTracker pmt = this.pawn.mutant;
                MutantDef md = pmt.Def;
                BecomeEvilIfRevertedByTrans beirbt = md.GetModExtension<BecomeEvilIfRevertedByTrans>();
                if (beirbt == null || beirbt.canRevert)
                {
                    pmt.GetType().GetField("hasTurned", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pmt, true);
                    Messages.Message("HVT_ImmuneToGhoulizing".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, false);
                    this.pawn.mutant.Revert();
                    if (beirbt != null && Faction.OfHoraxCult != null)
                    {
                        this.pawn.SetFaction(Faction.OfHoraxCult);
                        if (this.pawn.Spawned)
                        {
                            List<Pawn> thisPawn = new List<Pawn>();
                            thisPawn.Add(this.pawn);
                            Lord lord = LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_AssaultColony(this.pawn.Faction, true, true, false, false, true, false, false), this.pawn.Map, null);
                            lord.AddPawns(thisPawn, true);
                        }
                        if (this.pawn.guest != null && Rand.Chance(beirbt.unwaveringLoyaltyChance))
                        {
                            this.pawn.guest.Recruitable = false;
                        }
                    }
                }
                /*mutants usually lose their psylinks. Woke and trans traits are supposed to be for psycasters, and there are probably some edge cases I overlooked where a trans assumes you have a psylink, so you get one back*/
                if (!this.pawn.HasPsylink)
                {
                    PawnUtility.ChangePsylinkLevel(this.pawn, 1, false);
                }
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.pawn.story != null && PsychicTraitAndGeneCheckUtility.IsTranscendent(this.pawn))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.def, this.pawn, null);
                float newSev = 0f;
                foreach (Trait t in this.pawn.story.traits.allTraits)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
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
    /*transcendence reverts mutation to avoid headaches with mutant psydeafness and psylinklessness. this leads to various cheesy resurrections with certain mutant types (e.g. deadlife dusting a trans corpse)
     Apply this to a mutant def to make it ALWAYS be hostile on coming back, so there's at least some challenge to the cheese*/
    public class BecomeEvilIfRevertedByTrans : DefModExtension
    {
        public BecomeEvilIfRevertedByTrans()
        {

        }
        public float unwaveringLoyaltyChance = 0.5f;
        public bool canRevert = true;
    }
}
