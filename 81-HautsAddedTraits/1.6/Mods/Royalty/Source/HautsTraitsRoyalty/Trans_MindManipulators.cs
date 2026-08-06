using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HautsTraitsRoyalty
{
    /*Apocritons have a psycast-triggered ExtraOnHitEffect that inflicts a 'control' hediff on non-ultraheavy mechs or Odyssey drones. Each MechWeightClassDef has its own chance to be affected (but ultraheavies and boss pawnkinddefs are immune).
     * Control turns the pawn to the Apocriton's faction and makes it glow bright blue for an hour, then it returns to its old faction (as stored in the control hediff).
     * In case a pawn has a mechweightclassdef that isn't in the chancesPerWeight dictionary, the chance to be affected is the fallbackWeight.*/
    public class HediffCompProperties_TechnopathicControl : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_TechnopathicControl()
        {
            this.compClass = typeof(HediffComp_TechnopathicControl);
        }
        public Dictionary<MechWeightClassDef, float> chancesPerWeight;
        public HediffDef hediff;
        public float baseSeverity = 1f;
        public float fallbackWeight;
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
            if (victim.RaceProps.IsDrone)
            {
                return 1f;
            }
            else
            {
                MechWeightClassDef mw = victim.RaceProps.mechWeightClass;
                if (mw != null && this.Props.chancesPerWeight.ContainsKey(mw))
                {
                    return this.Props.chancesPerWeight.TryGetValue(mw);
                }
            }
            return this.Props.fallbackWeight;
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if ((victim.RaceProps.IsMechanoid || victim.RaceProps.IsDrone) && !victim.kindDef.isBoss)
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
                    }
                    else
                    {
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
    /*this isn't for Psychic Apocritons themselves, but rather their victims.
     * Ultraheavy mechs are unaffected, even if the control hediff is applied to them.
     * This assigns the mechs to defend the Apocriton's position at time of control, or if the mech is now of a player-hostile faction, tells it to go a-raiding. These Lords are removed on resurrection or the end of the control.
     * If they'd be restored to a player-hostile faction, then they go a-raiding.*/
    public class Hediff_ApocritonControl : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.originalFaction = this.pawn.Faction;
            if (this.pawn.RaceProps.mechWeightClass != MechWeightClassDefOf.UltraHeavy)
            {
                if (this.newFaction != null && (this.pawn.RaceProps.IsMechanoid || this.pawn.RaceProps.IsDrone) && this.pawn.Spawned)
                {
                    this.pawn.SetFaction(this.newFaction);
                    this.pawn.jobs.StopAll(false, true);
                    if (this.originalCaster != null)
                    {
                        LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_DefendPoint(this.pawn.Position), this.pawn.Map, Gen.YieldSingle<Pawn>(this.pawn));
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
            if (this.pawn.lord != null)
            {
                this.pawn.lord.RemovePawn(this.pawn);
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (ModsConfig.BiotechActive && this.pawn.GetOverseer() != null)
            {
                if (this.pawn.lord != null)
                {
                    this.pawn.lord.RemovePawn(this.pawn);
                }
                return;
            }
            if (!this.pawn.Dead)
            {
                if (this.pawn.RaceProps.IsDrone)
                {
                    this.pawn.Kill(null);
                    return;
                }
                this.pawn.SetFaction(this.originalFaction);
                if (this.pawn.jobs != null)
                {
                    this.pawn.jobs.StopAll(false, true);
                }
                if (this.pawn.Faction != Faction.OfPlayer)
                {
                    if (this.pawn.HostileTo(Faction.OfPlayer))
                    {
                        LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction, true, true, false, false, true, false, false), pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                    }
                }
                else if (this.pawn.lord != null)
                {
                    this.pawn.lord.RemovePawn(this.pawn);
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
    /*every day, Budgies grant goodwill with all factions you could gain goodwill with. Magnitude scales w/ psylink level, up to a max of +2 at level 6 (or assuming no level cap e.g. VPE, +12 at 36).
     * Per user request, you can disable this functionality. I don't want it to be totally useless if you disable it, though, so if you toggle it off the periodic effect is instead skill xp and a small chance for inspiration.*/
    public class Hediff_Budgie : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(60000, delta) && this.pawn.Faction != null && this.pawn.Faction == Faction.OfPlayerSilentFail)
            {
                if (this.gainGoodwill)
                {
                    foreach (Faction f in Find.FactionManager.AllFactionsVisible)
                    {
                        if (f != this.pawn.Faction && f.def.humanlikeFaction && !f.def.PermanentlyHostileTo(FactionDefOf.PlayerColony) && f.HasGoodwill)
                        {
                            Faction.OfPlayerSilentFail.TryAffectGoodwillWith(f, (int)Math.Ceiling((double)Math.Min(12, this.pawn.GetPsylinkLevel()) / 3), false, true, HVTRoyaltyDefOf.HVT_TransDiplomacy, null);
                        }
                    }
                } else {
                    if (this.pawn.mindState.inspirationHandler != null && !this.pawn.Inspired && Rand.Chance(0.25f))
                    {
                        TaggedString reasonText = "HVT_BudgieInspired".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve();
                        InspirationDef randomAvailableInspirationDef = this.pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                        if (randomAvailableInspirationDef != null)
                        {
                            this.pawn.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, reasonText, true);
                        }
                    }
                    if (this.pawn.skills != null)
                    {
                        int improvements = 3;
                        foreach (SkillRecord sr in this.pawn.skills.skills.InRandomOrder())
                        {
                            if (!sr.TotallyDisabled)
                            {
                                sr.Learn(500*Math.Min(20,this.pawn.GetPsylinkLevel()),true,true);
                                improvements--;
                                if (improvements == 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.uiIcon == null)
            {
                this.uiIcon = ContentFinder<Texture2D>.Get("Things/Mote/PsycastSkipFlash", true);
            }
            Command_Action cmdRecall = new Command_Action
            {
                defaultLabel = "HVT_Budgie_ToggleLabel".Translate().Resolve(),
                defaultDesc = (!this.gainGoodwill ? "HVT_Budgie_ToggleTooltipOn" : "HVT_Budgie_ToggleTooltipOff").Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(),
                icon = this.uiIcon,
                action = delegate ()
                {
                    this.gainGoodwill = !this.gainGoodwill;
                }
            };
            yield return cmdRecall;
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.gainGoodwill, "gainGoodwill", true, false);
        }
        Texture2D uiIcon;
        public bool gainGoodwill = true;
    }
    //pawns gain a psysens-scaling mood opinion of Cuckoos. The last time I checked, this also for some reason scales off the Cuckoo's psychic sensitivity. I have no idea why that is the case, but it's fine.
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
    //Echinoids' pain aura has an indiscriminate effect, and a hostile-only effect. The former is handled by the normal aura parameters; hostiles are given hediffFoe
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
                }
                else
                {
                    pawn.health.AddHediff(this.Props.hediffFoe, null);
                }
            }
        }
    }
    //Meerkats are mostly handled by existing imperative code, but we also want pawns generated with the trait as having been under its effect for some time, so they gain extra skill levels
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
    //The Peacock aura slowly fills the beauty need of whomever it affects
    public class Hediff_Resplendence : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.needs.beauty != null && !this.pawn.Suspended && (!this.pawn.needs.beauty.def.freezeWhileSleeping || this.pawn.Awake()) && (!this.pawn.needs.beauty.def.freezeInMentalState || !this.pawn.InMentalState) && (this.pawn.SpawnedOrAnyParentSpawned || this.pawn.IsCaravanMember() || PawnUtility.IsTravelingInTransportPodWorldObject(this.pawn)))
            {
                this.pawn.needs.beauty.CurLevel += 0.00025f * delta;
            }
        }
    }
    /*Above minGoodSeverity, the Ptarmigan aura applies hediffGood to non-hostile pawns. Below maxBadSeverity, it applies hediffBad to hostile pawns. These are not the same value to leave room for a gap between them where the aura does nothing.
     * Aura's severity is mediated by BreakRiskSeverity, a different comp which is from the Framework.*/
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
            }
            else if (this.parent.Severity <= this.Props.maxBadSeverity && this.parent.pawn.HostileTo(pawn))
            {
                Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffBad, pawn, null);
                pawn.health.AddHediff(hediff, null);
            }
        }
    }
    /*Psychic Spelopedes generate hives near their current location by spending enough psyfocus.
     * They also emanate an aura that stops hostile insectoids' jobs, stops any aggro mental state they're in, tells them to wander around where they are now, and turns them wild. This will stop them from maintaining their hives.*/
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
                    LordMaker.MakeNewLord(this.Pawn.Faction, new LordJob_DefendPoint(pawn.Position, 12f, 4f), this.Pawn.Map, Gen.YieldSingle<Pawn>(pawn));
                }
            }
        }
    }
    /*Strixes have a foe-affecting, psycast-inflicted MentalStateOnHit derivative (itself a derivative of ExtraOnHitEffects) that only has a chance to work.
     * Chance is based on some parent properties set in XML (25% times victim's psysens), offset by [chancePerVictimPsylink * victim's psylink level]. As chancePerVictimPsylink is negative, this makes
     * psycasters more resistant. However, the chance can't go below the chanceMinimum.*/
    public class HediffCompProperties_ThoughtsInChaos : HediffCompProperties_MentalStateOnHit
    {
        public HediffCompProperties_ThoughtsInChaos()
        {
            this.compClass = typeof(HediffComp_ThoughtsInChaos);
        }
        public float chancePerVictimPsylink;
        public float chanceMinimum;
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
            return Math.Max(this.Props.chanceMinimum, Math.Min(1f, base.ChanceForVictim(victim)) + (victim.GetPsylinkLevel() * this.Props.chancePerVictimPsylink));
        }
    }
    /*Whaleheads (the original Vultures, before I decided to use that name for a corpse-generator transcendence instead) have an aura with two big effects. ANY hostile pawn on the same map gets hit by a hefty moodlet (mapwideThought).
     * Secondly, foes actually IN the aura have a chance (see baseFleeChance, fleeChanceScalar which is psysens as usual, and maxFleeChance) every second (as set by tickPeriodicity) to start fleeing.
     * fleeRange: the Flee job will go up to a random value in this range away
     * fleeDuration: it'll last for at least this long
     * iconPath: affected pawns show a thought bubble containing this icon, so you get immediate feedback when they're running for Spooky Scary Supernatural reasons and not just because their pather took a couple lessons from the StarCraft 1 dragoon.*/
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
                if (pawn.HostileTo(p) && pawn.needs.mood != null)
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
            if (Rand.Chance(Math.Min(this.Props.maxFleeChance, this.Props.baseFleeChance * pawn.GetStatValue(this.Props.fleeChanceScalar))))
            {
                Job job = FleeUtility.FleeJob(pawn, self, this.Props.fleeRange.RandomInRange);
                job.expiryInterval = this.Props.fleeDuration.RandomInRange;
                job.mote = MoteMaker.MakeThoughtBubble(pawn, this.Props.iconPath, true);
                RestUtility.WakeUp(pawn, true);
                pawn.jobs.StopAll(false, true);
                pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, false, true, null, null, false, false, null, false, true, false);
            }
        }
    }
    /*Sirens can set targeted pawns to their own faction. No save, duration instantaneous
     * setsIdeo: also makes the target a believer in the Siren's ideo
     * letterLabel|Text: you receive a letter with these contents if the ability either moved a pawn into or out of your faction.
     * failsOnAwokens|Trans: ewisott
     * AiScansForTargets derivative, looks for psysens non-woke pawns with a market value >= aiMinMarketValueToTarget.*/
    public class CompProperties_AbilityMindControl : CompProperties_AbilityAiScansForTargets
    {
        public bool setsIdeo = true;
        public bool failsOnAwokens;
        public bool failsOnTrans;
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
                    if (this.Props.failsOnAwokens && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                    {
                        return false;
                    }
                    if (this.Props.failsOnTrans && PsychicTraitAndGeneCheckUtility.IsTranscendent(pawn))
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
                        if (ModCompatibilityUtility.IsHighFantasy())
                        {
                            Messages.Message("HVT_WontTargetSameFactionFantasy".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                        else
                        {
                            Messages.Message("HVT_WontTargetSameFaction".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    return false;
                }
                if (pawn.story != null)
                {
                    if (this.Props.failsOnAwokens && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                    {
                        if (throwMessages)
                        {
                            Messages.Message("HVT_WontTargetAwakened".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                        }
                        return false;
                    }
                    if (this.Props.failsOnTrans && PsychicTraitAndGeneCheckUtility.IsTranscendent(pawn))
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
                    if (this.Props.failsOnAwokens && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                    {
                        return;
                    }
                    if (this.Props.failsOnTrans && PsychicTraitAndGeneCheckUtility.IsTranscendent(pawn))
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
                }
                else if (pawnFaction == Faction.OfPlayerSilentFail)
                {
                    letterDef = LetterDefOf.NegativeEvent;
                }
                else
                {
                    return;
                }
                ChoiceLetter notification = LetterMaker.MakeLetter(
                this.Props.letterLabel.Formatted(this.parent.pawn.Named("INITIATOR"), pawn.Named("RECIPIENT")), this.Props.letterText.Formatted(this.parent.pawn.Named("INITIATOR"), pawn.Named("RECIPIENT")), letterDef, new LookTargets(pawn), null, null, null);
                Find.LetterStack.ReceiveLetter(notification, null);
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return base.AICanTargetNow(target) && target.Pawn != null && (target.Pawn.story == null || !PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(target.Pawn)) && target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && target.Pawn.MarketValue >= this.Props.aiMinMarketValueToTarget;
        }
    }
}
