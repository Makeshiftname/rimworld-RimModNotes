using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using HarmonyLib;
using HautsTraits;
using MonoMod.Utils;
using RimWorld.Planet;
using VanillaPsycastsExpanded;
using VFECore.Abilities;
using HautsTraitsRoyalty;
using HautsFramework;
using HautsFrameworkVPE;
using VanillaPsycastsExpanded.Harmonist;
using static UnityEngine.GraphicsBuffer;
using System.Security.Cryptography;

namespace Hauts_VPE
{
    [StaticConstructorOnStartup]
    public static class Hauts_VPE
    {
        private static readonly Type patchType = typeof(Hauts_VPE);
        static Hauts_VPE()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsvpecompatibility.main");
            harmony.Patch(AccessTools.Method(typeof(Hediff_IncarnatePsycastsKnown), nameof(Hediff_IncarnatePsycastsKnown.VPECompat)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_Hediff_IncarnatePsycastsKnownPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.TotalPsyfocusRefund)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_TotalPsyfocusRefundPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.GetPsyfocusUsedByPawn)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_GetPsyfocusUsedByPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.ValidateTarget)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_ValidateTargetPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VFECore.Abilities.Ability) }),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_CastPrefix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VFECore.Abilities.Ability) }),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_CastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_GainTraitPrefix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_JoinFaction), nameof(AbilityExtension_JoinFaction.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VFECore.Abilities.Ability)}),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_JoinFactionCastPrefix)));
            harmony.Patch(AccessTools.Method(typeof(HediffComp_MindControl), nameof(HediffComp_MindControl.CompPostPostAdd)),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_MindControl_CompPostPostPrefix1)));
            harmony.Patch(AccessTools.Method(typeof(HediffComp_MindControl), nameof(HediffComp_MindControl.CompPostPostRemoved)),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_MindControl_CompPostPostPrefix2)));
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HVT_VPE_Hediff_IncarnatePsycastsKnownPostfix(ref float __result, Hediff_IncarnatePsycastsKnown __instance)
        {
            CompAbilities comp = __instance.pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                float psycastsKnown = 0f;
                foreach (VFECore.Abilities.Ability a in comp.LearnedAbilities)
                {
                    if (a.def.HasModExtension<AbilityExtension_Psycast>())
                    {
                        psycastsKnown += 1f;
                    }
                }
                __result = psycastsKnown;
            }
        }
        public static void HVT_VPE_TotalPsyfocusRefundPostfix(ref float __result, Pawn pawn, float psyfocusCost, bool isWord, bool isSkip)
        {
            if (isWord && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitNightingale))
            {
                __result = Math.Max(__result, Math.Max(0f,psyfocusCost - 0.1f));
            }
        }
        public static void HVT_VPE_GetPsyfocusUsedByPawnPostfix(ref float __result, AbilityExtension_Psycast __instance, Pawn pawn)
        {
            if (__instance.abilityDef.abilityClass == typeof(Ability_WordOf) && pawn != null && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird))
            {
                __result += 0.075f;
                if (__result > 1f)
                {
                    __result = 1f;
                }
            }
        }
        public static void HVT_VPE_ValidateTargetPostfix(ref bool __result, LocalTargetInfo target, VFECore.Abilities.Ability ability, bool throwMessages = false)
        {
            if (target.Thing != null && target.Thing is Pawn p && p.story != null && p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDragon) && ability.pawn.HostileTo(p))
            {
                if (throwMessages)
                {
                    Messages.Message("Ineffective".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                __result = false;
            }
        }
        public static void HVT_VPE_CastPrefix(AbilityExtension_Psycast __instance, GlobalTargetInfo[] targets, VFECore.Abilities.Ability ability, out List<Thing> __state)
        {
            __state = new List<Thing>();
            if (ability.pawn != null && ability.pawn.story != null)
            {
                Pawn pawn = ability.pawn;
                if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird) && targets[0].Thing is Pawn target && target.health.hediffSet.HasHediff(VPE_DefOf.VPE_GroupLink))
                {
                    target.health.RemoveHediff(target.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_GroupLink));
                }
                if (HautsUtility.IsSkipAbility(__instance.abilityDef) && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitGlowworm))
                {
                    List<IntVec3> iv3s = new List<IntVec3>();
                    for (int i = 0; i < targets.Length; i += 2)
                    {
                        if (targets[i].Cell != null && targets[i].Cell.InBounds(targets[i].Map) && targets.Length > i+1 && targets[i+1].Cell != null && targets[i + 1].Cell.InBounds(targets[i].Map) && targets[i].Map == targets[i+1].Map)
                        {
                            if (targets[i].Cell != targets[i+1].Cell)
                            {
                                foreach (IntVec3 bres in GenSight.BresenhamCellsBetween(targets[i].Cell, targets[i+1].Cell))
                                {
                                    foreach (IntVec3 bres3 in GenRadial.RadialCellsAround(bres, 1.42f, true))
                                    {
                                        if (!iv3s.Contains(bres3) && bres3.InBounds(pawn.Map))
                                        {
                                            iv3s.Add(bres3);
                                        }
                                    }
                                }
                            } else {
                                foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(targets[i].Cell, Math.Min(6f, pawn.GetPsylinkLevel()), true))
                                {
                                    if (!iv3s.Contains(iv3) && iv3.InBounds(pawn.Map))
                                    {
                                        iv3s.Add(iv3);
                                    }
                                }
                            }
                        } else {
                            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(targets[i].Cell, 1.42f, true))
                            {
                                if (!iv3s.Contains(iv3) && iv3.InBounds(pawn.Map))
                                {
                                    iv3s.Add(iv3);
                                }
                            }
                        }
                    }
                    List<Thing> things = new List<Thing>();
                    foreach (IntVec3 toHit in iv3s)
                    {
                        foreach (Thing thing in toHit.GetThingList(pawn.Map))
                        {
                            things.Add(thing);
                        }
                    }
                    __state = things;
                }
            }
        }
        public static void HVT_VPE_CastPostfix(AbilityExtension_Psycast __instance, GlobalTargetInfo[] targets, VFECore.Abilities.Ability ability, List<Thing> __state)
        {
            Pawn pawn = ability.pawn;
            for (int i = __state.Count - 1; i >= 0; i--)
            {
                Thing t = __state[i];
                if ((targets[0].Thing == null || t != targets[0].Thing) && (t.def.useHitPoints || t is Pawn) && (pawn.HostileTo(t) || t.Faction == null))
                {
                    Vector3 vfxOffset = new Vector3((Rand.Value - 0.5f), (Rand.Value - 0.5f), (Rand.Value - 0.5f));
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(t.Position.ToVector3Shifted() + vfxOffset, pawn.Map, FleckDefOf.PsycastSkipInnerExit, 0.3f);
                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    pawn.Map.flecks.CreateFleck(dataStatic);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(t.Position.ToVector3Shifted() + vfxOffset, pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 0.3f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    pawn.Map.flecks.CreateFleck(dataStatic2);
                    t.TakeDamage(new DamageInfo(HautsDefOf.Hauts_SkipFrag, 5f, 999f, -1, pawn, t is Pawn p ? p.health.hediffSet.GetRandomNotMissingPart(HautsDefOf.Hauts_SkipFrag) : null));
                }
            }
            if (pawn != null && pawn.psychicEntropy != null && !pawn.health.hediffSet.HasHediff(HautsDefOf.Hauts_PsycastLoopBreaker))
            {
                if (pawn.story != null)
                {
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBouldermit) && __instance.level >= 5 && Rand.Chance(0.1f))
                    {
                        IncidentParms parms = new IncidentParms
                        {
                            target = pawn.Map
                        };
                        DefDatabase<IncidentDef>.GetNamedSilentFail("MeteoriteImpact").Worker.TryExecute(parms);
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitElectrophorus))
                    {
                        List<Pawn> pawns = new List<Pawn>();
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (p.HostileTo(pawn))
                            {
                                pawns.Add(p);
                            }
                        }
                        if (pawns.Count > 0)
                        {
                            int maxStrikes = Math.Max(1, (int)pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                            while (maxStrikes > 0)
                            {
                                Pawn p = pawns.RandomElement<Pawn>();
                                if (p != pawn && p.Position.DistanceTo(pawn.Position) <= 45f)
                                {
                                    SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(pawn.Map);
                                    IntVec3 strikeLoc = p.Position;
                                    if (!strikeLoc.IsValid)
                                    {
                                        strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Standable(p.Map) && !p.Map.roofGrid.Roofed(sq), p.Map, 1000);
                                    }
                                    Mesh boltMesh = LightningBoltMeshPool.RandomBoltMesh;
                                    if (!strikeLoc.Fogged(p.Map))
                                    {
                                        GenExplosion.DoExplosion(strikeLoc, p.Map, 1.9f, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
                                        Vector3 loc = strikeLoc.ToVector3Shifted();
                                        for (int i = 0; i < 4; i++)
                                        {
                                            FleckMaker.ThrowSmoke(loc, p.Map, 1.5f);
                                            FleckMaker.ThrowMicroSparks(loc, p.Map);
                                            FleckMaker.ThrowLightningGlow(loc, p.Map, 1.5f);
                                        }
                                    }
                                    SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, p.Map, false), MaintenanceType.None);
                                    SoundDefOf.Thunder_OnMap.PlayOneShot(info);
                                    Graphics.DrawMesh(boltMesh, strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity, FadedMaterialPool.FadedVersionOf(MatLoader.LoadMat("Weather/LightningBolt", -1), 1f), 0);
                                    maxStrikes--;
                                }
                            }
                        }
                    }
                    if (targets.Any<GlobalTargetInfo>()) {
                        Vector3 loc = ability.def.hasAoE ? ability.firstTarget.CenterVector3 : ((targets[0].Thing != null) ? targets[0].Thing.DrawPos : targets[0].Cell.ToVector3());
                        Map map = (targets[0].Thing != null) ? targets[0].Map : pawn.MapHeld;
                        bool didCanary = false;
                        if (__instance.abilityDef.abilityClass == typeof(Ability_WordOf) && targets[0].Thing is Pawn target)
                        {
                            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird))
                            {
                                FleckMaker.Static(loc.ToIntVec3(), pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * Math.Max(2, pawn.GetPsylinkLevel()));
                                bool canary = pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary);
                                List<Pawn> pawnsAround = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, Math.Max(2, pawn.GetPsylinkLevel()), true).OfType<Pawn>().Distinct<Pawn>().ToList();
                                if (pawnsAround.Count > 0)
                                {
                                    for (int i = pawnsAround.Count - 1; i >= 0; i--)
                                    {
                                        if (pawnsAround[i] == pawn || targets.Contains(pawnsAround[i]))
                                        {
                                            pawnsAround.RemoveAt(i);
                                        }
                                    }
                                    if (pawnsAround.Count > 0)
                                    {
                                        Hediff hediff = HediffMaker.MakeHediff(HautsDefOf.Hauts_PsycastLoopBreaker, pawn);
                                        pawn.health.AddHediff(hediff);
                                        foreach (Pawn p in pawnsAround)
                                        {
                                            bool canApplyTo = ability.AbilityModExtensions.All((AbilityExtension_AbilityMod x) => x.ValidateTarget(p, ability, false));
                                            foreach (AbilityExtension_AbilityMod abilityExtension_AbilityMod in ability.AbilityModExtensions)
                                            {
                                                if (!abilityExtension_AbilityMod.CanApplyOn(p, ability, false))
                                                {
                                                    canApplyTo = false;
                                                }
                                            }
                                            if (canApplyTo)
                                            {
                                                GlobalTargetInfo[] gti = new GlobalTargetInfo[targets.Count()];
                                                gti[0] = p;
                                                for (int i = targets.Count() - 1; i > 0; i--)
                                                {
                                                    gti[i] = targets[i];
                                                }
                                                ability.Cast(gti);
                                            }
                                            if (canary)
                                            {
                                                didCanary = true;
                                                PsychicAwakeningUtility.DoCanaryEffects(pawn, p);
                                            }
                                        }
                                    }
                                }
                            }
                            if (!didCanary && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary))
                            {
                                PsychicAwakeningUtility.DoCanaryEffects(pawn, target);
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBouldermit))
                        {
                            FleckMaker.Static(loc.ToIntVec3(), pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.level));
                            ThingDef chunkDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                                 where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.StoneChunks)
                                                 select tdef).RandomElement();
                            if (chunkDef != null)
                            {
                                GenSpawn.Spawn(chunkDef, loc.ToIntVec3(), pawn.Map);
                                if (Rand.Chance(__instance.psyfocusCost))
                                {
                                    ThingDef metalDef = (from td in DefDatabase<ThingDef>.AllDefsListForReading
                                                         where td.stuffProps != null && td.stuffProps.categories != null && td.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic)
                                                         select td).RandomElement();
                                    if (metalDef != null)
                                    {
                                        Thing metal = ThingMaker.MakeThing(metalDef);
                                        metal.stackCount = (int)(Rand.Value * metalDef.stackLimit);
                                        GenSpawn.Spawn(metal, loc.ToIntVec3(), pawn.Map);
                                    }
                                }
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitDiabolus))
                        {
                            FleckMaker.Static(loc.ToIntVec3(), pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.level));
                            foreach (IntVec3 iv3 in GenRadial.RadialCellsAround(loc.ToIntVec3(), __instance.level, true))
                            {
                                if (iv3.IsValid && GenSight.LineOfSight(loc.ToIntVec3(), iv3, pawn.MapHeld, true, null, 0, 0) && FilthMaker.TryMakeFilth(iv3, pawn.MapHeld, ThingDefOf.Filth_Fuel, 1, FilthSourceFlags.None, true))
                                {
                                    continue;
                                }
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitFirefly))
                        {
                            ThingWithComps firefly = (ThingWithComps)ThingMaker.MakeThing(HVTRoyaltyDefOf.HVT_FireflyLight);
                            firefly.GetComp<CompAuraEmitter>().creator = pawn;
                            GenSpawn.Spawn(firefly, loc.ToIntVec3(), map, WipeMode.Vanish);
                            CompAbilityEffect_Teleport.SendSkipUsedSignal(new LocalTargetInfo(loc.ToIntVec3()), pawn);
                            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(loc.ToIntVec3(), map, false));
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOilbird))
                        {
                            foreach (Hediff h in pawn.health.hediffSet.hediffs)
                            {
                                if (h is Hediff_Oilbird ho)
                                {
                                    if (ho.activeAura != null && !ho.activeAura.Destroyed)
                                    {
                                        ho.activeAura.Destroy();
                                    }
                                    ho.MakeNewAura(loc.ToIntVec3());
                                    break;
                                }
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOrca))
                        {
                            FleckMaker.Static(loc.ToIntVec3(), pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.level));
                            foreach (IntVec3 c in GenRadial.RadialCellsAround(loc.ToIntVec3(), (float)Math.Pow(1.7d, __instance.level), true))
                            {
                                if (c.InBounds(targets[0].Map))
                                {
                                    List<Thing> thingList = c.GetThingList(targets[0].Map);
                                    for (int i = thingList.Count - 1; i >= 0; i--)
                                    {
                                        Pawn pawn2;
                                        if (thingList[i] is Fire)
                                        {
                                            thingList[i].Destroy(DestroyMode.Vanish);
                                        } else if ((pawn2 = (thingList[i] as Pawn)) != null) {
                                            HediffComp_Invisibility invisibilityComp = pawn2.GetInvisibilityComp();
                                            if (invisibilityComp != null)
                                            {
                                                invisibilityComp.DisruptInvisibility();
                                            }
                                        }
                                    }
                                    if (!c.Filled(targets[0].Map))
                                    {
                                        FilthMaker.TryMakeFilth(c, targets[0].Map, ThingDefOf.Filth_Water, 1, FilthSourceFlags.None, true);
                                    }
                                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(c.ToVector3Shifted(), targets[0].Map, FleckDefOf.WaterskipSplashParticles, 1f);
                                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                    targets[0].Map.flecks.CreateFleck(dataStatic);
                                }
                            }
                        }
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitTermite))
                        {
                            FleckMaker.Static(loc.ToIntVec3(), pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 2f * (float)Math.Pow(1.7d, __instance.level));
                            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(loc.ToIntVec3(), map, (float)Math.Pow(1.7d, __instance.level), true))
                            {
                                if (pawn.HostileTo(thing))
                                {
                                    if (thing is Building)
                                    {
                                        SoundInfo info = SoundInfo.InMap(new TargetInfo(thing.Position, thing.Map, false), MaintenanceType.None);
                                        SoundDefOf.Building_Deconstructed.PlayOneShot(info);
                                        thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, 25f * pawn.GetStatValue(StatDefOf.PsychicSensitivity), 0f, -1, pawn));
                                    } else if (thing is Pawn p) {
                                        p.stances.stagger.StaggerFor((int)Math.Ceiling(60f * Math.Min((pawn.GetStatValue(StatDefOf.PsychicSensitivity) + p.GetStatValue(StatDefOf.PsychicSensitivity)) / 2f, 2f)));
                                    }
                                }
                            }
                        }
                    }
                    if (pawn.Map != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOrbWeaver))
                    {
                        FleckMaker.Static(pawn.Position, pawn.MapHeld, FleckDefOf.PsycastAreaEffect, 6f);
                        foreach (Plant plant in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Plant>().Distinct<Plant>())
                        {
                            plant.Growth += 0.1f * pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                            plant.DirtyMapMesh(plant.Map);
                        }
                        if (pawn.Faction != null)
                        {
                            foreach (Building building in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Building>().Distinct<Building>())
                            {
                                if (building.Faction != null && building.Faction == pawn.Faction)
                                {
                                    building.HitPoints += Math.Max(building.MaxHitPoints,(int)((building.MaxHitPoints / 10) * pawn.GetStatValue(StatDefOf.PsychicSensitivity)));
                                }
                            }
                            List<Pawn> eligiblePatients = new List<Pawn>();
                            foreach (Pawn p in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>().Distinct<Pawn>())
                            {
                                if (!p.HostileTo(pawn) && !pawn.HostileTo(p) && p.health.summaryHealth.SummaryHealthPercent < 1f)
                                {
                                    eligiblePatients.Add(p);
                                }
                            }
                            if (eligiblePatients.Count > 0)
                            {
                                Pawn p = eligiblePatients.RandomElement<Pawn>();
                                HautsUtility.StatScalingHeal(Math.Max(2f, Rand.Value * 10f), StatDefOf.PsychicSensitivity, p, p);
                            }
                        }
                        if (ModsConfig.BiotechActive)
                        {
                            foreach (Pawn pawn2 in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>().Distinct<Pawn>())
                            {
                                if (!pawn.HostileTo(pawn2) && !pawn2.HostileTo(pawn) && pawn2.RaceProps.IsMechanoid && MechRepairUtility.CanRepair(pawn2))
                                {
                                    for (int i = 0; i < Math.Floor(4f*pawn.GetStatValue(StatDefOf.PsychicSensitivity)); i++)
                                    {
                                        MechRepairUtility.RepairTick(pawn2);
                                    }
                                }
                            }
                        }
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_AwakenedErudite))
                    {
                        if (__instance.abilityDef.abilityClass == typeof(Ability_WordOf))
                        {
                            Hediff hediff = HediffMaker.MakeHediff(HautsDefOf.Hauts_PsycastLoopBreaker, pawn);
                            pawn.health.AddHediff(hediff);
                        }
                        Hediff_PsycastAbilities hediff_PsycastAbilities = pawn.Psycasts();
                        if (hediff_PsycastAbilities != null)
                        {
                            hediff_PsycastAbilities.GainExperience(__instance.psyfocusCost * 100f * PsycastsMod.Settings.XPPerPercent, true);
                        }
                    }
                }
            }
        }
        //no, you don't get to mind control OR mind break awakened psychics
        public static bool HVT_VPE_GainTraitPrefix(TraitSet __instance, Trait trait)
        {
            if (trait.def == VPE_DefOf.VPE_Thrall)
            {
                Pawn pawn = GetInstanceField(typeof(TraitSet), __instance, "pawn") as Pawn;
                if (PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    return false;
                }

            }
            return true;
        }
        public static bool HVT_VPE_JoinFactionCastPrefix(GlobalTargetInfo[] targets)
        {
            foreach (GlobalTargetInfo globalTargetInfo in targets)
            {
                Pawn pawn = globalTargetInfo.Thing as Pawn;
                if (pawn != null && pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(pawn))
                {
                    Messages.Message("HVT_StayedWoke".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                    return false;
                }
            }
            return true;
        }
        public static bool HVT_VPE_MindControl_CompPostPostPrefix1(HediffComp_MindControl __instance)
        {
            if (__instance.Pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(__instance.Pawn))
            {
                Messages.Message("HVT_StayedWoke".Translate().CapitalizeFirst().Formatted(__instance.Pawn.Named("PAWN")).AdjustedFor(__instance.Pawn, "PAWN", true).Resolve(), __instance.Pawn, MessageTypeDefOf.RejectInput, true);
                return false;
            }
            return true;
        }
        public static bool HVT_VPE_MindControl_CompPostPostPrefix2(HediffComp_MindControl __instance)
        {
            if (__instance.Pawn.story != null && PsychicAwakeningUtility.IsAwakenedPsychic(__instance.Pawn))
            {
                return false;
            }
            return true;
        }
    }
    [DefOf]
    public static class HautsTraitsVPEDefOf
    {
        static HautsTraitsVPEDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HautsTraitsVPEDefOf));
        }
        public static HediffDef HVT_Dominicus;
        public static HediffDef HVT_TabulaRasaAcumen;
        public static HediffDef HVT_TabulaRasaTraitGiver;
        public static BackstoryDef HVT_TabulaRasaChild;
        public static BackstoryDef HVT_TabulaRasaAdult;
    }
    public class Ability_TabulaRasa : Ability_TargetCorpse
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            foreach (GlobalTargetInfo globalTargetInfo in targets)
            {
                Corpse corpse = globalTargetInfo.Thing as Corpse;
                Pawn pawn = corpse.InnerPawn;
                if (ResurrectionUtility.TryResurrectWithSideEffects(pawn))
                {
                    if (pawn.story != null)
                    {
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in pawn.story.traits.allTraits)
                        {
                            if (!HautsUtility.IsExciseTraitExempt(t.def,false) || PsychicAwakeningUtility.IsAwakenedTrait(t.def) || PsychicAwakeningUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove)
                        {
                            pawn.story.traits.RemoveTrait(t);
                        }
                        if (this.pawn.Faction != null && pawn.Faction != null)
                        {
                            pawn.SetFaction(this.pawn.Faction);
                        }
                        if (ModsConfig.IdeologyActive && this.pawn.ideo != null && pawn.ideo != null)
                        {
                            pawn.ideo.SetIdeo(this.pawn.ideo.Ideo);
                        }
                        if (pawn.story.GetBackstory(BackstorySlot.Childhood) != null)
                        {
                            pawn.story.Childhood = HautsTraitsVPEDefOf.HVT_TabulaRasaChild;
                        }
                        if (pawn.story.GetBackstory(BackstorySlot.Adulthood) != null)
                        {
                            pawn.story.Adulthood = HautsTraitsVPEDefOf.HVT_TabulaRasaAdult;
                        }
                    }
                    if (pawn.skills != null)
                    {
                        foreach (SkillRecord sr in pawn.skills.skills)
                        {
                            sr.Level = 0;
                            int randPassion = (int)Math.Ceiling(Rand.Value * 5);
                            if (randPassion <= 2)
                            {
                                sr.passion = Passion.None;
                            } else if (randPassion <= 4) {
                                sr.passion = Passion.Minor;
                            } else {
                                sr.passion = Passion.Major;
                            }
                        }
                        pawn.skills.Notify_SkillDisablesChanged();
                    }
                    if (!this.pawn.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_Dominicus))
                    {
                        Hediff_Catarina newPawnLink = (Hediff_Catarina)HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_Dominicus, this.pawn, null);
                        this.pawn.health.AddHediff(newPawnLink);
                        newPawnLink.thoseRisen.Add(pawn);
                    } else {
                        Hediff_Catarina pawnLink = (Hediff_Catarina)this.pawn.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_Dominicus);
                        pawnLink.thoseRisen.Add(pawn);
                    }
                    pawn.Notify_DisabledWorkTypesChanged();
                    Hediff hediff = HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaAcumen, pawn, null);
                    HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                    hediffComp_Disappears.ticksToDisappear = (int)(120000 * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity));
                    pawn.health.AddHediff(hediff, null, null, null);
                    Hediff_TraitGiver hediff2 = (Hediff_TraitGiver)HediffMaker.MakeHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver, pawn, null);
                    pawn.health.AddHediff(hediff2, null, null, null);
                    hediff2.resurrector = this.pawn;
                }
            }
        }
    }
    public class Hediff_Catarina : Hediff
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            foreach (Pawn p in thoseRisen)
            {
                if (p.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver))
                {
                    Hediff_TraitGiver traitGiver = (Hediff_TraitGiver)p.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_TabulaRasaTraitGiver);
                    traitGiver.resurrector = null;
                }
                p.Kill(null);
            }
            this.pawn.health.RemoveHediff(this);
        }
        public List<Pawn> thoseRisen = new List<Pawn>();
    }
    public class Hediff_TraitGiver : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (this.pawn.story == null || this.pawn.DevelopmentalStage.Baby() || this.pawn.DevelopmentalStage.Newborn())
            {
                this.Severity = -1f;
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (this.resurrector != null && this.resurrector.health.hediffSet.HasHediff(HautsTraitsVPEDefOf.HVT_Dominicus))
            {
                Hediff_Catarina pawnLink = (Hediff_Catarina)this.resurrector.health.hediffSet.GetFirstHediffOfDef(HautsTraitsVPEDefOf.HVT_Dominicus);
                pawnLink.thoseRisen.Remove(this.pawn);
            }
        }
        public override void PostTick()
        {
            base.PostTick();
            if (this.pawn.IsHashIntervalTick(60) && this.pawn.story != null)
            {
                if (Rand.MTBEventOccurs(2, 60000f, 60f))
                {
                    List<TraitDef> traitPool = new List<TraitDef>();
                    foreach (TraitDef td in DefDatabase<TraitDef>.AllDefs)
                    {
                        if (!HautsUtility.IsExciseTraitExempt(td,false) && !this.pawn.story.traits.HasTrait(td))
                        {
                            bool toAdd = true;
                            foreach (Trait t in this.pawn.story.traits.allTraits)
                            {
                                if (t.def.ConflictsWith(td))
                                {
                                    toAdd = false;
                                }
                            }
                            foreach (SkillRecord sr in this.pawn.skills.skills)
                            {
                                if ((sr.TotallyDisabled && td.RequiresPassion(sr.def)) || (sr.passion != Passion.None && td.ConflictsWithPassion(sr.def)))
                                {
                                    toAdd = false;
                                }
                            }
                            if (toAdd)
                            {
                                traitPool.Add(td);
                            }
                        }
                    }
                    TraitDef toGain = traitPool.RandomElement<TraitDef>();
                    Trait trait = new Trait(toGain, PawnGenerator.RandomTraitDegree(toGain), false);
                    this.pawn.story.traits.GainTrait(trait);
                    TaggedString message;
                    if (HautsUtility.IsHighFantasy())
                    {
                        message = "HVT_TabulaRasadFantasy".Translate(this.pawn.Name.ToStringShort, trait.Label);
                    } else {
                        message = "HVT_TabulaRasad".Translate(this.pawn.Name.ToStringShort, trait.Label);
                    }
                    if (this.pawn.story.traits.allTraits.Count >= HVT_Mod.settings.traitsMax)
                    {
                        message += "HVT_EndTabulaRasa".Translate(this.pawn.Name.ToStringShort);
                    }
                    Messages.Message(message, this.pawn, MessageTypeDefOf.NeutralEvent, true);
                }
                if (this.pawn.story.traits.allTraits.Count >= HVT_Mod.settings.traitsMax)
                {
                    this.Severity = -1f;
                }
            }
        }
        public Pawn resurrector = null;
    }
    public class AbilityExtension_ExciseTrait : AbilityExtension_AbilityMod
    {
        public override void Cast(GlobalTargetInfo[] targets, VFECore.Abilities.Ability ability)
        {
            base.Cast(targets, ability);
            foreach (GlobalTargetInfo globalTargetInfo in targets)
            {
                Pawn pawn = globalTargetInfo.Thing as Pawn;
                if (pawn.story != null && pawn.story.traits.allTraits.Count > 0)
                {
                    List<Trait> allTakeableTraits = new List<Trait>();
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (t.def.exclusionTags != null && t.def.exclusionTags.Contains("SexualOrientation"))
                        {
                            continue;
                        }
                        if (t.def != HVTRoyaltyDefOf.HVT_LatentPsychic && !PsychicAwakeningUtility.IsAwakenedTrait(t.def) && !PsychicAwakeningUtility.IsTranscendentTrait(t.def) && !HautsUtility.IsExciseTraitExempt(t.def,false))
                        {
                            allTakeableTraits.Add(t);
                        }
                    }
                    if (allTakeableTraits.Count > 0)
                    {
                        Trait traitToRemove = allTakeableTraits.RandomElement();
                        pawn.story.traits.RemoveTrait(traitToRemove, true);
                        pawn.story.traits.RecalculateSuppression();
                    } else {
                        Messages.Message("HVT_ExciseTraitExemptionFailure".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                    }
                } else {
                    Messages.Message("HVT_ExciseTraitTraitlessFailure".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                }
            }
        }
    }
}
