using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Hediffs;
using Verse;

namespace HautsTraitsRoyalty
{
    //ewisott. The Harmony patch abruptly removes any Hediff_BloodRage hediff on this pawn, right in its PostAdd
    public class BloodRainImmune : DefModExtension
    {
        public BloodRainImmune() { }
    }
    /*prevents gravship takeoff from destroying any map that still has a pawn with a trait tagged with this DME on it (gravity-manipulators like the Fodiator or Quelea).
     * While you're here, I'd like to make the case that you should be using Vanilla Gravship Expanded - Chapter 1 if you aren't already.
     * You will rarely hear me shill for a VE mod (because they're already very popular or at least well-known). VGE is very much worth it. One of the few VE mods that actually fixes a lackluster non-modded system.
     * Using VGE will completely obviate the need for this DME, but until such time as everyone has it in their modlist LivingGravAnchor has a purpose.*/
    public class LivingGravAnchor : DefModExtension
    {
        public LivingGravAnchor()
        {

        }
    }
    /*Arcturans are "immmune" to several heatlh conditions in their hediffsToRemoveSelf whitelist, of which the only non-modded one is Heart Attack. They are similarly immune to isBad hediffs that cap consciousness at 30% or lower.
     * Immunity, in this case, is the removal of all such conditions every 3 seconds.*/
    public class HediffCompProperties_AndItsMyOwnHeart : HediffCompProperties_ExtraDamageOnHit
    {
        public HediffCompProperties_AndItsMyOwnHeart()
        {
            this.compClass = typeof(HediffComp_AndItsMyOwnHeart);
        }
        public List<HediffDef> hediffsToRemoveSelf;
    }
    public class HediffComp_AndItsMyOwnHeart : HediffComp_ExtraDamageOnHit
    {
        public new HediffCompProperties_AndItsMyOwnHeart Props
        {
            get
            {
                return (HediffCompProperties_AndItsMyOwnHeart)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(180, delta))
            {
                for (int i = this.Pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff hediff = this.Pawn.health.hediffSet.hediffs[i];
                    if (this.Props.hediffsToRemoveSelf.Contains(hediff.def))
                    {
                        this.Pawn.health.RemoveHediff(hediff);
                    }
                    else if (hediff.def.isBad && hediff.CurStage != null && hediff.CurStage.capMods != null)
                    {
                        foreach (PawnCapacityModifier pcm in hediff.CurStage.capMods)
                        {
                            if (pcm.capacity == PawnCapacityDefOf.Consciousness && pcm.setMax <= 0.3f)
                            {
                                this.Pawn.health.RemoveHediff(hediff);
                            }
                        }
                    }
                }
            }
        }
    }
    /*Harbingers defer 100% of damage taken to a corpse or Anomaly mutant within corpseSearchRadius, provided there is one. Because corpses have 100 hit points regardless of how tough they were while alive,
     * the damage they take is scaled by their inner pawns' Lethal Damage Threshold.*/
    public class HediffCompProperties_PupilOfTheGrave : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_PupilOfTheGrave()
        {
            this.compClass = typeof(HediffComp_PupilOfTheGrave);
        }
        public float corpseSearchRadius;
    }
    public class HediffComp_PupilOfTheGrave : HediffComp_DamageNegation
    {
        public new HediffCompProperties_PupilOfTheGrave Props
        {
            get
            {
                return (HediffCompProperties_PupilOfTheGrave)this.props;
            }
        }
        public List<Thing> NearbyCorpses()
        {
            List<Thing> corpses = new List<Thing>();
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.corpseSearchRadius, true).ToList())
            {
                if (t is Pawn p && ModsConfig.AnomalyActive && p.IsMutant)
                {
                    corpses.Add(p);
                }
                else if (t is Corpse c)
                {
                    corpses.Add(c);
                }
            }
            return corpses;
        }
        public override bool ShouldPreventAttachment(Thing attachment)
        {
            if (base.ShouldPreventAttachment(attachment))
            {
                List<Thing> corpses = this.NearbyCorpses();
                return corpses.Count > 0;
            }
            return false;
        }
        public override void DoModificationInner(ref DamageInfo dinfo, ref bool absorbed, float amount)
        {
            List<Thing> corpses = this.NearbyCorpses();
            if (corpses.Count > 0)
            {
                float initialDamage = dinfo.Amount;
                base.DoModificationInner(ref dinfo, ref absorbed, amount);
                float removedDamage = Math.Max(0f, initialDamage - dinfo.Amount);
                while (removedDamage > 0 && corpses.Count > 0)
                {
                    Thing corpse = corpses.RandomElement();
                    Pawn p = corpse as Pawn;
                    float toTake = removedDamage;
                    if (corpse is Corpse c)
                    {
                        toTake = Math.Min(removedDamage * 75 / c.InnerPawn.health.LethalDamageThreshold, c.HitPoints);
                    }
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(corpse.PositionHeld.ToVector3(), corpse.MapHeld, FleckDefOf.Smoke, 1f);
                    dataStatic.rotationRate = Rand.Range(-30f, 30f);
                    dataStatic.velocityAngle = (float)Rand.Range(30, 40);
                    dataStatic.velocitySpeed = Rand.Range(0.5f, 0.7f);
                    corpse.MapHeld.flecks.CreateFleck(dataStatic);
                    corpses.Remove(corpse);
                    corpse.TakeDamage(new DamageInfo(dinfo.Def, toTake, 99f, -1f, dinfo.Instigator, p != null ? p.health.hediffSet.GetRandomNotMissingPart(dinfo.Def) : null, dinfo.Weapon, dinfo.Category));
                    removedDamage -= toTake;
                }
            }
        }
    }
    //Noctules are immune to the damage of Anomaly's unnatural darkness. It's caused by a hediff that takes longer than a second to start doing damage, so we just need to remove that every second.
    public class Hediff_TheHeartOfDarkness : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(60))
            {
                for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.health.hediffSet.hediffs[i] is Hediff_DarknessExposure)
                    {
                        this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                    }
                }
            }
        }
    }
    //Quokkas never take damage from animals. I forget why there's an option to make it only work on insectoids - maybe this was intended to be one of the comps for Spelopede before I actually started working on it?
    public class HediffCompProperties_AnimalImmunity : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_AnimalImmunity()
        {
            this.compClass = typeof(HediffComp_AnimalImmunity);
        }
        public bool onlyInsectoids = false;
    }
    public class HediffComp_AnimalImmunity : HediffComp_DamageNegation
    {
        public new HediffCompProperties_AnimalImmunity Props
        {
            get
            {
                return (HediffCompProperties_AnimalImmunity)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return base.ShouldDoEffect(dinfo) && dinfo.Instigator != null && dinfo.Instigator is Pawn p && p.RaceProps.Animal && (!this.Props.onlyInsectoids || p.RaceProps.Insect);
        }
    }
    /*Scarabs have a DamageNegation effect. Although it visually resembles DamageNegationShield, it far predates the creation of that tool.
     * The shield graphics are handled via the NeuralHeatShield comp, utilizing VEF's shield hediff drawing technology.
     * Scarab is the actual DamageNegation hediff, which overrides PayCostOfHit to grant neural heat based on [the damage blocked * percentNeuralHeatOnHit]. In more standardized RimWorld nomenclature, that should more correctly be called
     * something like damageToNeuralHeatFactor or whatever. Sue me I made this one like three years ago. I still didn't know what an ExposeData was.
     * If neural heat goes beyond the limit, then the pawn gains the switchToOnBreak hediff (a neural heat recovery buff which will eventually regen into this hediff via its own comps, in this case) and this hediff is removed.
     * The ForcedByOtherProperty comp is written to permit this substitution.*/
    public class HediffCompProperties_NeuralHeatShield : HediffCompProperties
    {
        public override void PostLoad()
        {
            base.PostLoad();
            ShieldsSystem.ApplyDrawPatches();
        }
        public HediffCompProperties_NeuralHeatShield()
        {
            this.compClass = typeof(HediffComp_NeuralHeatShield);
        }
        public float displayUnderSeverity = 1f;
        public float drawSize = 1f;
        public Color color = new Color(1f, 1f, 1f, 1f);
    }
    public class HediffComp_NeuralHeatShield : HediffComp_Draw
    {
        public HediffCompProperties_NeuralHeatShield Props
        {
            get
            {
                return (HediffCompProperties_NeuralHeatShield)this.props;
            }
        }
        protected bool ShouldDisplay
        {
            get
            {
                return this.Pawn.Spawned && !this.Pawn.Dead && !this.Pawn.Downed && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon && this.parent.Severity <= this.Props.displayUnderSeverity;
            }
        }
        public override void DrawAt(Vector3 drawPos)
        {
            if (this.ShouldDisplay)
            {
                float num = this.Props.drawSize;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                int num2 = Find.TickManager.TicksGame - this.lastAbsorbDamageTick;
                if (num2 < 8)
                {
                    float num3 = (float)(8 - num2) / 8f * 0.05f;
                    num -= num3;
                }
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default;
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, this.Props.color), 0);
            }
        }
        private int lastAbsorbDamageTick = -9999;
    }
    public class HediffCompProperties_Scarab : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_Scarab()
        {
            this.compClass = typeof(HediffComp_Scarab);
        }
        public HediffDef switchToOnBreak;
        public float percentNeuralHeatOnHit;
    }
    public class HediffComp_Scarab : HediffComp_DamageNegation
    {
        public new HediffCompProperties_Scarab Props
        {
            get
            {
                return (HediffCompProperties_Scarab)this.props;
            }
        }
        public override void PayCostOfHit(float damageAmount)
        {
            this.Pawn.psychicEntropy.TryAddEntropy((damageAmount * this.Props.severityOnHit) + (this.Pawn.psychicEntropy.MaxEntropy * this.Props.percentNeuralHeatOnHit), null, true, true);
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.psychicEntropy != null)
            {
                this.parent.Severity = this.Pawn.psychicEntropy.EntropyRelativeValue;
                if (this.parent.Severity >= 1f)
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.switchToOnBreak, this.Pawn);
                    this.Pawn.health.AddHediff(hediff, this.parent.Part);
                    hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Def);
                    this.Pawn.health.RemoveHediff(hediff);
                }
            }
        }
        public override bool ShouldDoModificationInner(DamageInfo dinfo)
        {
            return base.ShouldDoModificationInner(dinfo) && this.Pawn.psychicEntropy != null && this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon;
        }
    }
    //Vultures have a DamageNegation which simply Always Works against Anomaly mutants.
    public class HediffCompProperties_MutantImmunity : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_MutantImmunity()
        {
            this.compClass = typeof(HediffComp_MutantImmunity);
        }
    }
    public class HediffComp_MutantImmunity : HediffComp_DamageNegation
    {
        public new HediffCompProperties_MutantImmunity Props
        {
            get
            {
                return (HediffCompProperties_MutantImmunity)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            return base.ShouldDoEffect(dinfo) && dinfo.Instigator != null && dinfo.Instigator is Pawn p && p.IsMutant;
        }
    }
    //Wraiths gain a buff after transferring to a new body. This is one of the comps of that buff, which provides immunity to damage from anything other than a Bladelink-tagged weapon (a persona weapon)
    public class HediffCompProperties_DamageNegationWraith : HediffCompProperties_DamageNegation
    {
        public HediffCompProperties_DamageNegationWraith()
        {
            this.compClass = typeof(HediffComp_DamageNegationWraith);
        }
    }
    public class HediffComp_DamageNegationWraith : HediffComp_DamageNegation
    {
        public new HediffCompProperties_DamageNegationWraith Props
        {
            get
            {
                return (HediffCompProperties_DamageNegationWraith)this.props;
            }
        }
        public override bool ShouldDoEffect(DamageInfo dinfo)
        {
            if (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon)
            {
                return false;
            }
            if (dinfo.Weapon != null && dinfo.Weapon.weaponTags != null && dinfo.Weapon.weaponTags.Contains("Bladelink"))
            {
                return false;
            }
            return base.ShouldDoEffect(dinfo);
        }
    }
}
