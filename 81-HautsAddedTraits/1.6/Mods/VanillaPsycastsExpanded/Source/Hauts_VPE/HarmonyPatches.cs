using HarmonyLib;
using HautsFramework;
using HautsTraitsRoyalty;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VanillaPsycastsExpanded;
using VanillaPsycastsExpanded.Harmonist;
using VEF.Abilities;
using Verse;
using Verse.Sound;

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
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.GetPsyfocusUsedByPawn)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_GetPsyfocusUsedByPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.ValidateTarget)),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_ValidateTargetPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VEF.Abilities.Ability) }),
                            prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_CastPrefix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VEF.Abilities.Ability) }),
                            postfix: new HarmonyMethod(patchType, nameof(HVT_VPE_CastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.GainTrait)),
                          prefix: new HarmonyMethod(patchType, nameof(HVT_VPE_GainTraitPrefix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_JoinFaction), nameof(AbilityExtension_JoinFaction.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VEF.Abilities.Ability)}),
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
        //incarnates' scaling works off of VPE psycasts instead of regular ones, necessary for obvious reasons
        public static void HVT_VPE_Hediff_IncarnatePsycastsKnownPostfix(ref float __result, Hediff_IncarnatePsycastsKnown __instance)
        {
            CompAbilities comp = __instance.pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                float psycastsKnown = 0f;
                foreach (VEF.Abilities.Ability a in comp.LearnedAbilities)
                {
                    if (a.def.HasModExtension<AbilityExtension_Psycast>())
                    {
                        psycastsKnown += 1f;
                    }
                }
                __result = psycastsKnown;
            }
        }
        //bellbirds work differently in VPE; they get Group Link and it's free for them
        public static void HVT_VPE_GetPsyfocusUsedByPawnPostfix(ref float __result, AbilityExtension_Psycast __instance, Pawn pawn)
        {
            if (__instance.abilityDef == DefDatabase<VEF.Abilities.AbilityDef>.GetNamedSilentFail("VPE_GroupLink") && pawn != null && pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird))
            {
                __result = 0f;
            }
        }
        //Dragon transcendence cannot be directly targeted by psycasts from hostile casters
        public static void HVT_VPE_ValidateTargetPostfix(ref bool __result, LocalTargetInfo target, VEF.Abilities.Ability ability, bool throwMessages = false)
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
        /*handles Glowworm transcendent effects.
         * The prefix puts in __state all the things a Glowworm is affecting with a skip psycast, as well as all the stuff in the lines between those things and their destinations, or all the things around themselves if teleported to own point.
         *   Well, that's how it's supposed to work (and does work without VPE). Here, it always ends up just doing the latter: catalogue all things around the targets at their endpoint.
         * The postfix deals skip damage to all things in __state, putting tiny little skipgate pinholes on top of them to show the visual destruction.
         * The postfix itself also handles a couple other psychic trait effects. In order as coded:
         * Erudites have a VPE-exclusive function to gain experience from casting, enabled here.
         * Bouldermit can generate meteorites on casting 5th level casts, 10% chance. They also generate stone chunks and possibly (psyfocus cost-scaling chance) random metals on any psycast. This is anti-synergistic with psyfocus cost reductions,
         *   but that's fine. Not all avenues of power need to compound.
         * Electrophorus generates a psysens-scaling number of lightning strikes on random pawns not under a roof upon casting any psycast.
         * Bellbird gains a small amount of psyfocus for casting a Word psycast if they don't have Group Link active
         * Canary has several additional effects on Word use (resistance reduction, mood boost, conversion attempt).
         * Diabolus coats an area around the target psycast with chemfuel puddles.
         * Firefly generates a unique kind of solar pinhole with its own blinding aura (written just under the trait itself in Traits_R.xml) at the targeted point, or refreshes the duration of an existing such pinhole and recruits it
         *   (the pinhole doesn't blind people if allied to its faction, initially set by its creator) if there is one already there.
         * Oilbird has an aura that doesn't move with it; it has to be repositioned by casting a psycast, at which point it appears at the target point. This creates (or moves) the aura.
         * Orcas generate a Waterskip effect at the target point.
         * Termites deal damage in an AoE around the target point, but only to buildings. Radius is scaled by the psycast's level to the 1.7th power.
         * Orb Weavers emit a pulse on their own location that adds to the growth of plants, repairs buildings, reduces the injury severity of a nearby non-hostile pawn, and repairs nearby non-hostile mechs for one repair tick.
         *   The heal specifically scales off the random pawn's psysens.*/
        public static void HVT_VPE_CastPrefix(AbilityExtension_Psycast __instance, ref GlobalTargetInfo[] targets, VEF.Abilities.Ability ability, out List<Thing> __state)
        {
            __state = new List<Thing>();
            if (ability.pawn != null && ability.pawn.story != null && targets.Any() && targets[0].Thing != null)
            {
                Pawn pawn = ability.pawn;
                if (Stats_AbilityRangesUtility.IsSkipAbility(__instance.abilityDef) && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitGlowworm))
                {
                    List<IntVec3> iv3s = new List<IntVec3>();
                    for (int i = 0; i < targets.Length; i += 2)
                    {
                        if (targets[i].Cell.IsValid && targets[i].Cell.InBounds(targets[i].Map) && targets.Length > i+1 && targets[i+1].Cell.IsValid && targets[i + 1].Cell.InBounds(targets[i].Map) && targets[i].Map == targets[i+1].Map)
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
        public static void HVT_VPE_CastPostfix(AbilityExtension_Psycast __instance, GlobalTargetInfo[] targets, VEF.Abilities.Ability ability, List<Thing> __state)
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
                    if (pawn.MapHeld == null)
                    {
                        return;
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBouldermit) && __instance.level >= 5 && Rand.Chance(0.1f))
                    {
                        IncidentParms parms = new IncidentParms
                        {
                            target = pawn.Map
                        };
                        DefDatabase<IncidentDef>.GetNamedSilentFail("MeteoriteImpact").Worker.TryExecute(parms);
                    }
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitElectrophorus) && !pawn.MapHeld.Biome.inVacuum)
                    {
                        List<Pawn> pawns = new List<Pawn>();
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (p.HostileTo(pawn) && !p.Position.Roofed(p.Map))
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
                                        GenExplosion.DoExplosion(strikeLoc, p.Map, 1.9f, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
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
                        if (__instance.abilityDef.abilityClass == typeof(Ability_WordOf))
                        {
                            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitCanary))
                            {
                                foreach (GlobalTargetInfo gti in targets)
                                {
                                    if (gti.Thing != null && gti.Thing is Pawn p)
                                    {
                                        PsychicPowerUtility.DoCanaryEffects(pawn, p);
                                    }
                                }
                            }
                            if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird) && !pawn.health.hediffSet.HasHediff(VPE_DefOf.VPE_GroupLink) && pawn.psychicEntropy != null)
                            {
                                pawn.psychicEntropy.OffsetPsyfocusDirectly(0.1f);
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
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitFirefly) && loc.ToIntVec3().Walkable(pawn.Map))
                        {
                            bool alreadyExists = false;
                            List<Thing> thingList = loc.ToIntVec3().GetThingList(pawn.Map);
                            if (!thingList.NullOrEmpty())
                            {
                                foreach (Thing t in thingList)
                                {
                                    if (t.def == HVTRoyaltyDefOf.HVT_FireflyLight)
                                    {
                                        alreadyExists = true;
                                        CompDestroyAfterDelay cdad = t.TryGetComp<CompDestroyAfterDelay>();
                                        if (cdad != null)
                                        {
                                            cdad.spawnTick = Find.TickManager.TicksGame;
                                        }
                                        CompAuraEmitterHediff caeh = t.TryGetComp<CompAuraEmitterHediff>();
                                        if (caeh != null)
                                        {
                                            caeh.faction = pawn.Faction;
                                        }
                                        break;
                                    }
                                }
                            }
                            if (!alreadyExists)
                            {
                                ThingWithComps firefly = (ThingWithComps)ThingMaker.MakeThing(HVTRoyaltyDefOf.HVT_FireflyLight);
                                firefly.GetComp<CompAuraEmitter>().creator = pawn;
                                GenSpawn.Spawn(firefly, loc.ToIntVec3(), pawn.Map, WipeMode.Vanish);
                                CompAbilityEffect_Teleport.SendSkipUsedSignal(new LocalTargetInfo(loc.ToIntVec3()), pawn);
                                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(loc.ToIntVec3(), pawn.Map, false));
                            }
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
                                if (!p.HostileTo(pawn) && p.health.summaryHealth.SummaryHealthPercent < 1f)
                                {
                                    eligiblePatients.Add(p);
                                }
                            }
                            if (eligiblePatients.Count > 0)
                            {
                                Pawn p = eligiblePatients.RandomElement<Pawn>();
                                HautsMiscUtility.StatScalingHeal(Math.Max(2f, Rand.Value * 10f), StatDefOf.PsychicSensitivity, p, p);
                            }
                        }
                        if (ModsConfig.BiotechActive)
                        {
                            foreach (Pawn pawn2 in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>().Distinct<Pawn>())
                            {
                                if (!pawn.HostileTo(pawn2) && pawn2.RaceProps.IsMechanoid && MechRepairUtility.CanRepair(pawn2))
                                {
                                    for (int i = 0; i < Math.Floor(4f*pawn.GetStatValue(StatDefOf.PsychicSensitivity)); i++)
                                    {
                                        MechRepairUtility.RepairTick(pawn2, 1);
                                    }
                                }
                            }
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
                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
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
                if (pawn != null && pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn))
                {
                    Messages.Message("HVT_StayedWoke".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.RejectInput, true);
                    return false;
                }
            }
            return true;
        }
        public static bool HVT_VPE_MindControl_CompPostPostPrefix1(HediffComp_MindControl __instance)
        {
            if (__instance.Pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(__instance.Pawn))
            {
                Messages.Message("HVT_StayedWoke".Translate().CapitalizeFirst().Formatted(__instance.Pawn.Named("PAWN")).AdjustedFor(__instance.Pawn, "PAWN", true).Resolve(), __instance.Pawn, MessageTypeDefOf.RejectInput, true);
                return false;
            }
            return true;
        }
        public static bool HVT_VPE_MindControl_CompPostPostPrefix2(HediffComp_MindControl __instance)
        {
            if (__instance.Pawn.story != null && PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(__instance.Pawn))
            {
                return false;
            }
            return true;
        }
    }
}
