using HarmonyLib;
using HautsTraits;
using System;
using Verse;
using Vehicles;
using Vehicles.World;

namespace HVT_VehicleFramework
{
    [StaticConstructorOnStartup]
    public static class HVT_VehicleFramework
    {
        private static readonly Type patchType = typeof(HVT_VehicleFramework);
        static HVT_VehicleFramework()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautstraits.vehicleframework");
            if (ModsConfig.OdysseyActive)
            {
                harmony.Patch(AccessTools.Method(typeof(VehicleSkyfaller_Arriving), nameof(VehicleSkyfaller_Arriving.FinalizeLanding)),
                               postfix: new HarmonyMethod(patchType, nameof(HVTFinalizeLandingPostfix)));
                harmony.Patch(AccessTools.Method(typeof(AerialVehicleInFlight), nameof(AerialVehicleInFlight.SwitchToCaravan)),
                               prefix: new HarmonyMethod(patchType, nameof(HVTSwitchToCaravanPrefix)));
            }
        }
        //when aerial vehicles finish deploying, Skybound and Earthborne pawns should get the appropriate moodlets (and grav nausea in the latter case)
        public static void HVTFinalizeLandingPostfix(VehicleSkyfaller_Arriving __instance)
        {
            if (__instance.vehicle != null && __instance.vehicle.AllPawnsAboard != null)
            {
                foreach (Pawn pawn in __instance.vehicle.AllPawnsAboard)
                {
                    HVTUtility.DoAerospaceFlyingThoughts(pawn);
                }
            }
        }
        public static void HVTSwitchToCaravanPrefix(AerialVehicleInFlight __instance)
        {
            if (__instance.Vehicle != null && __instance.Vehicle.AllPawnsAboard != null)
            {
                foreach (Pawn pawn in __instance.Vehicle.AllPawnsAboard)
                {
                    HVTUtility.DoAerospaceFlyingThoughts(pawn);
                }
            }
        }
    }
}
