using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace HautsTraits
{
    /*CanUseWith restrictions prevent natural assignation of these strat workers to raids if the relevant mod settings are disabling them.
     * However, it does nothing against dev mode and so therefore may not be proof against other mods' means of stimulating raids.
     * Thus the MakeLordJob overrides ALSO enforcing those mod settings, at the very least turning their intended Lords into generic colony assaults.*/
    public class RaidStrategyWorker_Burglary : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_StealFromColony(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Espionage : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Espionage(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Assassinate : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Assassinate();
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind);
        }
    }
    public class RaidStrategyWorker_Sabotage : RaidStrategyWorker
    {
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            if (HVT_Mod.settings.disableStealthRaids || HVT_Mod.settings.disableHardStealthRaids || map.listerBuildings.allBuildingsColonist.Count < 1)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                return new LordJob_AssaultColony(parms.faction, true, parms.canTimeoutOrFlee, false, false, true, false, false);
            }
            return new LordJob_Sabotage(parms.faction, true, parms.canTimeoutOrFlee, false, false);
        }
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return !HVT_Mod.settings.disableStealthRaids && !HVT_Mod.settings.disableHardStealthRaids && parms.faction != null && parms.faction.def.humanlikeFaction && base.CanUseWith(parms, groupKind) && parms.faction.def.techLevel >= TechLevel.Industrial;
        }
    }
}
