using HautsFramework;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace HautsTraits
{
    /*Everliving grants a bonus trait from the following list. It doesn't grant ALL of them due to balance concern,
     * and it's handled after a tick rather than instantly to stop TGS from aborting partway through as the trait collection gets modified while iterating thru it*/
    public class HediffCompProperties_CreepjoinerBonusTraitPool : HediffCompProperties_ForcedByOtherProperty
    {
        public HediffCompProperties_CreepjoinerBonusTraitPool()
        {
            this.compClass = typeof(HediffComp_CreepjoinerBonusTraitPool);
        }
        public List<BackstoryTrait> possibleTraits;
    }
    public class HediffComp_CreepjoinerBonusTraitPool : HediffComp_ForcedByOtherProperty
    {
        public new HediffCompProperties_CreepjoinerBonusTraitPool Props
        {
            get
            {
                return (HediffCompProperties_CreepjoinerBonusTraitPool)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!this.alreadyGrantedTrait)
            {
                this.alreadyGrantedTrait = true;
                if (!this.Props.possibleTraits.NullOrEmpty())
                {
                    Pawn_StoryTracker story = this.Pawn.story;
                    if (story != null)
                    {
                        BackstoryTrait bt = this.Props.possibleTraits.Where((BackstoryTrait bst) => !story.traits.HasTrait(bst.def) && !story.traits.allTraits.Any((Trait tr) => bst.def.ConflictsWith(tr))).RandomElement();
                        if (bt != null)
                        {
                            story.traits.GainTrait(new Trait(bt.def, bt.degree, true), false);
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.alreadyGrantedTrait, "alreadyGrantedTrait", true, false);
        }
        public bool alreadyGrantedTrait = false;
    }
    /*Compromised periodically checks for nearby pawns of the Horaxian Cult faction in LoS. If it finds any, there's a chance they switch sides and a red letter is sent.
     * They join the AI Lord of the "recruiter" if any; otherwise it's a 50/50 to attack the colony or try leaving the map.
     * Negated if blind and deaf.*/
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
        public string onFactionChangedLabel;
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta) && this.Pawn.Spawned && (this.Pawn.Faction == null || this.Pawn.Faction.def != this.Props.faction) && (this.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight) || this.Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)))
            {
                foreach (Pawn p in this.Pawn.Map.mapPawns.AllHumanlikeSpawned)
                {
                    if (p.Faction != null && p.Faction.def == this.Props.faction && p.Position.DistanceTo(this.Pawn.Position) <= this.Props.range && GenSight.LineOfSight(p.Position, this.Pawn.Position, this.Pawn.Map) && Rand.Chance(this.Props.chance))
                    {
                        if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                        {
                            Find.LetterStack.ReceiveLetter(this.Props.onFactionChangedLabel.Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Props.onFactionChangeMessage.Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), LetterDefOf.ThreatBig, this.Pawn);
                        }
                        this.Pawn.SetFaction(p.Faction, p);
                        if (p.lord != null)
                        {
                            if (this.Pawn.lord != null)
                            {
                                this.Pawn.lord.RemovePawn(this.Pawn);
                            }
                            p.lord.AddPawn(this.Pawn);
                        }
                        else if (Rand.Chance(0.5f))
                        {
                            if (this.Pawn.lord != null)
                            {
                                this.Pawn.lord.RemovePawn(this.Pawn);
                            }
                            Lord lord = LordMaker.MakeNewLord(this.Pawn.Faction, new LordJob_AssaultColony(this.Pawn.Faction, true, true, false, false, false, false, true), this.Pawn.Map, null);
                            lord.AddPawn(this.Pawn);
                            this.Pawn.mindState.duty.def = DutyDefOf.AssaultColony;
                        }
                        break;
                    }
                }
            }
        }
    }
    //Pathogenic MTB invokes a Framework method very similar to what Sickly does, except it strikes random pawns instead of just the pawn in question.
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
                return (HediffCompProperties_SuperSpreader)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(60, delta) && this.Pawn.Faction != null && this.Pawn.Faction == Faction.OfPlayerSilentFail && Rand.MTBEventOccurs(this.Props.mtbDays, 60000f, 60f))
            {
                HautsMiscUtility.DoRandomDiseaseOutbreak(this.Pawn);
            }
        }
    }
}
