using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    public static class PsychicPowerUtility
    {
        //heals the pawn, a bunch. there might theoretically be some pawn out there (esp with mods) who's so injured this doesn't fully heal them, but that's unlikely. Also has SFX and VFX, because of course.
        public static void PsychicHeal(Pawn pawn, bool shouldCure)
        {
            HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
            if (shouldCure)
            {
                for (int i = 100; i > 0; i--)
                {
                    HealthUtility.FixWorstHealthCondition(pawn);
                }
            }
            if (pawn.Map != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 10f);
                SoundDefOf.Thunder_OnMap.PlayOneShot(pawn);
                WeatherEvent_LightningStrike lightningflash = new WeatherEvent_LightningStrike(pawn.Map);
                lightningflash.WeatherEventDraw();
            }
        }
        //grants a psylink level, and either grants up to "abilities" number of psycasts (without VPE), or else bonus levels equal to half that amount (with)
        public static void GrantEruditeEffects(Pawn pawn, int abilities)
        {
            PawnUtility.ChangePsylinkLevel(pawn, 1, false);
            Hediff_Psylink hediff_Psylink = pawn.GetMainPsylinkSource();
            if (!ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
            {
                for (int i = 0; i < abilities; i++)
                {
                    int randLevel = (int)Math.Ceiling(Rand.Value * hediff_Psylink.level);
                    List<AbilityDef> unknownPsycasts = DefDatabase<AbilityDef>.AllDefs.Where((AbilityDef a) => a.IsPsycast && a.level == randLevel && pawn.abilities.GetAbility(a) == null).ToList<AbilityDef>();
                    if (unknownPsycasts.Count > 0)
                    {
                        pawn.abilities.GainAbility(unknownPsycasts.RandomElement());
                    }
                }
            }
            else
            {
                int levelsToGain = abilities / 2;
                if (levelsToGain >= 5)
                {
                    levelsToGain = 4;
                }
                for (int i = 0; i < levelsToGain; i++)
                {
                    PawnUtility.ChangePsylinkLevel(pawn, 1, false);
                }
            }
        }
        //Aptenodytes man in black spawning
        public static void ColonyHuddle(Pawn pawn)
        {
            Pawn guy = PsychicPowerUtility.ColonyHuddleInner(pawn);
            guy.SetFaction(pawn.Faction);
            if (pawn.SpawnedOrAnyParentSpawned)
            {
                CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => pawn.MapHeld.reachability.CanReachColony(c) && !c.Fogged(pawn.MapHeld), pawn.MapHeld, CellFinder.EdgeRoadChance_Neutral, out IntVec3 iv3);
                GenSpawn.Spawn(guy, iv3, pawn.MapHeld);
            } else if (pawn.GetCaravan() != null) {
                Find.WorldPawns.PassToWorld(guy, PawnDiscardDecideMode.Decide);
                guy.SetFaction(pawn.GetCaravan().Faction);
                pawn.GetCaravan().AddPawn(guy, true);
                guy.SetFaction(pawn.Faction);
            } else {
                return;
            }
            ChoiceLetter choiceLetter = LetterMaker.MakeLetter("HVT_PengwengGetLabel".Translate(), "HVT_PengwengGetText".Translate(pawn.Name.ToStringShort, guy.Name.ToStringShort), LetterDefOf.PositiveEvent, guy);
            Find.LetterStack.ReceiveLetter(choiceLetter, null, 0, true);

        }
        public static Pawn ColonyHuddleInner(Pawn pawn)
        {
            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("StrangerInBlack"), pawn.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, true, true, 20f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, ModsConfig.IdeologyActive ? pawn.Ideo : null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
        }
        /*For transcendent effects that trigger on psycast, but are neither handled via hediffs nor require the target(s) as input
         * -Bellbirds lose additional psyfocus after casting any Word
         * -Electrophoruses call lightning a psysens-scaling number of times on hostile pawns in 45c who aren't under a roof. Doesn't work in space.
         * -Orb Weavers release a 6c radius pulse that makes plants grow, repairs same-faction buildings, heals a random non-hostile pawn for an amount scaling by its psysens, and repairs non-hostile mechs*/
        public static void PsycastActivationRiderEffects(Psycast __instance)
        {
            if (__instance.pawn != null)
            {
                Pawn pawn = __instance.pawn;
                if (pawn.story != null)
                {
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitBellbird) && __instance.def.category == DefDatabase<AbilityCategoryDef>.GetNamed("WordOf") && pawn.psychicEntropy != null)
                    {
                        pawn.psychicEntropy.OffsetPsyfocusDirectly(-0.075f);
                    }
                    if (pawn.Map != null)
                    {
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitElectrophorus) && !pawn.Map.Biome.inVacuum)
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
                        if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitOrbWeaver))
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
                                        building.HitPoints += (int)(0.1f * building.MaxHitPoints * pawn.GetStatValue(StatDefOf.PsychicSensitivity));
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
                                foreach (Pawn pawn2 in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Pawn>())
                                {
                                    if (!pawn.HostileTo(pawn2) && pawn2.RaceProps.IsMechanoid && MechRepairUtility.CanRepair(pawn2))
                                    {
                                        for (int i = 0; i < Math.Floor(4 * pawn.GetStatValue(StatDefOf.PsychicSensitivity)); i++)
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
        }
        /*Pawns affected by the Word casting of a Canary lose unwavering loyalty, lose resistance and will, and either get conversion attempted (if not of Canary's ideo) or gain a mood boost (otherwise).
         * Effects scale with Canary's talking and victim's psysens*/
        public static void DoCanaryEffects(Pawn canary, Pawn p)
        {
            float magnitude = canary.health.capacities.GetLevel(PawnCapacityDefOf.Talking) * p.GetStatValue(StatDefOf.PsychicSensitivity);
            if (p.guest != null)
            {
                if (p.Faction == null || p.Faction != Faction.OfPlayerSilentFail || p.guest.IsPrisoner)
                {
                    p.guest.Recruitable = true;
                }
                if (p.guest.IsPrisoner)
                {
                    p.guest.resistance -= Math.Max(1f, 2f * magnitude);
                    if (p.guest.resistance < 0f)
                    {
                        p.guest.resistance = 0f;
                    }
                    if (ModsConfig.IdeologyActive)
                    {
                        p.guest.will -= Math.Max(0.2f, 0.4f * magnitude);
                        if (p.guest.will < 0f)
                        {
                            p.guest.will = 0f;
                        }
                    }
                }
            }
            bool shouldBoostMood = true;
            if (ModsConfig.IdeologyActive)
            {
                if (canary.ideo != null && p.ideo != null && canary.ideo.Ideo != null && p.ideo.Ideo != null)
                {
                    if (canary.ideo.Ideo != p.ideo.Ideo)
                    {
                        shouldBoostMood = false;
                        Ideo ideo = p.Ideo;
                        Precept_Role role = ideo.GetRole(p);
                        float num = Math.Max(0.01f, InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(canary, p) * magnitude);
                        if (p.ideo.IdeoConversionAttempt(num, canary.Ideo, true))
                        {
                            p.ideo.SetIdeo(canary.Ideo);
                            if (PawnUtility.ShouldSendNotificationAbout(canary) || PawnUtility.ShouldSendNotificationAbout(p))
                            {
                                string letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                                string letterText = "LetterConvertIdeoAttempt_Success".Translate(canary.Named("INITIATOR"), p.Named("RECIPIENT"), canary.Ideo.Named("IDEO"), ideo.Named("OLDIDEO")).Resolve();
                                LetterDef letterDef = LetterDefOf.PositiveEvent;
                                LookTargets lookTargets = new LookTargets(new TargetInfo[] { canary, p });
                                if (role != null)
                                {
                                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(p.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                                }
                                Find.LetterStack.ReceiveLetter(letterLabel, letterText, letterDef, lookTargets ?? canary, null, null, null, null, 0, true);
                            }
                        }
                    }
                }
            }
            if (shouldBoostMood && p.needs.mood != null)
            {
                p.needs.mood.thoughts.memories.TryGainMemory(HVTRoyaltyDefOf.HVT_CanarySong);
            }
        }
        //Kea skill gains and RimLanguage random language learning
        public static void KeaOffsetPsyfocusLearning(Pawn pawn, float offset, float languageChance)
        {
            if (pawn.skills != null)
            {
                int maxSkill = 0;
                if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                {
                    Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                    if (psylink != null)
                    {
                        maxSkill = Math.Max((int)(psylink.level / 4), 1);
                    }
                }
                else
                {
                    maxSkill = 3 * pawn.GetPsylinkLevel();
                }
                foreach (SkillRecord skillRecord in pawn.skills.skills)
                {
                    if (skillRecord.Level < maxSkill)
                    {
                        skillRecord.Learn(offset * 5000f, true);
                        if (skillRecord.Level >= maxSkill)
                        {
                            skillRecord.Level = maxSkill;
                        }
                    }
                }
                if (Rand.Chance(languageChance))
                {
                    ModCompatibilityUtility.LearnLanguage(pawn, null, 0.06f);
                }
            }
        }
        //Hil Mynah psycast copying (20% chance) - works for both non-VPE and VPE
        public static void MynahAbilityCopy(Pawn pawn, Pawn pawn2)
        {
            if (pawn.story != null && pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitMynah))
            {
                foreach (Ability a in pawn2.abilities.abilities)
                {
                    if (a is Psycast && Rand.Chance(0.2f) && pawn.abilities.GetAbility(a.def) == null)
                    {
                        pawn.abilities.GainAbility(a.def);
                        if (PawnUtility.ShouldSendNotificationAbout(pawn))
                        {
                            Messages.Message("HVT_MynahLearnAbility".Translate().CapitalizeFirst().Formatted(pawn.Name.ToStringShort, a.def.LabelCap, pawn2.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent, true);
                        }
                    }
                }
                VEF.Abilities.CompAbilities comp = pawn.GetComp<VEF.Abilities.CompAbilities>();
                VEF.Abilities.CompAbilities comp2 = pawn2.GetComp<VEF.Abilities.CompAbilities>();
                if (comp != null && comp2 != null)
                {
                    foreach (VEF.Abilities.Ability a in comp2.LearnedAbilities)
                    {
                        if (ModCompatibilityUtility.IsVPEPsycast(a) && Rand.Chance(0.2f) && !comp.HasAbility(a.def))
                        {
                            comp.GiveAbility(a.def);
                            if (PawnUtility.ShouldSendNotificationAbout(pawn))
                            {
                                Messages.Message("HVT_MynahLearnAbility".Translate().CapitalizeFirst().Formatted(pawn.Name.ToStringShort, a.def.LabelCap, pawn2.Name.ToStringShort), pawn, MessageTypeDefOf.PositiveEvent, true);
                            }
                        }
                    }
                }
            }
        }
        //Xerigium fresh injury or immunizable illness removal
        public static void XerigiumHeal(Pawn doctor, Pawn patient)
        {
            List<Hediff> toHeals = new List<Hediff>();
            foreach (Hediff h in patient.health.hediffSet.hediffs)
            {
                if ((h is Hediff_Injury && !h.IsPermanent()) || (h is HediffWithComps hwc && hwc.TryGetComp<HediffComp_Immunizable>() != null))
                {
                    toHeals.Add(h);
                }
            }
            if (toHeals.Count > 0)
            {
                patient.health.RemoveHediff(toHeals.RandomElement());
            }
        }
        //The animal-generating transcendences (Oxpecker, War Queen) cause all fish populations in the current map to periodically increase
        public static void AddMoreFish(Pawn pawn, int delta, float percentMaxPopulation)
        {
            if (ModsConfig.OdysseyActive && pawn.IsHashIntervalTick(2500, delta) && pawn.SpawnedOrAnyParentSpawned && pawn.MapHeld.waterBodyTracker != null && !pawn.MapHeld.waterBodyTracker.Bodies.NullOrEmpty())
            {
                foreach (WaterBody body in pawn.MapHeld.waterBodyTracker.Bodies)
                {
                    body.Population += body.MaxPopulation * percentMaxPopulation;
                }
            }
        }
        //a pawn must clear these hurdles to be a valid host for Wraiths to transfer to when they die
        public static bool IsEligibleForWraithJump(Pawn p, Pawn wraith)
        {
            if (p.story != null && (p.Faction == null || wraith.Faction == null || p.Faction != wraith.Faction))
            {
                return true;
            }
            return false;
        }
        /*on death, a Wraith finds an eligible pawn to transfer into (preferentially same map and spawned, or same caravan; preferentially NOT a world pawn.)
         * Copies a bunch of stuff from the Wraith into the new pawn, see comment labels inside.*/
        public static void WraithTransfer(Pawn pawn)
        {
            List<Pawn> eligiblePawns = new List<Pawn>();
            if (pawn.Faction != null)
            {
                if (pawn.Map != null)
                {
                    if (pawn.Map.mapPawns.AllHumanlikeSpawned.Count > 1)
                    {
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    else
                    {
                        foreach (Pawn p in pawn.Map.mapPawns.AllPawns)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                }
                else if (pawn.GetCaravan() != null)
                {
                    foreach (Pawn p in pawn.GetCaravan().PawnsListForReading)
                    {
                        if (IsEligibleForWraithJump(p, pawn))
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
                else
                {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (IsEligibleForWraithJump(p, pawn))
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (IsEligibleForWraithJump(p, pawn))
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
            }
            if (eligiblePawns.Count == 0)
            {
                if (pawn.Map != null)
                {
                    foreach (Pawn p in pawn.Map.mapPawns.AllPawns)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
                else if (pawn.GetCaravan() != null)
                {
                    foreach (Pawn p in pawn.GetCaravan().PawnsListForReading)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
                else
                {
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn p in map.mapPawns.AllPawns)
                        {
                            if (p.story != null)
                            {
                                eligiblePawns.Add(p);
                            }
                        }
                    }
                    foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (p.story != null)
                        {
                            eligiblePawns.Add(p);
                        }
                    }
                }
            }
            if (eligiblePawns.Count > 0)
            {
                List<Pawn> pawnsToRemove = new List<Pawn>();
                if (eligiblePawns.Contains(pawn))
                {
                    pawnsToRemove.Add(pawn);
                }
                foreach (Pawn p in eligiblePawns)
                {
                    if (p.GetStatValue(StatDefOf.PsychicSensitivity) < 1E-45f || PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(p) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitSeraph) || p.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_TTraitWraith))
                    {
                        pawnsToRemove.Add(p);
                    }
                }
                foreach (Pawn p in pawnsToRemove)
                {
                    eligiblePawns.Remove(p);
                }
                if (eligiblePawns.Count > 0)
                {
                    Pawn newHost = eligiblePawns.RandomElement();
                    //name change
                    TaggedString newHostOldName = newHost.Name.ToStringFull;
                    newHost.Name = NameTriple.FromString(pawn.Name.ToString(), false);
                    //replace faction.
                    Faction newHostOldFaction = newHost.Faction;
                    if (pawn.Faction != null && (newHost.Faction == null || newHost.Faction != pawn.Faction))
                    {
                        newHost.SetFaction(pawn.Faction);
                    }
                    //clear all relations and thoughts first, not replacing them w/ anything. Do NOT remove other pawns' relations with the host, except for the wraith
                    List<DirectPawnRelation> dprsToRemove = new List<DirectPawnRelation>();
                    List<DirectPawnRelation> dprsToTransfer = new List<DirectPawnRelation>();
                    List<Trait> traitsToRemove = new List<Trait>();
                    List<TraitDef> wokeTraitPool = new List<TraitDef>();
                    List<GeneDef> wokeGenePool = new List<GeneDef>();
                    GeneDef transferredGene = null;
                    Hediff_Wraithly hediff = (Hediff_Wraithly)pawn.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
                    if (newHost.relations != null)
                    {
                        foreach (DirectPawnRelation dpr in newHost.relations.DirectRelations)
                        {
                            dprsToRemove.Add(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToRemove)
                        {
                            newHost.relations.RemoveDirectRelation(dpr);
                        }
                    }
                    dprsToRemove.Clear();
                    if (pawn.relations != null)
                    {
                        foreach (DirectPawnRelation dpr in pawn.relations.DirectRelations)
                        {
                            if (dpr.otherPawn != newHost)
                            {
                                dprsToTransfer.Add(dpr);
                            }
                        }
                        foreach (DirectPawnRelation dpr in pawn.relations.DirectRelations)
                        {
                            dprsToRemove.Add(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToRemove)
                        {
                            pawn.relations.RemoveDirectRelation(dpr);
                        }
                        foreach (DirectPawnRelation dpr in dprsToTransfer)
                        {
                            newHost.relations.AddDirectRelation(dpr.def, dpr.otherPawn);
                        }
                    }
                    newHost.story.favoriteColor = pawn.story.favoriteColor;
                    //replace ideoligion
                    if (ModsConfig.IdeologyActive && newHost.ideo != null && pawn.ideo != null)
                    {
                        newHost.ideo.SetIdeo(pawn.ideo.Ideo);
                    }
                    //replace all backstories
                    newHost.story.Childhood = pawn.story.Childhood;
                    newHost.story.Adulthood = pawn.story.Adulthood;
                    //replace all non-genetic, non-exemption, non-wokepsychic traits; possession can only transfer ONE wokening, but any number of transcendences is fine
                    foreach (Trait t in newHost.story.traits.allTraits)
                    {
                        if (!TraitModExtensionUtility.IsExciseTraitExempt(t.def) && t.sourceGene == null)
                        {
                            traitsToRemove.Add(t);
                        }
                    }
                    foreach (Trait t in traitsToRemove)
                    {
                        newHost.story.traits.RemoveTrait(t);
                    }
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (t.sourceGene == null && !PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                        {
                            if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                            {
                                wokeTraitPool.Add(t.def);
                            }
                            else if (!TraitModExtensionUtility.IsExciseTraitExempt(t.def))
                            {
                                newHost.story.traits.GainTrait(new Trait(t.def, t.Degree));
                            }
                        }
                    }
                    //add up to one woke trait or gene... otherwise, transes can't be passed on. If it somehow isn't possible to pass one over, forcibly awaken the new host
                    if (wokeTraitPool.Count > 0)
                    {
                        newHost.story.traits.GainTrait(new Trait(wokeTraitPool.RandomElement()));
                    }
                    else if (ModsConfig.BiotechActive && newHost.genes != null && pawn.genes != null)
                    {
                        foreach (Gene g in pawn.genes.GenesListForReading)
                        {
                            if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(g.def))
                            {
                                wokeGenePool.Add(g.def);
                            }
                        }
                        if (wokeGenePool.Count > 0)
                        {
                            transferredGene = wokeGenePool.RandomElement();
                            if (hediff != null)
                            {
                                hediff.geneToRemove = transferredGene;
                            }
                            newHost.genes.AddGene(transferredGene, true);
                        }
                    }
                    else
                    {
                        AwakeningMethodsUtility.AwakenPsychicTalent(newHost, false, "", "", true);
                    }
                    //NOW we can add the trans traits
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                        {
                            newHost.story.traits.GainTrait(new Trait(t.def));
                        }
                    }
                    //replace all skills & passions
                    newHost.skills.skills.Clear();
                    foreach (SkillRecord skillRecord in pawn.skills.skills)
                    {
                        SkillRecord item = new SkillRecord(newHost, skillRecord.def)
                        {
                            levelInt = skillRecord.levelInt,
                            passion = skillRecord.passion,
                            xpSinceLastLevel = skillRecord.xpSinceLastLevel,
                            xpSinceMidnight = skillRecord.xpSinceMidnight
                        };
                        newHost.skills.skills.Add(item);
                    }
                    //make host's psylink level at least as high as the wraith's
                    if (pawn.GetMainPsylinkSource() != null)
                    {
                        int levelDifference = pawn.GetPsylinkLevel() - newHost.GetPsylinkLevel();
                        if (levelDifference > 0)
                        {
                            newHost.ChangePsylinkLevel(levelDifference, false);
                        }
                    }
                    //NOW we can replace thoughts. old situational thoughts don't transfer, just memories
                    if (pawn.needs.mood != null && newHost.needs.mood != null)
                    {
                        List<Thought_Memory> memories = newHost.needs.mood.thoughts.memories.Memories;
                        memories.Clear();
                        foreach (Thought_Memory thought_Memory in pawn.needs.mood.thoughts.memories.Memories)
                        {
                            Thought_Memory thought_Memory2 = (Thought_Memory)ThoughtMaker.MakeThought(thought_Memory.def);
                            thought_Memory2.CopyFrom(thought_Memory);
                            thought_Memory2.pawn = newHost;
                            memories.Add(thought_Memory2);
                        }
                    }
                    //set hediff severity so that there's a cooldown on transferral
                    Hediff hediff2 = newHost.health.hediffSet.GetFirstHediffOfDef(HVTRoyaltyDefOf.HVT_THediffWraith);
                    if (hediff2 != null)
                    {
                        hediff2.Severity = 23f;
                    }
                    else
                    {
                        Hediff wraithCD = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_THediffWraith, newHost);
                        newHost.health.AddHediff(wraithCD);
                        wraithCD.Severity = 23f;
                    }
                    //psycast copy-over
                    List<AbilityDef> abilitiesToRemove = new List<AbilityDef>();
                    foreach (Ability a in newHost.abilities.abilities)
                    {
                        if (!(a is Psycast))
                        {
                            abilitiesToRemove.Add(a.def);
                        }
                    }
                    foreach (AbilityDef ad in abilitiesToRemove)
                    {
                        newHost.abilities.RemoveAbility(ad);
                    }
                    foreach (Ability ability in pawn.abilities.abilities)
                    {
                        if (newHost.abilities.GetAbility(ability.def, false) == null && ability is Psycast)
                        {
                            newHost.abilities.GainAbility(ability.def);
                        }
                    }
                    VEF.Abilities.CompAbilities comp = pawn.GetComp<VEF.Abilities.CompAbilities>();
                    VEF.Abilities.CompAbilities comp2 = newHost.GetComp<VEF.Abilities.CompAbilities>();
                    if (comp != null && comp2 != null)
                    {
                        List<VEF.Abilities.Ability> learnedAbilities = HautsTraitsRoyalty.GetInstanceField(typeof(VEF.Abilities.CompAbilities), comp, "learnedAbilities") as List<VEF.Abilities.Ability>;
                        List<VEF.Abilities.Ability> learnedAbilities2 = HautsTraitsRoyalty.GetInstanceField(typeof(VEF.Abilities.CompAbilities), comp2, "learnedAbilities") as List<VEF.Abilities.Ability>;
                        for (int i = learnedAbilities2.Count - 1; i >= 0; i--)
                        {
                            if (ModCompatibilityUtility.IsVPEPsycast(learnedAbilities2[i]))
                            {
                                learnedAbilities2.RemoveAt(i);
                            }
                        }
                        for (int i = 0; i < learnedAbilities.Count; i++)
                        {
                            if (!comp2.HasAbility(learnedAbilities[i].def) && ModCompatibilityUtility.IsVPEPsycast(learnedAbilities[i]))
                            {
                                comp2.GiveAbility(learnedAbilities[i].def);
                                ModCompatibilityUtility.VPEUnlockAbility(newHost, learnedAbilities[i].def);
                                ModCompatibilityUtility.VPESetSkillPointsAndExperienceTo(newHost, pawn);
                            }
                        }
                    }
                    //I rewrote a lot of Wraith to use the code from Anomaly's duplication, but this is the only thing I've 100% ripped off bc I didn't even think of it
                    if (pawn.guest != null && newHost.guest != null)
                    {
                        newHost.guest.Recruitable = pawn.guest.Recruitable;
                    }
                    newHost.Drawer.renderer.SetAllGraphicsDirty();
                    newHost.Notify_DisabledWorkTypesChanged();
                    //send you a letter
                    LookTargets lt = null;
                    LetterDef letterNature = null;
                    if (pawn.Faction == Faction.OfPlayerSilentFail)
                    {
                        if (newHost.Spawned)
                        {
                            lt = newHost;
                        }
                        letterNature = LetterDefOf.PositiveEvent;
                    }
                    else if (newHostOldFaction == Faction.OfPlayerSilentFail)
                    {
                        if (newHost.Spawned)
                        {
                            lt = newHost;
                        }
                        letterNature = LetterDefOf.NegativeEvent;
                    }
                    if (letterNature != null)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                "HVT_WraithLabel".Translate(newHost.Name.ToStringShort), "HVT_WraithText".Translate(pawn.Name.ToStringFull, newHostOldName).CapitalizeFirst(), letterNature, lt, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    Hediff hediff3 = HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_Wraithform, newHost, null);
                    newHost.health.AddHediff(hediff3);
                    hediff3.Severity = hediff3.def.maxSeverity;
                    FleckMaker.Static(newHost.Position, newHost.MapHeld, FleckDefOf.PsycastAreaEffect, 6f);
                    foreach (Thing thing in GenRadial.RadialDistinctThingsAround(newHost.Position, newHost.Map, 12f, true))
                    {
                        if (newHost.Faction == null || thing.Faction != newHost.Faction)
                        {
                            if (thing is Pawn p)
                            {
                                Pawn_StanceTracker stances = p.stances;
                                if (stances != null)
                                {
                                    StunHandler stunner = stances.stunner;
                                    if (stunner != null)
                                    {
                                        stunner.StunFor((int)(Rand.Value * 6f * newHost.GetStatValue(StatDefOf.PsychicSensitivity)), newHost, false);
                                    }
                                }
                            }
                            else if (thing is Building b)
                            {
                                CompStunnable comp3 = b.GetComp<CompStunnable>();
                                if (comp3 != null)
                                {
                                    comp3.StunHandler.StunFor((int)(Rand.Value * 6f * newHost.GetStatValue(StatDefOf.PsychicSensitivity)), newHost, false);
                                }
                            }
                        }
                    }
                    //what if lost????
                    if (!newHost.SpawnedOrAnyParentSpawned && newHost.GetCaravan() == null)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                "HVT_WraithLabel".Translate(newHost.Name.ToStringShort), "HVT_WraithLost".Translate(pawn.Name.ToStringFull, newHostOldName).CapitalizeFirst(), LetterDefOf.Death, null, null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    if (hediff != null)
                    {
                        hediff.transferredOut = true;
                    }
                }
            }
        }
    }
}
