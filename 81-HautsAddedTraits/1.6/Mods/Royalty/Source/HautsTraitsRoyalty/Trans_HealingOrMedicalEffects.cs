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
    //used by multiple transes, it's basically like luciferium except the cooldown is specifiable in XML and is divided by the pawn's psysens
    public class HediffCompProperties_HealPermanentWoundsPsyScaling : HediffCompProperties
    {
        public HediffCompProperties_HealPermanentWoundsPsyScaling()
        {
            this.compClass = typeof(HediffComp_HealPermanentWoundsPsyScaling);
        }
        public IntRange ticksToHeal;
    }
    public class HediffComp_HealPermanentWoundsPsyScaling : HediffComp
    {
        public HediffCompProperties_HealPermanentWoundsPsyScaling Props
        {
            get
            {
                return (HediffCompProperties_HealPermanentWoundsPsyScaling)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.ResetTicksToHeal();
        }
        private void ResetTicksToHeal()
        {
            this.ticksToHeal = (int)(this.Props.ticksToHeal.RandomInRange / Math.Max(1f, this.parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity)));
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (this.ticksToHeal > 0)
            {
                this.ticksToHeal -= delta;
            }
            else
            {
                HediffComp_HealPermanentWoundsPsyScaling.TryRegenerateInjury(base.Pawn, this.parent.LabelCap);
                this.ResetTicksToHeal();
            }
        }
        public static void TryRegenerateInjury(Pawn pawn, string cause)
        {
            Hediff hediff;
            if (!(from hd in pawn.health.hediffSet.hediffs
                  where hd.IsPermanent() || hd.def.hediffClass == typeof(Hediff_MissingPart) || hd.def.chronic
                  select hd).TryRandomElement(out hediff))
            {
                return;
            }
            if (hediff.def.hediffClass == typeof(Hediff_MissingPart))
            {
                pawn.health.RestorePart(hediff.Part);
            }
            else
            {
                HealthUtility.Cure(hediff);
            }
        }
        private int ticksToHeal;
    }
    /*Cranchiids' hediff gains severity based on how 'deep' the current water tile is and how heavily it's raining if exposed to rain (that comes from this being a WaterImmersionSeverity derivative).
     * Its severity acts as a multiplier to its regeneration, which works a lot like the regeneration system introduced in Anomaly. Frankly I just didn't want to use <regeneration> since it seems Anomaly-exclusive*/
    public class HediffCompProperties_GlassSquid : HediffCompProperties_WaterImmersionSeverity
    {
        public HediffCompProperties_GlassSquid()
        {
            this.compClass = typeof(HediffComp_GlassSquid);
        }
        public float healPerSeverity;
    }
    public class HediffComp_GlassSquid : HediffComp_WaterImmersionSeverity
    {
        public new HediffCompProperties_GlassSquid Props
        {
            get
            {
                return (HediffCompProperties_GlassSquid)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                this.bankedHealing += this.parent.Severity * this.Props.healPerSeverity;
                if (this.bankedHealing > 1f)
                {
                    this.bankedHealing -= 1f;
                    float heal = 1.5f;
                    List<Hediff> injuries = new List<Hediff>();
                    List<Hediff> missingParts = new List<Hediff>();
                    foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
                    {
                        if (h is Hediff_Injury)
                        {
                            injuries.Add(h);
                        }
                        else if (h is Hediff_MissingPart && (h.Part.parent == null || (!this.Pawn.health.hediffSet.PartIsMissing(h.Part.parent) && !this.Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(h.Part.parent))))
                        {
                            missingParts.Add(h);
                        }
                    }
                    foreach (Hediff h in injuries)
                    {
                        float toHeal = Math.Min(h.Severity, heal);
                        h.Severity -= toHeal;
                        heal -= toHeal;
                        if (heal <= 0f)
                        {
                            return;
                        }
                    }
                    if (heal > 0f)
                    {
                        foreach (Hediff h in missingParts)
                        {
                            BodyPartRecord part = h.Part;
                            this.Pawn.health.RemoveHediff(h);
                            Hediff hediff5 = this.Pawn.health.AddHediff(HediffDefOf.Misc, part, null, null);
                            float partHealth = this.Pawn.health.hediffSet.GetPartHealth(part);
                            hediff5.Severity = Mathf.Max(partHealth - 1f, partHealth * 0.9f);
                            return;
                        }
                    }
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.bankedHealing, "bankedHealing", 0f, false);
        }
        public float bankedHealing;
    }
    /*Knapweed deals "damage" to a nearby plant while injured or down w an immunizable sickness. As it burns away into smoke ("damageType" is actually Rotting), the severity of a random such negative hediff is reduced by "lifeLeech".
     * They also drain psyfocus from hostile psycasters in their aura at a rate of psyfocusLeech per quarter second (1.4% per second unless you modify the XML).*/
    public class HediffCompProperties_AuraAllelopathy : HediffCompProperties_Aura
    {
        public HediffCompProperties_AuraAllelopathy()
        {
            this.compClass = typeof(HediffComp_AuraAllelopathy);
        }
        public float damage;
        public DamageDef damageType;
        public float lifeLeech;
        public float psyfocusLeech;
    }
    public class HediffComp_AuraAllelopathy : HediffComp_Aura
    {
        public new HediffCompProperties_AuraAllelopathy Props
        {
            get
            {
                return (HediffCompProperties_AuraAllelopathy)this.props;
            }
        }
        public override void AffectPawn(Pawn self, Pawn pawn)
        {
            base.AffectPawn(self, pawn);
            if (pawn.psychicEntropy != null && self.psychicEntropy != null && self.psychicEntropy.CurrentPsyfocus < 1f)
            {
                float leech = Math.Min(Math.Min(this.Props.psyfocusLeech, pawn.psychicEntropy.CurrentPsyfocus), 1f - self.psychicEntropy.CurrentPsyfocus);
                pawn.psychicEntropy.OffsetPsyfocusDirectly(-leech);
                if (leech > 0f)
                {
                    self.psychicEntropy.OffsetPsyfocusDirectly(leech);
                }
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            List<Hediff> healables = new List<Hediff>();
            foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury)
                {
                    healables.Add(h);
                }
                else if (h is HediffWithComps hwc)
                {
                    HediffComp_Immunizable hci = hwc.TryGetComp<HediffComp_Immunizable>();
                    if (hci != null)
                    {
                        healables.Add(h);
                    }
                }
            }
            if (healables.Count > 0)
            {
                List<Plant> plants = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.range, true).OfType<Plant>().Distinct<Plant>().ToList<Plant>();
                while (healables.Count > 0 && plants.Count > 0)
                {
                    Plant plant = plants.RandomElement();
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(plant.PositionHeld.ToVector3(), plant.MapHeld, FleckDefOf.Smoke, 1f);
                    dataStatic.rotationRate = Rand.Range(-30f, 30f);
                    dataStatic.velocityAngle = (float)Rand.Range(30, 40);
                    dataStatic.velocitySpeed = Rand.Range(0.5f, 0.7f);
                    plant.MapHeld.flecks.CreateFleck(dataStatic);
                    plant.TakeDamage(new DamageInfo(this.Props.damageType, this.Props.damage));
                    Hediff h2 = healables.RandomElement();
                    h2.Severity -= this.Props.lifeLeech;
                    if (h2.ShouldRemove)
                    {
                        this.Pawn.health.RemoveHediff(h2);
                        healables.Remove(h2);
                    }
                    plants.Remove(plant);
                }
            }
        }
    }
    //in addition to its health stat buffing, the Ladybug aura also cleans any filth in its radius except for the blacklisted types in exemptFilthTypes
    public class HediffCompProperties_AuraLadybug : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_AuraLadybug()
        {
            this.compClass = typeof(HediffComp_AuraLadybug);
        }
        public List<ThingDef> exemptFilthTypes;
    }
    public class HediffComp_AuraLadybug : HediffComp_AuraHediff
    {
        public new HediffCompProperties_AuraLadybug Props
        {
            get
            {
                return (HediffCompProperties_AuraLadybug)this.props;
            }
        }
        public override void AffectSelf()
        {
            base.AffectSelf();
            if (this.Pawn.Spawned)
            {
                foreach (Filth filth in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.Props.range, true).OfType<Filth>().Distinct<Filth>())
                {
                    if (!this.Props.exemptFilthTypes.Contains(filth.def))
                    {
                        filth.ThinFilth();
                    }
                }
            }
        }
    }
    /*Mosquitoes have a psycast-based InflictHediffOnHit that inflicts blood loss, up to the limit of canOnlyIncreaseSeverityUpTo (non-lethal). Based on the amount of blood loss inflicted,
     * heal from an injury by an amount also scaling off lifestealEfficiency. If the Mosquito is hemogenic, gain hemogen which also scales with hemogenGainEfficiency; if the victim is hemogenic, they lose an identical amount.*/
    public class HediffCompProperties_Mosquito : HediffCompProperties_InflictHediffOnHit
    {
        public HediffCompProperties_Mosquito()
        {
            this.compClass = typeof(HediffComp_Mosquito);
        }
        public float lifestealEfficiency = 1f;
        public float hemogenGainEfficiency = 1f;
    }
    public class HediffComp_Mosquito : HediffComp_InflictHediffOnHit
    {
        public new HediffCompProperties_Mosquito Props
        {
            get
            {
                return (HediffCompProperties_Mosquito)this.props;
            }
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            float remainingBlood = victim.health.hediffSet.HasHediff(this.Props.hediff) ? Math.Max(0f, this.Props.canOnlyIncreaseSeverityUpTo - victim.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff).Severity) : this.Props.canOnlyIncreaseSeverityUpTo;
            base.DoExtraEffects(victim, valueToScale, hitPart);
            if (victim != this.parent.pawn)
            {
                List<Hediff_Injury> source = new List<Hediff_Injury>();
                this.parent.pawn.health.hediffSet.GetHediffs<Hediff_Injury>(ref source, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
                float lifeStolen = Math.Min(remainingBlood, this.ScaledValue(victim, this.Props.baseSeverity, valueToScale)) * this.Props.lifestealEfficiency;
                if (source.TryRandomElement(out Hediff_Injury hediff_Injury))
                {
                    hediff_Injury.Heal(100f * lifeStolen);
                }
                lifeStolen *= this.Props.hemogenGainEfficiency;
                GeneUtility.OffsetHemogen(this.parent.pawn, lifeStolen, true);
                GeneUtility.OffsetHemogen(victim, -lifeStolen, true);
            }
        }
    }
    /*Pitohui have a derivative of CureHediffOnHit which removes drug overdoses, food poisoning, chemical dependencies and addictions (even for luciferium).
     * It also deals -20% severity to any hediff found that's on its hediffsToCure whitelist*/
    public class HediffCompProperties_PitohuiPurgeOnHit : HediffCompProperties_CureHediffsOnHit
    {
        public HediffCompProperties_PitohuiPurgeOnHit()
        {
            this.compClass = typeof(HediffComp_PitohuiPurgeOnHit);
        }
    }
    public class HediffComp_PitohuiPurgeOnHit : HediffComp_CureHediffsOnHit
    {
        public new HediffCompProperties_PitohuiPurgeOnHit Props
        {
            get
            {
                return (HediffCompProperties_PitohuiPurgeOnHit)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.pawn.IsHashIntervalTick(200, delta))
            {
                this.PurgePitohuiToxins(this.parent.pawn, this.parent.pawn);
            }
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target)
        {
            base.Notify_PawnUsedVerb(verb, target);
            if (verb is RimWorld.Verb_CastAbility vca && vca.ability is Psycast)
            {
                List<LocalTargetInfo> targets = vca.ability.GetAffectedTargets(target).ToList();
                foreach (LocalTargetInfo lti in targets)
                {
                    if (lti.Pawn != null && !this.parent.pawn.HostileTo(lti.Pawn))
                    {
                        this.PurgePitohuiToxins(lti.Pawn, this.parent.pawn);
                    }
                }
            }
            else if (verb is VEF.Abilities.Verb_CastAbility vcavfe && ModCompatibilityUtility.IsVPEPsycast(vcavfe.ability))
            {
                GlobalTargetInfo[] targets = new GlobalTargetInfo[]
                {
                        target.ToGlobalTargetInfo(vcavfe.Caster.Map)
                };
                vcavfe.ability.ModifyTargets(ref targets);
                foreach (LocalTargetInfo lti in targets)
                {
                    if (lti.Pawn != null && !this.parent.pawn.HostileTo(lti.Pawn))
                    {
                        this.PurgePitohuiToxins(lti.Pawn, this.parent.pawn);
                    }
                }
            }
        }
        public void PurgePitohuiToxins(Pawn pawn, Pawn pitohui)
        {
            base.DoExtraEffects(pawn, 1f);
            Hediff hediff1 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.DrugOverdose, false);
            if (hediff1 != null)
            {
                pawn.health.hediffSet.hediffs.Remove(hediff1);
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiOD".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                else
                {
                    Messages.Message("HVT_PitohuiODother".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            Hediff hediff2 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning, false);
            if (hediff2 != null)
            {
                pawn.health.hediffSet.hediffs.Remove(hediff2);
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiFP".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                else
                {
                    Messages.Message("HVT_PitohuiFPother".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            List<Hediff> hediffsToRemove = new List<Hediff>();
            bool removedDependency = false;
            bool removedAddiction = false;
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h as Hediff_ChemicalDependency != null)
                {
                    hediffsToRemove.Add(h);
                    removedDependency = true;
                }
                else if (h as Hediff_Addiction != null)
                {
                    hediffsToRemove.Add(h);
                    removedAddiction = true;
                }
            }
            foreach (Hediff h in hediffsToRemove)
            {
                pawn.health.hediffSet.hediffs.Remove(h);
            }
            if (removedDependency)
            {
                string how = ModCompatibilityUtility.IsHighFantasy() ? "HVT_PitohuiGenDepFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve() : "HVT_PitohuiGenDep".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve();
                Messages.Message(how, pawn, MessageTypeDefOf.PositiveEvent, true);
            }
            if (removedAddiction)
            {
                if (pawn == pitohui)
                {
                    Messages.Message("HVT_PitohuiAdd".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
                else
                {
                    Messages.Message("HVT_PitohuiAddOther".Translate().CapitalizeFirst().Formatted(pitohui.Name.ToStringShort, pawn.Name.ToStringShort).Resolve(), pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    //a lot of Psychic Dragon's effects are handled elsewhere (in the XML or in two Harmony patches). This just causes them to heal every hour from any illnesses or bleeding conditions.
    public class Hediff_DragonsHoard : Hediff_PreDamageModification
    {
        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "HVT_DragonTooltip".Translate(this.Severity.ToStringByStyle(ToStringStyle.FloatMaxTwo));
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta))
            {
                for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.health.hediffSet.hediffs[i].def.makesSickThought || this.pawn.health.hediffSet.hediffs[i].Bleeding)
                    {
                        this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                    }
                }
            }
        }
    }
}
