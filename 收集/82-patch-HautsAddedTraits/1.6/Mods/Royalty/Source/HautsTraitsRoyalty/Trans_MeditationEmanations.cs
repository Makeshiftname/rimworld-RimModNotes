using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    //while meditating, Albatrosses generate an invisible object on their location that, like a tornado, pushes the current map's wind speed upward. If there already is one, just refreshes its duration and increases its wind speed.
    public class Hediff_CrossroadsPartII : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25, delta))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.pawn.Spawned)
                    {
                        Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_AlbatrossSquall);
                        if (t != null)
                        {
                            CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                            if (cdad != null)
                            {
                                cdad.spawnTick = Find.TickManager.TicksGame;
                            }
                            CompWindSource cws = t.TryGetComp<CompWindSource>();
                            if (cws != null)
                            {
                                cws.wind = Math.Max(2f, cws.wind + 0.025f);
                            }
                        }
                        else
                        {
                            GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_AlbatrossSquall, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                        }
                    }
                }
            }
        }
    }
    //Albatross squalls' internal wind speed should be the current wind speed whenever they are first created or the game is reloaded
    public class DoTheWindyThing : ThingWithComps
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (map != null)
            {
                CompWindSource cws = this.GetComp<CompWindSource>();
                if (cws != null)
                {
                    cws.wind = map.windManager.WindSpeed;
                }
            }
        }
    }
    /*Bowerbirds generate a small skipshield while meditating (or refresh the duration of an extant one at their position). Every quarter hours, it inflicts a small heal on nearby organic pawns (heal scales w/ pawn's psysens).
     * If somehow meditating in a caravan, heal applies to all organic pawns in caravan.
     * To make the most of this in combat, deploy a meditation spot, assign the Bowerbird to it, schedule them for meditation, set the desired psyfocus to 100%, and turn their hostility response to Ignore so they'll meditate even w hostiles nearby.*/
    public class Hediff_Bower : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25, delta))
            {
                if (this.Severity >= 0.5f && this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.pawn.Spawned)
                    {
                        Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_BowerShield);
                        if (t != null)
                        {
                            CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                            if (cdad != null)
                            {
                                cdad.spawnTick = Find.TickManager.TicksGame;
                            }
                        }
                        else
                        {
                            GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_BowerShield, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                        }
                        if (this.pawn.IsHashIntervalTick(625, delta))
                        {
                            if (this.pawn.Spawned)
                            {
                                foreach (Pawn p in this.pawn.Map.mapPawns.AllPawnsSpawned)
                                {
                                    if (p.RaceProps.IsFlesh && p.Position.DistanceTo(this.pawn.Position) <= 2.5f)
                                    {
                                        HautsMiscUtility.StatScalingHeal(1f, StatDefOf.PsychicSensitivity, p, p);
                                    }
                                }
                            }
                            else if (this.pawn.GetCaravan() != null)
                            {
                                foreach (Pawn p in this.pawn.GetCaravan().pawns.InnerListForReading)
                                {
                                    HautsMiscUtility.StatScalingHeal(2f, StatDefOf.PsychicSensitivity, p, p);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    //While meditating, Firehawks generate an invisible object on their location that pushes heat. If one already exists, refresh its duration
    public class Hediff_InnerWildfire : Hediff_HasExtraOnHitEffects
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25))
            {
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating)
                {
                    if (this.pawn.Spawned)
                    {
                        Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_InnerWildfire);
                        if (t != null)
                        {
                            CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                            if (cdad != null)
                            {
                                cdad.spawnTick = Find.TickManager.TicksGame;
                            }
                        }
                        else
                        {
                            GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_InnerWildfire, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                        }
                    }
                }
            }
        }
    }
    //Gnats release smoke explosions while meditating and not already in smoke. Since they go invisible in smoke, this is essentially free, if micro-heavy, invisibility (see Bowerbird comments).
    public class Hediff_BlotOutTheSun : Hediff_HasExtraOnHitEffects
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25, delta) && this.pawn.psychicEntropy.IsCurrentlyMeditating && this.pawn.Spawned && this.pawn.Map.gasGrid.DensityAt(this.pawn.Position, GasType.BlindSmoke) <= 80)
            {
                GenExplosion.DoExplosion(this.pawn.PositionHeld, this.pawn.MapHeld, 2.9f, DamageDefOf.Smoke, null, -1, -1f, SoundDefOf.Psycast_Skip_Pulse, null, null, null, null, 0f, 1, new GasType?(GasType.BlindSmoke), null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
            }
        }
    }
    /*Poluxes (Poluxi?) alter the terrain around them whenever this hediff reaches max severity (which occurs by meditating for long enough due to one of its comps, after which it goes to min severity).
     * It can turn sea ice into regular ice, or wet terrain to its corresponding dry terrain. It removes Biotech pollution, and turns terraformable non-water non-space non-floor non-rough stone to terrain of a higher fertility,
     * using the natural terrain of the biome that would be next most fertile*/
    public class Hediff_Polux : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity >= this.def.maxSeverity && this.pawn.Spawned)
            {
                this.Severity = this.def.initialSeverity;
                int cells = GenRadial.NumCellsInRadius(10f);
                for (int i = 0; i < cells; i++)
                {
                    bool done = false;
                    IntVec3 c = this.pawn.Position + GenRadial.RadialPattern[i];
                    if (c.InBounds(this.pawn.Map) && c.IsValid)
                    {
                        TerrainDef td = this.pawn.Map.terrainGrid.TerrainAt(c);
                        if (this.pawn.Map.Biome == BiomeDefOf.SeaIce)
                        {
                            this.pawn.Map.terrainGrid.SetTerrain(c, TerrainDefOf.Ice);
                            done = true;
                        }
                        else if (td.driesTo != null)
                        {
                            this.pawn.Map.terrainGrid.SetTerrain(c, td.driesTo);
                            done = true;
                        }
                        TerrainDef utd = this.pawn.Map.terrainGrid.UnderTerrainAt(c);
                        if (utd != null)
                        {
                            if (this.pawn.Map.Biome == BiomeDefOf.SeaIce)
                            {
                                this.pawn.Map.terrainGrid.SetUnderTerrain(c, TerrainDefOf.Ice);
                                done = true;
                            }
                            else if (td.driesTo != null)
                            {
                                this.pawn.Map.terrainGrid.SetUnderTerrain(c, td.driesTo);
                                done = true;
                            }
                        }
                        if (c.CanUnpollute(this.pawn.Map))
                        {
                            this.pawn.Map.pollutionGrid.SetPolluted(c, false, false);
                            done = true;
                        }
                        if (!td.IsFloor && !td.affordances.Contains(TerrainAffordanceDefOf.SmoothableStone) && !td.IsRiver && !td.IsWater && !td.isFoundation && td.canEverTerraform && (td.tags == null || !td.tags.Contains("Space")))
                        {
                            List<TerrainDef> tdList = HautsMiscUtility.FertilityTerrainDefs(this.pawn.Map, false);
                            if (!tdList.NullOrEmpty())
                            {
                                IOrderedEnumerable<TerrainDef> source = from e in tdList.FindAll((TerrainDef e) => (double)e.fertility > (double)td.fertility && !e.IsWater && !e.IsRiver)
                                                                        orderby e.fertility
                                                                        select e;
                                if (source.Count<TerrainDef>() != 0)
                                {
                                    TerrainDef newTerr = source.First<TerrainDef>();
                                    this.pawn.Map.terrainGrid.SetTerrain(c, newTerr);
                                    done = Rand.Chance(0.2f);
                                }
                            }
                        }
                    }
                    if (done)
                    {
                        SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(this.pawn.Position, this.pawn.Map, false));
                        return;
                    }
                }
            }
        }

    }
    //Meditating, Sea Ice transes generate an invisible object that pushes cold, or refresh the duration of an already extant one
    public class Hediff_ColdColdHeart : Hediff_HasExtraOnHitEffects
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25, delta))
            {
                Hediff hypo = this.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
                if (hypo != null)
                {
                    this.pawn.health.RemoveHediff(hypo);
                }
                if (this.pawn.psychicEntropy != null && this.pawn.psychicEntropy.IsCurrentlyMeditating && this.pawn.Spawned)
                {
                    Thing t = this.pawn.Position.GetFirstThing(this.pawn.Map, HVTRoyaltyDefOf.HVT_ColdColdHeart);
                    if (t != null)
                    {
                        CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                        if (cdad != null)
                        {
                            cdad.spawnTick = Find.TickManager.TicksGame;
                        }
                    }
                    else
                    {
                        GenSpawn.Spawn(HVTRoyaltyDefOf.HVT_ColdColdHeart, this.pawn.Position, this.pawn.Map, WipeMode.Vanish);
                    }
                }
            }
        }
    }
}
