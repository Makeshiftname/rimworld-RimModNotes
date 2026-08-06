using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsTraitsRoyalty
{
    //Anacondas have a set of stat boosts that grows over time, passively, forever. To simulate this for pawns generated with the Psychic Anaconda trait, they gain a random amount of the buff, up to an amount they could reasonably have due to their lifespan
    public class Hediff_IndeterminateGrowth : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (PawnGenerator.IsBeingGenerated(this.pawn))
            {
                this.Severity = Rand.Value * 4f * (1f + Math.Max(1f, this.pawn.ageTracker.AgeBiologicalYears - 10f));
            }
        }
    }
    /*On gaining the Fossilized Psychic trait, you recover from all injuries and missing parts. You then replace all your exterior non-artificial body parts with Fossilized parts, which have a high flat part efficiency.
     * Every hour, fossilization takes place again (not the full heal thing, but the organic body part replacement thing).*/
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
                }
                else if (hediffs[i] is Hediff_MissingPart hmp)
                {
                    if (this.pawn.Spawned)
                    {
                        MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(this.pawn, hediffs[i].Part, this.pawn.Position, this.pawn.Map);
                    }
                    else
                    {
                        this.pawn.health.RestorePart(hediffs[i].Part);
                    }
                }
            }
            this.Fossilize();
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta))
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
                if (bpr.depth == BodyPartDepth.Outside && pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null).Contains(bpr) && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(bpr) && !pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_THediffFossilPart, bpr))
                {
                    this.pawn.health.AddHediff(HVTRoyaltyDefOf.HVT_THediffFossilPart, bpr);
                }
            }
        }
    }
    //Every 24 hours, Goldfinches gain a mood boost, repair all their apparel and equipped weapons, experiences a lot of injury healing, and also becomes invulnerable to the next instance of damage taken (a consequence of reaching 1 severity in this hediff)
    public class Hediff_DawnChorus : Hediff_PreDamageModification
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(60000, delta))
            {
                PsychicPowerUtility.PsychicHeal(this.pawn, true);
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
    /*Keas gain xp in all skills from psycasting and from meditating. If you have RimLanguages, psycasting and meditating also grant progress towards learning a random language.
     * As with Anaconda, a pawn who is generated with this trait logically has had it for some time, so we simulate an arbitrary amount of benefit accrued from their logically extant past.*/
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
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(20, delta))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    PsychicPowerUtility.KeaOffsetPsyfocusLearning(this.pawn, 0.0004f * this.pawn.GetStatValue(StatDefOf.MeditationFocusGain), 0.02f);
                }
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast psycast)
            {
                PsychicPowerUtility.KeaOffsetPsyfocusLearning(this.pawn, psycast.FinalPsyfocusCost(target), 1f);
            }
            else if (verb is VEF.Abilities.Verb_CastAbility vcavfe && ModCompatibilityUtility.IsVPEPsycast(vcavfe.ability))
            {
                PsychicPowerUtility.KeaOffsetPsyfocusLearning(this.pawn, ModCompatibilityUtility.GetVPEPsyfocusCost(vcavfe.ability), 1f);
            }
        }
    }
    /*Mynahs have a similar generation dealio as Meerkats. They also gain [baseXPgainPerPeriod*effectScalar] xp in a skill per pawn in their aura who is of a higher level in it than them.
     * (unsurprisingly, the scalar is psysens)
     * If you have RimLanguages, they also make progress towards learning the languages of everyone in their aura.*/
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
                            s.Learn(lvlDiff * this.Props.baseXPgainPerPeriod * self.GetStatValue(this.Props.effectScalar), true);
                        }
                    }
                }
                if (pawn.Faction != null)
                {
                    ModCompatibilityUtility.LearnLanguage(self, pawn, 0.015f * self.GetStatValue(this.Props.effectScalar));
                }
            }
        }
    }
    /*Oilbirds have an aura that isn't bound to their body - instead, it's a separate Thing (this hediff's activeAura). If it doesn't exist, this hediff makes one at the pawn's current position.
     * If it's in the wrong map, it gets rid of the current aura and makes a new one on the pawn's current position. If the unlikely event you've somehow removed this hediff, the aura is destroyed too.
     * The aura is only supposed to affect the Oilbird themself, and its XML prevents it from affecting anyone but its 'creator', so this hediff sets its AuraEmitter comp to regard the Oilbird as its creator.*/
    public class Hediff_Oilbird : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.Spawned)
            {
                if (this.activeAura == null)
                {
                    this.MakeNewAura(this.pawn.Position);
                }
                else if (this.activeAura.Map == null || this.activeAura.Map != this.pawn.Map)
                {
                    if (!this.activeAura.Destroyed)
                    {
                        this.activeAura.Destroy();
                    }
                    this.MakeNewAura(this.pawn.Position);
                }
            }
            else if (this.activeAura != null && !this.activeAura.Destroyed)
            {
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
                cae.faction = this.pawn.Faction ?? null;
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
    /*Swans have a powerful set of buffs while in a romantic relationship (or if Asexual and therefore incapable of having one). Recalculates every hour.
     * Any romaence partners gain a different and somewhat less strong buff which lasts for up to 10d after its last application. Bonded animals get the same buff, but for only half the duration.*/
    public class Hediff_SwanLove : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta))
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
                    this.CreateBuff(dpr.otherPawn, Math.Max(this.pawn.relations.OpinionOf(dpr.otherPawn), 1f));
                }
                else if (dpr.def == PawnRelationDefOf.Bond)
                {
                    this.CreateBuff(dpr.otherPawn, HVTRoyaltyDefOf.HVT_SwanBuff.maxSeverity / 2f);
                }
            }
        }
        public void CreateBuff(Pawn buffee, float severity)
        {
            if (buffee.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_SwanBuff))
            {
                buffee.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_SwanBuff).Severity = severity;
            }
            else
            {
                Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_SwanBuff, buffee);
                hediff.Severity = severity;
                buffee.health.AddHediff(hediff);
            }
        }
    }
    /*Touch-me-nots gain a buff when hit. Since this is a PreDamageModification, it occurs after vanilla shields but before some other PreDamageModifications (based on the priority ordering, but its own is so high it'll usually come first).
     * This means that you can gain the touch-me-not buff, and THEN any PDMs after it might still be able to reduce or entirely absorb the damage.*/
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
            this.Pawn.health.AddHediff(this.Props.hediffSelf, null);
            base.TryDoModification(ref dinfo, ref absorbed);
        }
    }
    /*Egregores have two sets of buffs. The one that scales "physical" stats by psysens does not require any custom work.
     * This one scales psychic stats, including psysens, and so obviously can't be scaled BY psysens unless you love crashing your game immediately.
     * Instead, every periodicity ticks (1 hour in the XML), we get the sum psylink level of all pawns in the same map/caravan (including self) and multiply this by severityPerTotalPsylinkLevels to determine the hediff's severity,
     * with the granted stats scaling off of the hediff's severity.*/
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(this.Props.periodicity, delta))
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
                }
                else if (this.Pawn.IsCaravanMember())
                {
                    Caravan c = this.Pawn.GetCaravan();
                    foreach (Pawn p in c.PawnsListForReading)
                    {
                        if (p.HasPsylink)
                        {
                            severity += p.GetPsylinkLevel();
                        }
                    }
                }
                this.parent.Severity = severity * this.Props.severityPerTotalPsylinkLevels;
            }
        }
    }
    /*Remember Heroes? The show that was good for 1 season? Zachary Quinto hot damn I mean sorry we were talking about transcendences
     * Harpies gain transcendences, woke traits, and archite genes by eating the corpses of other pawns - that's handled by a Harmony patch.
     * As with other pawns who provide stacking buffs to themselves over time, we simulate this process having happened before for a pawn who was generated with this trait. Gain up to three transes and four awakenings.*/
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
                    TranscendenceMethodsUtility.AchieveTranscendence(this.Pawn, "", "", 0f, true, null, true, false, false);
                    bonusTranses--;
                }
                int bonusWokes = (int)(Rand.Value * 4);
                while (bonusWokes > 0)
                {
                    AwakeningMethodsUtility.AwakenPsychicTalent(this.Pawn, false, "", "", true);
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
                    List<GeneDef> unacquiredWokeGenes = PsychicTraitAndGeneCheckUtility.AwakenedGeneList();
                    foreach (Gene g in this.Pawn.genes.GenesListForReading)
                    {
                        if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(g.def) && unacquiredWokeGenes.Contains(g.def))
                        {
                            unacquiredWokeGenes.Remove(g.def);
                        }
                    }
                    if (unacquiredWokeGenes.Count > 0)
                    {
                        this.Pawn.genes.AddGene(unacquiredWokeGenes.RandomElement(), true);
                    }
                }
            }
        }
    }
    /*Leviathan immediately grants a bonus awakening trait, and it grants random transcendences over time, indefinitely (MTB 2 years).
     * As with Anaconda, we back-simulate additional transes to gain for pawns generated with this trait (ThalassicGrandeur).
     * LeWokisme handles the periodic awakening. newAwakeningPeriodicity is the number of ticks in between each awakening. Although it's supposed to send a letter on inducing awakening, I believe it currently does not. Fix is low on priority list.*/
    public class Hediff_ThalassicGrandeur : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            AwakeningMethodsUtility.AwakenPsychicTalent(this.pawn, false, "HVT_WokeByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_WokeByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), false);
            if (PawnGenerator.IsBeingGenerated(this.pawn))
            {
                int bonusTranses = (int)(Rand.Value * 3);
                while (bonusTranses > 0)
                {
                    TranscendenceMethodsUtility.AchieveTranscendence(this.pawn, "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), 0.25f, true, null, true, false, false);
                    bonusTranses--;
                }
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(200, delta) && Rand.MTBEventOccurs(120f, 60000f, 200f))
            {
                TranscendenceMethodsUtility.AchieveTranscendence(this.pawn, "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), "HVT_TransByLeviathan".Translate().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), 0.25f, false, null, true, false, false);
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
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            this.ticksToNextAwakening -= delta;
            if (this.ticksToNextAwakening <= 0)
            {
                this.ticksToNextAwakening = this.Props.newAwakeningPeriodicity;
                string wokeString = "HVT_WokeByLeviathan".Translate().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve();
                AwakeningMethodsUtility.AwakenPsychicTalent(this.Pawn, true, wokeString, wokeString, true);
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksToNextAwakening, "ticksToNextAwakening", 60000, false);
        }
        public int ticksToNextAwakening;
    }
}
