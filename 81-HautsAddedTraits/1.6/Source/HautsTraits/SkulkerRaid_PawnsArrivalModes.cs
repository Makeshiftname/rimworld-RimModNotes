using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HautsTraits
{
    /*Since these are all Skulker raids, the mod option to disable skulker raids prevents these from being usable. Sabotage and Assasins are also prevented by the disabling of the "most unfair Skulker raids" setting.
     * picks a spot like a random but non-haywire drop pod raid. However, they just appear there, and roughly two-thirds of them (min 1) are Skulkers. Used as a possible but unlikely PAM for non-mechanoid skulker raids*/
    public class PawnsArrivalModeWorker_SkulkIn : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Pawn> pawnList = pawns.InRandomOrder().ToList();
            for (int i = 0; i < pawnList.Count; i++)
            {
                if (pawnList[i].story != null && !pawnList[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawnList[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            IntVec3 loc;
            Faction hostFaction = map.ParentFaction ?? Faction.OfPlayer;
            IEnumerable<Thing> enumerable = map.mapPawns.FreeHumanlikesSpawnedOfFaction(hostFaction).Cast<Thing>();
            if (hostFaction == Faction.OfPlayer)
            {
                enumerable = enumerable.Concat(map.listerBuildings.allBuildingsColonist.Cast<Thing>());
            } else {
                enumerable = enumerable.Concat(from x in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                                               where x.Faction == hostFaction
                                               select x);
            }
            int num = 0;
            float num2 = 65f;
            IntVec3 intVec = IntVec3.Invalid;
            foreach (IntVec3 iv3 in map.AllCells.Where((IntVec3 c) => c.Standable(map) && !c.GetTerrain(map).dangerous && !c.Fogged(map)).InRandomOrder())
            {
                num++;
                if (num > 300)
                {
                    intVec = iv3;
                }
                num2 -= 0.2f;
                bool flag = false;
                foreach (Thing thing in enumerable)
                {
                    if ((float)(intVec - thing.Position).LengthHorizontalSquared < num2 * num2)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag && map.reachability.CanReachFactionBase(intVec, hostFaction))
                {
                    intVec = iv3;
                }
            }
            if (!intVec.IsValid)
            {
                intVec = CellFinderLoose.RandomCellWith((IntVec3 c) => c.Standable(map) && !c.GetTerrain(map).dangerous && !c.Fogged(map), map, 1000);
            }
            loc = intVec;
            GenSpawn.Spawn(pawns[0], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            if (pawns.Count > 1)
            {
                for (int i = 1; i < pawns.Count; i++)
                {
                    IntVec3 loc2 = CellFinder.RandomClosewalkCellNear(loc, map, 3, (IntVec3 c) => c.Standable(map) && !c.GetTerrain(map).dangerous && !c.Fogged(map));
                    GenSpawn.Spawn(pawns[i], loc2, map, parms.spawnRotation, WipeMode.Vanish, false);
                }
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            if (HVT_Mod.settings.disableStealthRaids)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
            }
            Map map = (Map)parms.target;
            if (parms.attackTargets != null && parms.attackTargets.Count > 0)
            {
                CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => !map.roofGrid.Roofed(p) && p.Walkable(map), map, CellFinder.EdgeRoadChance_Hostile, out parms.spawnCenter);
            }
            if (!parms.spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Hostile, false, null))
            {
                return false;
            }
            parms.spawnRotation = Rot4.FromAngleFlat((map.Center - parms.spawnCenter).AngleFlat);
            return true;
        }
    }
    //spawns all the pawns near a building of yours. Also, most of them are Skulkers. Used by Espionage and Burglary
    public class PawnsArrivalModeWorker_SkulkInBaseCluster : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 3, null);
                while (!loc.Walkable(map))
                {
                    loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 3, null);
                }
                GenSpawn.Spawn(pawns[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || flag || map.listerBuildings.allBuildingsColonist.Count == 0)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                do
                {
                    if (map.listerBuildings.allBuildingsColonist.Count > 0)
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        toSpawn = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, (IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous && !p.Fogged(map));
                    } else {
                        CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous, map, CellFinder.EdgeRoadChance_Hostile, out toSpawn);
                    }
                } while (!toSpawn.Standable(map));
                parms.spawnCenter = toSpawn;
            }
            return true;
        }
    }
    //spawns each pawn near a random building of yours. Most Skulkers. The other possible PAM for Espionage or Burglary
    public class PawnsArrivalModeWorker_SkulkInBaseSplitUp : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            GenSpawn.Spawn(pawns[0], parms.spawnCenter, map, parms.spawnRotation, WipeMode.Vanish, false);
            if (pawns.Count > 1)
            {
                for (int i = 1; i < pawns.Count; i++)
                {
                    IntVec3 loc2;
                    do
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        loc2 = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        loc2 = CellFinder.RandomClosewalkCellNear(loc2, map, 2, null);
                    } while (!loc2.Walkable(map));
                    GenSpawn.Spawn(pawns[i], loc2, map, parms.spawnRotation, WipeMode.Vanish, false);
                }
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || flag || map.listerBuildings.allBuildingsColonist.Count == 0)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                do
                {
                    if (map.listerBuildings.allBuildingsColonist.Count > 0)
                    {
                        int randomCell = (int)(Rand.Value * map.listerBuildings.allBuildingsColonist.Count);
                        toSpawn = map.listerBuildings.allBuildingsColonist[randomCell].Position;
                        toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, (IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous && !p.Fogged(map));
                    } else {
                        CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous, map, CellFinder.EdgeRoadChance_Hostile, out toSpawn);
                    }
                } while (!toSpawn.Standable(map));
                parms.spawnCenter = toSpawn;
            }
            return true;
        }
    }
    /*derivative of SplitUp in which each pawn also spawns with a nearby, lit-wick IED. These 'sabotage mines' take hours to go off (unless touched, then they blooey pretty fast, but your pawns try really hard not to
     * walk on them). This is the only possible PAM for Sabotage, obviously, since it's got the defining feature.*/
    public class PawnsArrivalModeWorker_SabotagePAM : PawnsArrivalModeWorker_SkulkInBaseSplitUp
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            base.Arrive(pawns, parms);
            Map map = (Map)parms.target;
            int timer = 10000 + (2500 * (int)Math.Ceiling(Rand.Value * 5));
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawns[i].Position, map, 6, null);
                while (loc == parms.spawnCenter)
                {
                    loc = CellFinder.RandomClosewalkCellNear(pawns[i].Position, map, 6, null);
                }
                Building_TrapExplosive ied;
                if (Rand.Value <= 0.01f)
                {
                    ied = (Building_TrapExplosive)ThingMaker.MakeThing(HVTDefOf.Hauts_SabotageIED_AntigrainWarhead, null);
                } else {
                    ied = (Building_TrapExplosive)ThingMaker.MakeThing(HVTDefOf.Hauts_SabotageIED_HighExplosive, null);
                }
                ied.SetFactionDirect(parms.faction);
                ied.HitPoints = ied.def.BaseMaxHitPoints;
                GenSpawn.Spawn(ied, loc, map, WipeMode.Vanish);
                ied.GetComp<CompExplosive>().StartWick(null);
                ied.GetComp<CompExplosive>().wickTicksLeft = timer;
            }
        }
    }
    /*scry n die (Ex)
     * for fairness' sake (well, actually because I didn't know how to force a job while I was making Assassins, but also I guess for fairness' sake) nothing actually tells them they HAVE to attack the pawn they
     * spawn around. It's emergently likely, but not guaranteed.*/
    public class PawnsArrivalModeWorker_Assassins : PawnsArrivalModeWorker
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms);
        }
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
                return;
            }
            Map map = (Map)parms.target;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].story != null && !pawns[i].story.traits.HasTrait(HVTDefOf.HVT_Skulker))
                {
                    if (Rand.Value < 0.66f || i == 1)
                    {
                        pawns[i].story.traits.GainTrait(new Trait(HVTDefOf.HVT_Skulker, 0, true));
                    }
                }
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                while (loc == parms.spawnCenter)
                {
                    loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 6, null);
                }
                GenSpawn.Spawn(pawns[i], loc, map, parms.spawnRotation, WipeMode.Vanish, false);
            }
        }
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            bool flag = parms.faction != null && parms.faction == Faction.OfMechanoids;
            Map map = (Map)parms.target;
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || flag || map.mapPawns.FreeColonistsAndPrisoners.Count == 0)
            {
                parms.raidArrivalMode = DefDatabase<PawnsArrivalModeDef>.GetNamed("RandomDrop");
                parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
                return true;
            }
            if (!parms.spawnCenter.IsValid)
            {
                IntVec3 toSpawn;
                int randomCell = (int)(Rand.Value * map.mapPawns.FreeColonistsAndPrisoners.Count);
                toSpawn = map.mapPawns.FreeColonistsAndPrisoners[randomCell].Position;
                toSpawn = CellFinder.RandomClosewalkCellNear(toSpawn, map, 2, (IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous && !p.Fogged(map));
                parms.spawnCenter = toSpawn;
                if (!parms.spawnCenter.IsValid)
                {
                    CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => p.Standable(map) && !p.GetTerrain(map).dangerous, map, CellFinder.EdgeRoadChance_Hostile, out parms.spawnCenter);
                }
            }
            return true;
        }
    }
}
