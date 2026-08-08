using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    //xpathed onto an ability to prevent it from targeting awakened pawns
    public class CompProperties_AbilityCantTargetWoke : CompProperties_AbilityEffect
    {
    }
    public class CompAbilityEffect_CantTargetWoke : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p))
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "HVT_CantTargetWoke".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
    }
    //chanshi need gain from meditation
    public class HediffCompProperties_Chanshi : HediffCompProperties_SatisfiesNeeds
    {
        public HediffCompProperties_Chanshi()
        {
            this.compClass = typeof(HediffComp_Chanshi);
        }
    }
    public class HediffComp_Chanshi : HediffComp_SatisfiesNeeds
    {
        public new HediffCompProperties_Chanshi Props
        {
            get
            {
                return (HediffCompProperties_Chanshi)this.props;
            }
        }
        public override bool ConditionsMetToSatisfyNeeds
        {
            get
            {
                return this.Pawn.psychicEntropy != null && this.Pawn.psychicEntropy.IsCurrentlyMeditating;
            }
        }
    }
    //incarnate severity per psycast known
    public class Hediff_IncarnatePsycastsKnown : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(150, delta))
            {
                float castsKnown = 0f;
                foreach (Ability a in this.pawn.abilities.abilities)
                {
                    if (a.def.IsPsycast)
                    {
                        castsKnown += 1f;
                    }
                }
                if (castsKnown > 0f)
                {
                    this.Severity = castsKnown;
                }
                else
                {
                    this.Severity = 0.001f;
                }
                this.Severity = this.VPECompat();
            }
        }
        //this gets Harmony patched in VPE compat to work with VPE psycasts
        public float VPECompat()
        {
            return this.Severity;
        }
    }
    /*NPC perennials who are currently casting a psycast (or VPE psycast) pop Archonic Syzygy to get its buffs, under the assumption they will continue to cast stuff afterward.
     * "You should use variable tick rate!" I would agree, but Ludeon didn't make abilities' CompTickIntervals actually get used by anything!*/
    public class CompAbilityEffect_Syzygy : CompAbilityEffect_ForcedByOtherProperty
    {
        public override void CompTick()
        {
            if (this.parent.CanCast && !this.parent.pawn.IsPlayerControlled && this.parent.pawn.CurJob != null && ((this.parent.pawn.CurJob.ability != null && this.parent.pawn.CurJob.ability.def.IsPsycast) || (this.parent.pawn.CurJob.verbToUse is VEF.Abilities.Verb_CastAbility vca && ModCompatibilityUtility.IsVPEPsycast(vca.ability))))
            {
                this.parent.pawn.jobs.StartJob(this.parent.GetJob(new LocalTargetInfo(this.parent.pawn), null), JobCondition.InterruptForced, null, true, false);
            }
            base.CompTick();
        }
    }
    //undying damage to psyfocus deferral
    public class HediffCompProperties_ManaBarrier : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_ManaBarrier()
        {
            this.compClass = typeof(HediffComp_ManaBarrier);
        }
        public Dictionary<TraitDef, float> contributingTraits;
        public Dictionary<GeneDef, float> contributingGenes;
    }
    public class HediffComp_ManaBarrier : HediffComp_DamageNegation
    {
        public new HediffCompProperties_ManaBarrier Props
        {
            get
            {
                return (HediffCompProperties_ManaBarrier)this.props;
            }
        }
        public float ManaBarrierStrength
        {
            get
            {
                float manaBarrierStrength = 0f;
                if (this.Pawn.psychicEntropy != null && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
                {
                    if (this.Props.contributingTraits != null && this.Pawn.story != null)
                    {
                        foreach (Trait t in this.Pawn.story.traits.allTraits)
                        {
                            if (this.Props.contributingTraits.ContainsKey(t.def))
                            {
                                manaBarrierStrength += this.Props.contributingTraits.TryGetValue(t.def);
                            }
                        }
                    }
                    if (ModsConfig.BiotechActive && this.Props.contributingGenes != null && this.Pawn.genes != null)
                    {
                        foreach (GeneDef gd in this.Props.contributingGenes.Keys.ToList())
                        {
                            if (this.Pawn.genes.HasActiveGene(gd))
                            {
                                manaBarrierStrength += this.Props.contributingGenes.TryGetValue(gd);
                            }
                        }
                    }
                }
                return manaBarrierStrength;
            }
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            if (this.ManaBarrierStrength > 0f && this.Pawn.psychicEntropy.CurrentPsyfocus >= 0.01f)
            {
                float maxNegatableDamage = 100f * this.Pawn.psychicEntropy.CurrentPsyfocus * this.ManaBarrierStrength;
                float toughnessContribution = 1f;
                if (this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor) < 1f && this.Props.shouldUseIncomingDamageFactor)
                {
                    toughnessContribution = 2f / (1f + this.Pawn.GetStatValue(StatDefOf.IncomingDamageFactor));
                    maxNegatableDamage *= toughnessContribution;
                }
                float actualNegationUsed = Math.Min(maxNegatableDamage, dinfo.Amount);
                dinfo.SetAmount(Math.Max(dinfo.Amount - maxNegatableDamage, 0f));
                this.Pawn.psychicEntropy.OffsetPsyfocusDirectly(-actualNegationUsed / (this.ManaBarrierStrength * 100f * toughnessContribution));
                this.DoGraphics(dinfo, amount);
            }
        }
        public override void DoGraphics(DamageInfo dinfo, float amount)
        {
            if (this.Props.soundOnBlock != null && this.Pawn.Spawned)
            {
                this.Props.soundOnBlock.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            }
            if (amount > 0 && this.Pawn.SpawnedOrAnyParentSpawned)
            {
                Vector3 loc = this.Pawn.SpawnedParentOrMe.TrueCenter();
                {
                    if (this.Props.fleckOnBlock != null)
                        FleckMaker.Static(loc, this.Pawn.MapHeld, this.Props.fleckOnBlock, 0.67f);
                }
                if (this.Props.throwDustPuffsOnBlock)
                {
                    int num2 = (int)Mathf.Min(10f, 2f + amount / 10f);
                    for (int i = 0; i < num2; i++)
                    {
                        FleckMaker.ThrowDustPuff(loc, this.Pawn.MapHeld, Rand.Range(0.8f, 1.2f));
                    }
                }
            }
        }
    }
}
