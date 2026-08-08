using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using VEF.Apparels;
using Verse;

namespace HautsTraits
{
    /*rambunctious uses a hediffcomp instead of traits' native random mb function because that can't trigger while the pawn is sleeping. Rambunctious is SUPPOSED to be able to trigger while asleep*/
    public class HediffCompProperties_WhyAreYouRunning : HediffCompProperties
    {
        public HediffCompProperties_WhyAreYouRunning()
        {
            this.compClass = typeof(HediffComp_WhyAreYouRunning);
        }
        public MentalStateDef mentalState;
        public Dictionary<JobDef, float> triggeringJobs;
    }
    public class HediffComp_WhyAreYouRunning : HediffComp
    {
        public HediffCompProperties_WhyAreYouRunning Props
        {
            get
            {
                return (HediffCompProperties_WhyAreYouRunning)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta))
            {
                Pawn p = this.Pawn;
                if (p.Spawned && p.CurJobDef != null && !p.pather.Moving)
                {
                    if (!this.Props.triggeringJobs.NullOrEmpty() && this.Props.triggeringJobs.Keys.Contains(p.CurJobDef))
                    {
                        this.Props.triggeringJobs.TryGetValue(p.CurJobDef, out float chance);
                        if (Rand.MTBEventOccurs(chance, 60000f, 150f) && this.Props.mentalState.Worker.StateCanOccur(p))
                        {
                            p.mindState.mentalStateHandler.TryStartMentalState(this.Props.mentalState, null, true, true, false, null, false, false, false);
                        }
                    }
                }
            }
        }
    }
    //repressed rage go berserk on taking damage sometimes
    public class Hediff_HulkSmash : HediffWithComps
    {
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (this.pawn.InMentalState || (Rand.Value < 0.05f && this.pawn.needs != null && this.pawn.needs.mood != null && this.pawn.needs.mood.CurLevel <= this.pawn.mindState.mentalBreaker.BreakThresholdMinor))
            {
                this.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "HVT_RROnDamage".Translate().CapitalizeFirst(), true, true, false, null, false, true);
            }
        }
    }
    //sunbather's hidden hediff governs its mood/work speed effect. The sunlight condition is handled here
    public class Hediff_BaskInTheSun : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.SpawnedOrAnyParentSpawned)
            {
                IntVec3 iv3 = this.pawn.PositionHeld;
                if (iv3.IsValid && iv3.InBounds(this.pawn.MapHeld))
                {
                    Map m = this.pawn.MapHeld;
                    if (!m.roofGrid.Roofed(iv3) && m.skyManager.CurSkyGlow > 0.3f)
                    {
                        this.Severity = this.def.maxSeverity;
                        return;
                    }
                    else
                    {
                        this.Severity = this.def.minSeverity;
                        return;
                    }
                }
                PlanetTile pt = this.pawn.Tile;
                if (pt != null && GenCelestial.CelestialSunGlow(pt, Find.TickManager.TicksAbs) > 0.3f)
                {
                    this.Severity = this.def.maxSeverity;
                    return;
                }
                this.Severity = this.def.minSeverity;
            }
        }
        public override void PostTick()
        {
            base.PostTick();
        }
    }
    //switches the dummy tranquil trait for the real one
    public class Hediff_TYNAN : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.story != null && this.pawn.Spawned)
            {
                if ((this.pawn.Faction != Faction.OfPlayerSilentFail && (this.pawn.Faction.HostileTo(Faction.OfPlayerSilentFail) || (this.pawn.equipment != null && this.pawn.equipment.Primary != null))) || this.pawn.kindDef.requiredWorkTags == WorkTags.Violent)
                {
                    if (this.pawn.story.traits.allTraits.Count == 1)
                    {
                        this.pawn.story.traits.GainTrait(new Trait(TraitDefOf.Kind), true);
                    }
                }
                else if (!this.pawn.story.traits.HasTrait(HVTDefOf.HVT_Tranquil))
                {
                    this.pawn.story.traits.GainTrait(new Trait(HVTDefOf.HVT_Tranquil));
                }
                else
                {
                    this.pawn.health.RemoveHediff(this);
                }
                for (int i = this.pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.story.traits.allTraits[i].def == HVTDefOf.HVT_Tranquil0)
                    {
                        this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.allTraits[i]);
                        break;
                    }
                }
            }
        }
    }
    //weapon artist buffs
    public class HediffCompProperties_ColinWallis : HediffCompProperties
    {
        public HediffCompProperties_ColinWallis()
        {
            this.compClass = typeof(HediffComp_ColinWallis);
        }
        public Dictionary<WeaponClassDef, HediffDef> hediffPerClass;
    }
    public class HediffComp_ColinWallis : HediffComp
    {
        public HediffCompProperties_ColinWallis Props
        {
            get
            {
                return (HediffCompProperties_ColinWallis)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(15, delta))
            {
                List<WeaponClassDef> wcds = new List<WeaponClassDef>();
                if (this.Pawn.equipment != null)
                {
                    ThingWithComps twc = this.Pawn.equipment.Primary;
                    if (twc != null)
                    {
                        wcds = twc.def.weaponClasses;
                    }
                }
                List<HediffDef> validWeaponArts = new List<HediffDef>();
                foreach (WeaponClassDef wcd in wcds)
                {
                    if (this.Props.hediffPerClass.ContainsKey(wcd))
                    {
                        HediffDef hd = this.Props.hediffPerClass.TryGetValue(wcd);
                        validWeaponArts.Add(hd);
                        this.Pawn.health.AddHediff(hd);
                    }
                }
                List<Hediff> toRemove = new List<Hediff>();
                foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
                {
                    if (h is Hediff_WeaponArt)
                    {
                        if (!validWeaponArts.Contains(h.def))
                        {
                            toRemove.Add(h);
                        }
                    }
                }
                foreach (Hediff h in toRemove)
                {
                    this.Pawn.health.RemoveHediff(h);
                }
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            List<Hediff> toRemove = new List<Hediff>();
            foreach (Hediff h in this.Pawn.health.hediffSet.hediffs)
            {
                if (h is Hediff_WeaponArt)
                {
                    toRemove.Add(h);
                }
            }
            foreach (Hediff h in toRemove)
            {
                this.Pawn.health.RemoveHediff(h);
            }
        }
    }
    //WA's hediff comp will only let you have one at a time
    public class Hediff_WeaponArt : HediffWithComps
    {

    }
    //switches what melee weapon buff you get depending on if VEF says you can wield that weapon with a shield or not. (Most weapons can be wielded with shields, which is NOT how I remember it used to work, but whatever)
    public class Hediff_MeleeWeaponArt : Hediff_WeaponArt
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.HanderCheck();
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            this.HanderCheck();
        }
        public void HanderCheck()
        {
            if (this.pawn.equipment != null)
            {
                ThingWithComps twc = this.pawn.equipment.Primary;
                if (twc != null)
                {
                    if (!twc.def.UsableWithShields())
                    {
                        this.Severity = this.def.maxSeverity;
                    }
                    else
                    {
                        this.Severity = this.def.minSeverity;
                    }
                }
            }
        }
    }
    //this WA buff is specifically for the Stun on Hit effect
    public class Hediff_WeaponArtExtraOnHitEffects : Hediff_WeaponArt
    {
        public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            base.Notify_PawnDamagedThing(thing, dinfo, result);
            HautsMiscUtility.DoExtraOnHitEffects(this, thing, dinfo, result);
        }
    }
    //dream spectrum functionality. read the dang XML if you need a sense of what the fields do
    public class HediffCompProperties_Oneiromancy : HediffCompProperties
    {
        public HediffCompProperties_Oneiromancy()
        {
            this.compClass = typeof(HediffComp_Oneiromancy);
        }
        public ThoughtDef thought;
        public int ticksPerStack;
        public int checkInterval = 60;
        public float numStacksToTrigger;
        public float inspirationChancePerStack;
        public float maxInspirationChance;
        public string inspireString = "HVT_VividDreamerInspired";
    }
    public class HediffComp_Oneiromancy : HediffComp
    {
        public HediffCompProperties_Oneiromancy Props
        {
            get
            {
                return (HediffCompProperties_Oneiromancy)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            Pawn p = this.Pawn;
            if (p.IsHashIntervalTick(this.Props.checkInterval, delta) && p.needs.mood != null)
            {
                Need_Rest nr = p.needs.rest;
                if (nr != null)
                {
                    if (nr.Resting)
                    {
                        this.parent.Severity += this.Props.checkInterval;
                    }
                    else
                    {
                        float sev = this.parent.Severity;
                        if (sev / this.Props.ticksPerStack >= this.Props.numStacksToTrigger)
                        {
                            if (p.mindState.inspirationHandler != null && Rand.Chance(Math.Min(this.Props.inspirationChancePerStack * sev / this.Props.ticksPerStack, this.Props.maxInspirationChance)))
                            {
                                InspirationDef randomAvailableInspirationDef = p.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                                if (randomAvailableInspirationDef != null)
                                {
                                    TaggedString reasonText = this.Props.inspireString.Translate().CapitalizeFirst().Formatted(p.Named("PAWN")).AdjustedFor(p, "PAWN", true).Resolve();
                                    p.mindState.inspirationHandler.TryStartInspiration(randomAvailableInspirationDef, reasonText, true);
                                }
                            }
                            while (sev > this.Props.ticksPerStack)
                            {
                                Thought_Memory thought_Memory = ThoughtMaker.MakeThought(this.Props.thought, null);
                                p.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, null);
                                sev -= this.Props.ticksPerStack;
                            }
                        }
                        this.parent.Severity = this.parent.def.minSeverity;
                    }
                }
            }
        }
    }
    //expectation spectrum (specifically prideful and entitled) - hediff activates once pawn's personal expectation level (set for these two traits via Harmony patch to be High at minimum in most player-relevant cases) is met by the map's expectation level
    public class Hediff_Pride : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.MapHeld != null)
            {
                if (ExpectationsUtility.CurrentExpectationFor(this.pawn.MapHeld).order >= ExpectationsUtility.CurrentExpectationFor(this.pawn).order)
                {
                    this.Severity = 1.001f;
                }
                else
                {
                    this.Severity = 0.001f;
                }
            }
        }
    }
}
