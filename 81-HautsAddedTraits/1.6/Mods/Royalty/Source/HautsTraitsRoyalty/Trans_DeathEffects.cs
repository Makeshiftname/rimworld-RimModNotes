using HautsFramework;
using RimWorld;
using System;
using Verse;

namespace HautsTraitsRoyalty
{
    /*Pawns resurrected by the last-hit of a Dulotic Psychic gain a hediff with this class. This sets their initial "a hostile is nearby, what should I do!?" response to attacking it, because this is much more useful than if they auto-fleed (the default).
     * If you don't like that, you can change it afterward, just like you could change any of your colonists' threat responses.
     * Removes itself on subsequent resurrections, but not on death because its existence on a dead pawn prevents it from being subject to further Dulosis resurrections. We don't want Dulosis to be able to chain infinitely, otherwise you just have
     * free recruits that require the occasional micromanagement to keep alive.*/
    public class Hediff_Dulosis : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.playerSettings != null)
            {
                this.pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(this.pawn);
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            this.pawn.health.RemoveHediff(this);
        }
    }
    /*Erinys' "Psychic Censure" ability either applies the hediffToGrant (Censure, which is 0x psysens) to the target if it isn't already psydeaf, or kills it if it is.
     * Its cooldown changes depending on the effect and victim.
     * Simply deafening a pawn does the minimum cooldown. Killing a non-psycaster inflicts cooldownOnNonPsycasterKill (coincidentally equal to the minimum in the XML).
     * Killing a psycaster makes the cooldown = [baseCooldownOnPsycasterKill * the victim's psylink level to the power of itself], with the psylink level capping at psylinkLevelCapForCooldown for this calculation's purpose.
     *   It can't go above the max of the cooldownTicksRange.
     * severityPerPsylinklevel: if the victim is a psycaster, the inflicted hediff's severity is this amount times its psylink level i.e. it's more severe (longer-lasting) on higher-level pawns.
     * If it kills a psycaster, it removes a random amount of their psylinks, creating 1 neuroformer nearby per lost level.
     * Derivative of AiScansForTargets, so that NPC Erinyes will just start fucking your pawns over. They will gladly target anyone who is already psydeaf, who has >100% psysens, or is a psycaster.
     * Otherwise, they need to pass the aiUseFrequencyOnMundanes chance (40% in XML), as pawns who didn't meet those criteria are not quite as valuable targets.*/
    public class CompProperties_AbilityErinys : CompProperties_AbilityAiScansForTargets
    {
        public float aiUseFrequencyOnMundanes;
        public int cooldownOnNonPsycasterKill;
        public int baseCooldownOnPsycasterKill;
        public int psylinkLevelCapForCooldown = 999;
        public HediffDef hediffToGrant;
        public float severityPerPsylinklevel;
    }
    public class CompAbilityEffect_Erinys : CompAbilityEffect_AiScansForTargets
    {
        public new CompProperties_AbilityErinys Props
        {
            get
            {
                return (CompProperties_AbilityErinys)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn != null)
            {
                float psysens = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                if (!pawn.health.hediffSet.HasHediff(HVTRoyaltyDefOf.HVT_ErinysCensure))
                {
                    Hediff hediff = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_ErinysCensure, pawn, null);
                    pawn.health.AddHediff(hediff);
                }
                bool lethal = psysens <= float.Epsilon && (pawn.story == null || !PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn));
                if (lethal)
                {
                    if (pawn.HasPsylink)
                    {
                        int psyLevel = pawn.GetPsylinkLevel();
                        int funcPsyLevel = Math.Min(this.Props.psylinkLevelCapForCooldown, psyLevel);
                        this.parent.StartCooldown(this.Props.baseCooldownOnPsycasterKill * (int)Math.Pow(funcPsyLevel, funcPsyLevel));
                        this.parent.pawn.health.hediffSet.TryGetHediff(this.Props.hediffToGrant, out Hediff h);
                        if (h != null)
                        {
                            h.Severity += psyLevel * this.Props.severityPerPsylinklevel;
                        }
                        else
                        {
                            h = HediffMaker.MakeHediff(this.Props.hediffToGrant, this.parent.pawn);
                            this.parent.pawn.health.AddHediff(h);
                            h.Severity = psyLevel * this.Props.severityPerPsylinklevel;
                        }
                        int psylinks = (int)Math.Ceiling(Rand.Value * pawn.GetPsylinkLevel());
                        for (int i = 0; i < psylinks; i++)
                        {
                            Thing thing = ThingMaker.MakeThing(ThingDefOf.PsychicAmplifier, null);
                            GenSpawn.Spawn(thing, pawn.Position, pawn.Map, Rot4.North, WipeMode.Vanish, false);
                            pawn.ChangePsylinkLevel(-1, false);
                        }
                    }
                    else
                    {
                        this.parent.StartCooldown(this.Props.cooldownOnNonPsycasterKill);
                    }
                    pawn.Kill(null);
                }
                else
                {
                    this.parent.StartCooldown(this.parent.def.cooldownTicksRange.min);
                }
                if (this.parent.CooldownTicksRemaining > this.parent.def.cooldownTicksRange.max)
                {
                    this.parent.StartCooldown(this.parent.def.cooldownTicksRange.max);
                }
            }
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return target.Pawn != null && (target.Pawn.story == null || !PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(target.Pawn)) && (target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > 1f || target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= float.Epsilon || Rand.Value < this.Props.aiUseFrequencyOnMundanes || (target.Pawn.HasPsylink && target.Pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon));
        }
    }
    /*technically, having 0% psysens disables your psychic entropy tracker, removing all your psysens. The post add/removed stuff is redundant, but who knows, maybe a mod out there makes it necessary to specify.
     * Tooltip is "Meditate for {0} to cure". Meditation to reduce severity is handled by a comp.*/
    public class Hediff_Censure : HediffWithComps
    {
        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "HVT_ErinysCensureTooltip".Translate(((int)this.Severity).ToStringTicksToPeriod(true, true, true, true, true));
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.psychicEntropy != null)
            {
                this.pawn.psychicEntropy.OffsetPsyfocusDirectly(-100f);
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.pawn.psychicEntropy != null)
            {
                this.pawn.psychicEntropy.OffsetPsyfocusDirectly(-100f);
            }
        }
    }
    /*If a Sphinx's ability fails to grant you a psylink, you gain a stack of the leer instead. Each stack past the 1st is +1 severity, and you die at >=2, so you die at 3 stacks.
     * It is intended to persist past death and resurrection. On death we reduce its severity to just a smidge below 2 since otherwise you'd just die again after being resurrected (and also because resurrection should only buy you one more attempt
     * to be psychically instructed, not a full slate of new tries).*/
    public class Hediff_LeeringSphinx : Hediff
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity >= 2f)
            {
                this.pawn.Kill(null);
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.Severity >= 2f)
            {
                this.Severity = 1.99f;
            }
        }
    }
}
