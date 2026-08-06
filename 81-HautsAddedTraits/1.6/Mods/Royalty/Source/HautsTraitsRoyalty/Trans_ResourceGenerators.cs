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
    /*If the mech faction is deactivated or nonexistent, Apocritons will periodically produce the boss-exclusive mechanitor resources via a hidden hediff.
     * Its comp contains a list of ApocritonChipManufactureDefs - produce one "thing" after a random number of hours from within "timerHours" range. Originally a dictionary, but I disliked having to xpath for Alpha Mechs' boss resources,
     * so now a list of unique data structures. Dictionaries hate it when you try and use MayRequire.
     * Each different item is generated on its own timer - a "ChipProductionLine" which is stored in the comp's list.*/
    public class ApocritonChipManufactureDef : Def
    {
        public ThingDef thing;
        public IntRange timerHours;
    }
    public class HediffCompProperties_TheLastChipFactory : HediffCompProperties
    {
        public HediffCompProperties_TheLastChipFactory()
        {
            this.compClass = typeof(HediffComp_TheLastChipFactory);
        }
        public List<ApocritonChipManufactureDef> productionSet;
    }
    public class HediffComp_TheLastChipFactory : HediffComp
    {
        public HediffCompProperties_TheLastChipFactory Props
        {
            get
            {
                return (HediffCompProperties_TheLastChipFactory)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.productionQueues == null)
            {
                this.productionQueues = new List<ChipProductionLine>();
            }
            if (this.Pawn.IsHashIntervalTick(2500, delta) && this.Pawn.inventory != null && (Faction.OfMechanoids == null || Faction.OfMechanoids.deactivated))
            {
                foreach (ApocritonChipManufactureDef acmd in this.Props.productionSet)
                {
                    if (!this.productionQueues.ContainsAny((ChipProductionLine cpl) => cpl.thingDef == acmd.thing))
                    {
                        this.productionQueues.Add(new ChipProductionLine(acmd.thing, acmd.timerHours.RandomInRange));
                    }
                }
                foreach (ChipProductionLine cpl2 in this.productionQueues)
                {
                    if (cpl2.hoursRemaining <= 0)
                    {
                        Thing thing = ThingMaker.MakeThing(cpl2.thingDef, null);
                        this.Pawn.inventory.innerContainer.TryAdd(thing, true);
                        ApocritonChipManufactureDef acmd2 = this.Props.productionSet.Where((ApocritonChipManufactureDef manuDef) => manuDef.thing == cpl2.thingDef).RandomElement();
                        cpl2.hoursRemaining = acmd2.timerHours.RandomInRange;
                    }
                    else
                    {
                        cpl2.hoursRemaining--;
                    }
                }
            }
        }
        public List<ChipProductionLine> productionQueues;
    }
    public class ChipProductionLine
    {
        public ChipProductionLine(ThingDef td, int hours)
        {
            this.thingDef = td;
            this.hoursRemaining = hours;
        }
        public ThingDef thingDef;
        public int hoursRemaining;
    }
    /*player Cockatiels passively accrue some sort of intellectual resource. It only does one of these at a time, stopping at the first one in this list it can do:
     * -VGE gravdata points if you have a selected gravship research project
     * -regular research points if you have a selected project
     * -Anomaly research points if you have a selected project
     * -xp in a random skill and psyfocus
     * The research ones scale with the appropriate research stat, and everything except for psyfocus scales with psy sensitivity (allButPsyfocusScalar). You can regen psyfocus quickly by deselecting all research projects.
     * If you have RimLanguages, you also gain progress towards learning a random language once an hour*/
    public class HediffCompProperties_BigBrain : HediffCompProperties
    {
        public HediffCompProperties_BigBrain()
        {
            this.compClass = typeof(HediffComp_BigBrain);
        }
        public float researchPerHour;
        public float gravdataPerHour;
        public float darkKnowledgePerHour;
        public float skillPerHour;
        public StatDef allButPsyfocusScalar;
        public float psyfocusPerHour;
    }
    public class HediffComp_BigBrain : HediffComp
    {
        public HediffCompProperties_BigBrain Props
        {
            get
            {
                return (HediffCompProperties_BigBrain)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(250, delta))
            {
                bool didResearch = false;
                if (this.Pawn.Faction != null && this.Pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    if (ModCompatibilityUtility.AddGravdata(this.Pawn, this.Props.gravdataPerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) / 10f))
                    {
                        didResearch = true;
                    }
                    else if (Find.ResearchManager.GetProject(null) != null)
                    {
                        Find.ResearchManager.ResearchPerformed(this.Props.researchPerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) * Math.Max(0.08f, this.Pawn.GetStatValue(StatDefOf.ResearchSpeed)) / 0.0825f, this.Pawn);
                        didResearch = true;
                    }
                    else if (ModsConfig.AnomalyActive && (Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Basic) != null || Find.ResearchManager.GetProject(KnowledgeCategoryDefOf.Advanced) != null))
                    {
                        Find.ResearchManager.ApplyKnowledge(KnowledgeCategoryDefOf.Advanced, this.Props.darkKnowledgePerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) * Math.Max(0.08f, this.Pawn.GetStatValue(StatDefOf.EntityStudyRate)) / 10f);
                        didResearch = true;
                    }
                }
                if (!didResearch)
                {
                    if (this.Pawn.psychicEntropy != null)
                    {
                        this.Pawn.psychicEntropy.OffsetPsyfocusDirectly(this.Props.psyfocusPerHour / 10f);
                    }
                    if (this.Pawn.skills != null)
                    {
                        SkillRecord sr = this.Pawn.skills.skills.RandomElement();
                        this.Pawn.skills.Learn(sr.def, this.Props.skillPerHour * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar) / 10f);
                    }
                }
                if (this.Pawn.IsHashIntervalTick(2500, delta))
                {
                    ModCompatibilityUtility.LearnLanguage(this.Pawn, null, 0.05f * this.Pawn.GetStatValue(this.Props.allButPsyfocusScalar));
                }
            }
        }
    }
    /*Ivies periodically increase the growth of all plants in their aura by a small amount. The aura only inflicts its entangling debuff on pawns who share a cell with a plant or are cardinally/diagonally adjacent to a tree*/
    public class HediffCompProperties_Kudzu : HediffCompProperties_AuraHediff
    {
        public HediffCompProperties_Kudzu()
        {
            this.compClass = typeof(HediffComp_Kudzu);
        }
        public float bonusPlantGrowth;
    }
    public class HediffComp_Kudzu : HediffComp_AuraHediff
    {
        public new HediffCompProperties_Kudzu Props
        {
            get
            {
                return (HediffCompProperties_Kudzu)this.props;
            }
        }
        public override void AffectSelf()
        {
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, this.FunctionalRange, true).OfType<Plant>().Distinct<Plant>())
                {
                    if (!plant.Blighted && plant.LifeStage > PlantLifeStage.Sowing)
                    {
                        plant.Growth += (this.Props.bonusPlantGrowth * this.Props.tickPeriodicity * plant.GrowthRateFactor_Fertility) / (60000f * plant.def.plant.growDays);
                        plant.DirtyMapMesh(plant.Map);
                    }
                }
            }
        }
        public override bool ValidatePawn(Pawn self, Pawn p, bool inCaravan)
        {
            if (p.SpawnedOrAnyParentSpawned)
            {
                bool preValidation = false;
                if (p.PositionHeld.GetPlant(p.MapHeld) != null)
                {
                    preValidation = true;
                }
                else
                {
                    foreach (Plant plant in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, 1.49f, true).OfType<Plant>().Distinct<Plant>())
                    {
                        if (plant.def.plant.IsTree)
                        {
                            preValidation = true;
                        }
                    }
                }
                if (preValidation)
                {
                    return base.ValidatePawn(self, p, inCaravan);
                }
            }
            return false;
        }
    }
    //Leghorns periodically produce meals of a specified def. A separate comp speeds up the severity-based "cooldown" via meditation. Probably the weakest transcendence overall.
    public class HediffCompProperties_Leghorn : HediffCompProperties_ForcedByOtherProperty
    {
        public HediffCompProperties_Leghorn()
        {
            this.compClass = typeof(HediffComp_Leghorn);
        }
        public ThingDef mealDef;
    }
    public class HediffComp_Leghorn : HediffComp_ForcedByOtherProperty
    {
        public new HediffCompProperties_Leghorn Props
        {
            get
            {
                return (HediffCompProperties_Leghorn)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.Severity == this.parent.def.maxSeverity)
            {
                if (this.Pawn.Spawned || this.Pawn.GetCaravan() != null)
                {
                    this.parent.Severity = 0.001f;
                    int mealsToPlace = Math.Min(Math.Max((int)(2 * this.Pawn.GetPsylinkLevel() * this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity)), 1), 100);
                    while (mealsToPlace > 0)
                    {
                        Thing fineMeal = ThingMaker.MakeThing(this.Props.mealDef);
                        fineMeal.stackCount = Math.Min(fineMeal.def.stackLimit, mealsToPlace);
                        mealsToPlace -= fineMeal.stackCount;
                        if (this.Pawn.Spawned)
                        {
                            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, 6, null);
                            GenPlace.TryPlaceThing(fineMeal, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
                            FleckMaker.AttachedOverlay(fineMeal, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                            FleckMaker.AttachedOverlay(fineMeal, FleckDefOf.PsycastSkipOuterRingExit, Vector3.zero, 1f, -1f);
                            fineMeal.Notify_DebugSpawned();
                        }
                        else if (this.Pawn.GetCaravan() != null)
                        {
                            this.Pawn.GetCaravan().AddPawnOrItem(fineMeal, true);
                        }
                    }
                    if (this.Pawn.Spawned)
                    {
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                    }
                }
            }
        }
    }
    //Musang uses a CreateThingsBySpendingSeverity comp, but it can spawn any psytrainer or skilltrainer as well as its whitelist of items.
    public class HediffCompProperties_KopiLuwak : HediffCompProperties_CreateThingsBySpendingSeverity
    {
        public HediffCompProperties_KopiLuwak()
        {
            this.compClass = typeof(HediffComp_KopiLuwak);
        }
    }
    public class HediffComp_KopiLuwak : HediffComp_CreateThingsBySpendingSeverity
    {
        public new HediffCompProperties_KopiLuwak Props
        {
            get
            {
                return (HediffCompProperties_KopiLuwak)this.props;
            }
        }
        public override KeyValuePair<ThingDef, FloatRange> GetThingToSpawn()
        {
            if (Rand.Chance(2f / (this.Props.spawnedThingAndCountPerTrigger.Count + 2f)))
            {
                ThingDef tDef;
                if (Rand.Chance(0.5f))
                {
                    tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                            where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.NeurotrainersPsycast)
                            select tdef).RandomElement();
                } else {
                    tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                            where tdef.thingCategories != null && tdef.thingCategories.Contains(ThingCategoryDefOf.NeurotrainersSkill)
                            select tdef).RandomElement();
                }
                return new KeyValuePair<ThingDef, FloatRange>(tDef, new FloatRange(1f));
            }
            return base.GetThingToSpawn();
        }
    }
    /*Quahogs' apparel and wielded equipment rapidly regenerate. While meditating (determined by a hediff comp which changes severity when meditating), there is a small chance to improve the quality of a worn or wielded item by 1.
     * There is a VERY small chance (0.5%) per 40s (almost an hour) while meditating to make a random book or minifiable art building which is spawned somewhere nearby, complete with the ever-reliable skip graphic.*/
    public class Hediff_Pearl : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            if (this.pawn.IsHashIntervalTick(60, delta))
            {
                if (this.pawn.apparel != null)
                {
                    foreach (Apparel a in this.pawn.apparel.WornApparel)
                    {
                        if (a.def.useHitPoints)
                        {
                            a.HitPoints = Math.Min(a.HitPoints + 1, a.MaxHitPoints);
                        }
                    }
                }
                if (this.pawn.equipment != null)
                {
                    foreach (Thing t in this.pawn.equipment.AllEquipmentListForReading)
                    {
                        if (t.def.useHitPoints)
                        {
                            t.HitPoints = Math.Min(t.HitPoints + 1, t.MaxHitPoints);
                        }
                    }
                }
                if (this.pawn.IsHashIntervalTick(2400, delta) && this.Severity >= 1f)
                {
                    float mfg = this.pawn.GetStatValue(StatDefOf.MeditationFocusGain);
                    if (Rand.Chance(0.08f * mfg))
                    {
                        List<CompQuality> items = new List<CompQuality>();
                        if (this.pawn.apparel != null)
                        {
                            foreach (Apparel a in this.pawn.apparel.WornApparel)
                            {
                                CompQuality qc = a.TryGetComp<CompQuality>();
                                if (qc != null)
                                {
                                    items.Add(qc);
                                }
                            }
                        }
                        if (this.pawn.equipment != null)
                        {
                            foreach (Thing t in this.pawn.equipment.AllEquipmentListForReading)
                            {
                                CompQuality qc = t.TryGetComp<CompQuality>();
                                if (qc != null)
                                {
                                    items.Add(qc);
                                }
                            }
                        }
                        if (items.Count > 0)
                        {
                            CompQuality cq = items.RandomElement();
                            cq.SetQuality((QualityCategory)Mathf.Min((int)(cq.Quality + (byte)1), 6), null);
                        }
                    }
                    if (Rand.Chance(0.005f * mfg))
                    {
                        ThingDef itemDef;
                        if (Rand.Chance(0.5f))
                        {
                            itemDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                       where tdef.thingClass != null && tdef.thingClass == typeof(Book)
                                       select tdef).RandomElement();
                        }
                        else
                        {
                            itemDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                       where tdef.thingClass != null && tdef.thingClass == typeof(Building_Art) && tdef.Minifiable && tdef.building != null && tdef.building.expandHomeArea == true
                                       select tdef).RandomElement();
                        }
                        if (itemDef != null)
                        {
                            ThingDef stuff = GenStuff.RandomStuffFor(itemDef);
                            Thing thing = ThingMaker.MakeThing(itemDef, stuff);
                            CompQuality compQuality = thing.TryGetComp<CompQuality>();
                            if (compQuality != null)
                            {
                                compQuality.SetQuality(Rand.Chance(0.8f) ? QualityCategory.Masterwork : QualityCategory.Legendary, ArtGenerationContext.Colony);
                            }
                            if (thing.def.Minifiable)
                            {
                                thing = thing.MakeMinified();
                            }
                            if (thing.def.CanHaveFaction)
                            {
                                thing.SetFaction(this.pawn.Faction, null);
                            }
                            if (this.pawn.Spawned)
                            {
                                IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.Position, this.pawn.Map, 6, null);
                                GenPlace.TryPlaceThing(thing, loc, this.pawn.Map, ThingPlaceMode.Near, null, null, default);
                                thing.Notify_DebugSpawned();
                                if (thing.SpawnedOrAnyParentSpawned && thing.Map != null)
                                {
                                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, FleckDefOf.PsycastSkipInnerExit, 1f);
                                    dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                                    dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                    thing.Map.flecks.CreateFleck(dataStatic);
                                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(thing.Position.ToVector3Shifted(), thing.Map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
                                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                                    thing.Map.flecks.CreateFleck(dataStatic2);
                                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(thing.Position, thing.Map, false));
                                }
                            }
                            else if (this.pawn.inventory != null)
                            {
                                this.pawn.inventory.innerContainer.TryAdd(thing, true);
                                thing.Notify_DebugSpawned();
                            }
                        }
                    }
                    this.Severity -= 1f;
                }
            }
            base.TickInterval(delta);
        }
    }
    /*Robins on moving caravans have a small, psysens-scaling chance to find random items (and this also sends you a letter when it happens). There's a bunch of restrictions imposed here on WHAT those items can be,
     * which tells you I probably should've just gone for a ThingSetMaker instead of winnowing down the ThingDef database, but alas. This does distinguish its output a lot from Magpie and Scrub Jay!*/
    public class Hediff_Robin : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(300, delta) && this.pawn.GetCaravan() != null && this.pawn.GetCaravan().pather.MovingNow && Rand.Chance(Math.Min(0.05f, 0.004f * this.pawn.GetStatValue(StatDefOf.PsychicSensitivity))))
            {
                float maxValue = Rand.Chance(0.9f) ? Math.Min(6f, this.pawn.GetPsylinkLevel()) * 600f : -1f;
                ThingDef tDef = (from tdef in DefDatabase<ThingDef>.AllDefsListForReading
                                 where ((tdef.BaseMarketValue <= maxValue || maxValue < 0f) && (tdef.category == ThingCategory.Item || (tdef.category == ThingCategory.Building && tdef.Minifiable)) && !tdef.thingClass.IsAssignableFrom(typeof(MinifiedThing)) && !tdef.thingClass.IsAssignableFrom(typeof(MinifiedTree)) && !tdef.thingClass.IsAssignableFrom(typeof(Corpse)) && !tdef.thingClass.IsAssignableFrom(typeof(UnfinishedThing)) && tdef.PlayerAcquirable)
                                 select tdef).RandomElement();
                if (tDef != null)
                {
                    Thing thing = ThingMaker.MakeThing(tDef, GenStuff.RandomStuffFor(tDef));
                    CompQuality cq = thing.TryGetComp<CompQuality>();
                    if (cq != null)
                    {
                        cq.SetQuality(QualityUtility.GenerateQuality(Rand.Chance(0.8f) ? QualityGenerator.BaseGen : QualityGenerator.Reward), ArtGenerationContext.Outsider);
                    }
                    thing.PostPostMake();
                    thing.stackCount = Math.Min(tDef.stackLimit, maxValue > 0f ? (int)Math.Ceiling(maxValue / tDef.BaseMarketValue) : tDef.stackLimit);
                    if (tDef.Minifiable)
                    {
                        Thing thing2 = thing.MakeMinified();
                        this.pawn.GetCaravan().AddPawnOrItem(thing2, true);
                    }
                    else
                    {
                        this.pawn.GetCaravan().AddPawnOrItem(thing, true);
                    }
                    thing.Notify_DebugSpawned();
                    Messages.Message("HVT_RobinGetItem".Translate().CapitalizeFirst().Formatted(this.pawn.Name.ToStringShort, thing.Label).Resolve(), null, MessageTypeDefOf.PositiveEvent, true);
                }
            }
        }
    }
    /*Scrub Jays not only have an absurd amount of inventory space, they also have the ability to fill it while meditating. Severity gains while meditating due to a comp, and on hitting the max it resets and a bunch of
     * items from one of the vanilla ThingSetMakers are generated into their inventory. The number of items that can be generated at once is both capped by psysens (sort of) and by psylink level.*/
    public class Hediff_Hammerspace : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.maxSeverity)
            {
                this.Severity = 0.001f;
                List<Thing> list = new List<Thing>();
                int tries = 10;
                while (list.Count < this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) * (1f + Rand.Value) && tries > 0)
                {
                    ThingSetMakerDef thingSetMakerDef;
                    float treasure = Rand.Value;
                    if (treasure <= 0.4f)
                    {
                        thingSetMakerDef = ThingSetMakerDefOf.DebugCaravanInventory;
                    }
                    else if (treasure <= 0.8f)
                    {
                        thingSetMakerDef = ThingSetMakerDefOf.ResourcePod;
                    }
                    else if (treasure <= 0.95f)
                    {
                        thingSetMakerDef = ThingSetMakerDefOf.MapGen_AncientTempleContents;
                    }
                    else
                    {
                        thingSetMakerDef = ThingSetMakerDefOf.Reward_ItemsStandard;
                    }
                    list = thingSetMakerDef.root.Generate(default(ThingSetMakerParams));
                    tries--;
                }
                for (int i = list.Count - 1; i >= this.pawn.GetPsylinkLevel(); i--)
                {
                    list.Remove(list[i]);
                }
                if (list.Count > 0)
                {
                    foreach (Thing t in list)
                    {
                        t.stackCount = (int)Math.Min(Rand.Value * 100f, t.def.stackLimit);
                        this.pawn.inventory.innerContainer.TryAdd(t, true);
                        t.Notify_DebugSpawned();
                    }
                    SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                }
            }
        }
    }
    /*yanno, there's an argument to be made here that this Vulture comp should be in Trans_PawnGenerators.cs. It is making pawns after all, they just happen to be dead.
     * Otoh, the wiki calls corpse management "Human RESOURCES", so I'm being playerbase terminology-compliant. This is very very like a CreateThingsBySpendingSeverity, and probably could've been done with that system,
     * but there's enough fiddly unique bits about generating corpses I went ahead and wrote it from scratch.
     * Corpses spawn without equipment or apparel.
     * spawnRadius: how far away you can place a corpse - corpses only spawn if the Vulture is spawned, so no special clauses about caravans or what-have-yous.
     * setToOwnFaction: ewisott for the inner pawns of the corpses
     * severityToTrigger: on reaching this severity, start spawning corpses. A PsyfocusSpentTracker comp is the source of severity gain that powers this.
     * The Fleck and Sound data play over the created items, obviously.*/
    public class HediffCompProperties_CarrionSpawn : HediffCompProperties
    {
        public HediffCompProperties_CarrionSpawn()
        {
            this.compClass = typeof(HediffComp_CarrionSpawn);
        }
        public float severityToTrigger;
        public bool setToOwnFaction = false;
        public float spawnRadius;
        public bool showProgressInTooltip = true;
        public float humanlikeChance;
        public FleckDef spawnFleck1;
        public FleckDef spawnFleck2;
        public SoundDef spawnSound;
    }
    public class HediffComp_CarrionSpawn : HediffComp
    {
        public HediffCompProperties_CarrionSpawn Props
        {
            get
            {
                return (HediffCompProperties_CarrionSpawn)this.props;
            }
        }
        public override void CompPostMake()
        {
            base.CompPostMake();
        }
        public override string CompTipStringExtra
        {
            get
            {
                if (this.Props.showProgressInTooltip)
                {
                    return base.CompTipStringExtra + "Hauts_TilNextSpawn".Translate(this.parent.Severity, this.Props.severityToTrigger);
                }
                return base.CompTipStringExtra;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.Severity >= this.Props.severityToTrigger && this.Pawn.Spawned)
            {
                this.SpawnThings();
            }
        }
        public void SpawnThings()
        {
            List<PawnKindDef> corpseList = new List<PawnKindDef>();
            float humanlikeChance = Rand.Value;
            foreach (PawnKindDef pkd in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (humanlikeChance <= this.Props.humanlikeChance || !pkd.race.race.Humanlike)
                {
                    corpseList.Add(pkd);
                }
            }
            PawnKindDef kind = corpseList.RandomElement();
            Pawn pawn = PawnGenerator.GeneratePawn(kind, this.Props.setToOwnFaction ? this.Pawn.Faction : null);
            pawn.health.SetDead();
            if (pawn.apparel != null)
            {
                pawn.apparel.DestroyAll(DestroyMode.Vanish);
            }
            if (pawn.equipment != null)
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            }
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            Corpse corpse = pawn.MakeCorpse(null, null);
            corpse.Age = Mathf.RoundToInt((float)(Rand.Value * 60000000));
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.Position, this.Pawn.Map, (int)Math.Ceiling(this.Props.spawnRadius), null);
            GenPlace.TryPlaceThing(corpse, loc, this.Pawn.Map, ThingPlaceMode.Near, null, null, default);
            corpse.Notify_DebugSpawned();
            if (corpse.SpawnedOrAnyParentSpawned && corpse.Map != null && this.Props.spawnFleck1 != null)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(corpse.Position.ToVector3Shifted(), corpse.Map, this.Props.spawnFleck1, 1f);
                dataStatic.rotationRate = (float)Rand.Range(-30, 30);
                dataStatic.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                corpse.Map.flecks.CreateFleck(dataStatic);
                if (this.Props.spawnFleck2 != null)
                {
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(corpse.Position.ToVector3Shifted(), corpse.Map, this.Props.spawnFleck2, 1f);
                    dataStatic2.rotationRate = (float)Rand.Range(-30, 30);
                    dataStatic2.rotation = (float)(90 * Rand.RangeInclusive(0, 3));
                    corpse.Map.flecks.CreateFleck(dataStatic2);
                }
            }
            this.parent.Severity -= this.Props.severityToTrigger;
        }
    }
    /*Sphinx' "Psychic Riddle" ability inflicts a coma. Its severity decays, and on hitting its minimum value, it can either grant a psylink or inflict a stack of failureHediff (Leering Sphinx, which kills on the third stack, see Trans_DeathEffects.cs).
     * Success is guaranteed on a mythic transcendent (e.g. Dragon, Erinys, Harpy, etc.) but is otherwise reduced based on the number of psylinks the pawn already has. This is more lenient in VPE since psylink levels go way higher in that system.
     * count: how many psylink levels to grant on success.
     * All the strings: determine the contents of the letter provided to the player, which describe the result*/
    public class HediffCompProperties_MaybeGrantPsylink : HediffCompProperties
    {
        public HediffCompProperties_MaybeGrantPsylink()
        {
            this.compClass = typeof(HediffComp_MaybeGrantPsylink);
        }
        public int count;
        public HediffDef failureHediff;
        [MustTranslate]
        public string succeedLetterLabel;
        [MustTranslate]
        public string succeedLetterText;
        [MustTranslate]
        public string failLetterLabel;
        [MustTranslate]
        public string failLetterText;
        [MustTranslate]
        public string killLetterLabel;
        [MustTranslate]
        public string killLetterText;
    }
    public class HediffComp_MaybeGrantPsylink : HediffComp
    {
        public HediffCompProperties_MaybeGrantPsylink Props
        {
            get
            {
                return (HediffCompProperties_MaybeGrantPsylink)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.Severity <= this.parent.def.minSeverity)
            {
                Pawn pawn = this.Pawn;
                float successChance = 1f;
                if (!PsychicTraitAndGeneCheckUtility.IsMythicTranscendent(pawn))
                {
                    if (ModsConfig.IsActive("VanillaExpanded.VPsycastsE"))
                    {
                        Hediff_Level psylink = (Hediff_Level)pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamedSilentFail("VPE_PsycastAbilityImplant"));
                        if (psylink != null)
                        {
                            successChance -= ((float)psylink.level / 100f);
                        }
                    }
                    else
                    {
                        successChance -= ((float)pawn.GetPsylinkLevel() / (float)pawn.GetMaxPsylinkLevel());
                    }
                }
                if (Rand.Value <= successChance)
                {
                    pawn.ChangePsylinkLevel(this.Props.count, false);
                    ChoiceLetter notification = LetterMaker.MakeLetter(
                    this.Props.succeedLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.succeedLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                    Find.LetterStack.ReceiveLetter(notification, null);
                }
                else if (!pawn.health.hediffSet.HasHediff(this.Props.failureHediff))
                {
                    Hediff hediff = HediffMaker.MakeHediff(this.Props.failureHediff, pawn, null);
                    pawn.health.AddHediff(hediff);
                    ChoiceLetter notification = LetterMaker.MakeLetter(
                    this.Props.failLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.failLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(pawn), null, null, null);
                    Find.LetterStack.ReceiveLetter(notification, null);
                }
                else
                {
                    pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.failureHediff).Severity += 1f;
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.failureHediff).Severity < 2f)
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        this.Props.failLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.failLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(pawn), null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                    else
                    {
                        ChoiceLetter notification = LetterMaker.MakeLetter(
                        this.Props.killLetterLabel.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), this.Props.killLetterText.CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(this.parent.pawn), null, null, null);
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                }
                pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    /*Thunderbirds' "Seed of Psychic Wisdom" ability also inflicts a coma. It either grants a pawn the Latent Psychic trait (if not a latent or woke psychic already), or has chanceToAwaken chance to grant an awakening to a latent or woke pawn.
     * Sends a letter describing the effect; the language keys are hardcoded.
     * As handled by the ability comp, the ability can't target a pawn that has at least maxAwakenings awakened traits. Any number of woke genes is fine though.*/
    public class HediffCompProperties_PsychicWisdom : HediffCompProperties
    {
        public HediffCompProperties_PsychicWisdom()
        {
            this.compClass = typeof(HediffComp_PsychicWisdom);
        }
        public float chanceToAwaken;
    }
    public class HediffComp_PsychicWisdom : HediffComp
    {
        public HediffCompProperties_PsychicWisdom Props
        {
            get
            {
                return (HediffCompProperties_PsychicWisdom)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.parent.Severity <= this.parent.def.minSeverity)
            {
                Pawn pawn = this.Pawn;
                if (pawn.story != null)
                {
                    ChoiceLetter notification;
                    if (pawn.story.traits.HasTrait(HVTRoyaltyDefOf.HVT_LatentPsychic) || PsychicTraitAndGeneCheckUtility.IsAwakenedPsychic(pawn, false))
                    {
                        if (Rand.Value <= this.Props.chanceToAwaken)
                        {
                            AwakeningMethodsUtility.AwakenPsychicTalent(pawn, true, "HVT_GetThunderbirdstruck".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_GetThunderbirdstruck".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve());
                        }
                        else
                        {
                            notification = LetterMaker.MakeLetter(
                        "HVT_ThunderbirdFailLetter".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdFailText".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.NeutralEvent, new LookTargets(pawn), null, null, null);
                            Find.LetterStack.ReceiveLetter(notification, null);
                        }
                    }
                    else
                    {
                        int degree = (int)Math.Ceiling(Rand.Value * HVTRoyaltyDefOf.HVT_LatentPsychic.degreeDatas.Count);
                        Trait toGain = new Trait(HVTRoyaltyDefOf.HVT_LatentPsychic, degree);
                        pawn.story.traits.GainTrait(toGain, true);
                        if (ModCompatibilityUtility.IsHighFantasy())
                        {
                            notification = LetterMaker.MakeLetter(
                            "HVT_ThunderbirdLatencyLetterFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdLatencyTextFantasy".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                        }
                        else
                        {
                            notification = LetterMaker.MakeLetter(
                            "HVT_ThunderbirdLatencyLetter".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), "HVT_ThunderbirdLatencyText".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), LetterDefOf.PositiveEvent, new LookTargets(pawn), null, null, null);
                        }
                        Find.LetterStack.ReceiveLetter(notification, null);
                    }
                }
                pawn.health.RemoveHediff(this.parent);
            }
        }
    }
    public class CompProperties_AbilityPsychicAwakening : CompProperties_AbilityEffect
    {
        public float maxAwakenings = 2;
    }
    public class CompAbilityEffect_PsychicAwakening : CompAbilityEffect
    {
        public new CompProperties_AbilityPsychicAwakening Props
        {
            get
            {
                return (CompProperties_AbilityPsychicAwakening)this.props;
            }
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn;
            if ((pawn = (target.Thing as Pawn)) != null)
            {
                if (pawn.story == null)
                {
                    return false;
                }
                int wokes = 0;
                foreach (Trait t in pawn.story.traits.TraitsSorted)
                {
                    if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def))
                    {
                        wokes++;
                    }
                }
                if (wokes >= this.Props.maxAwakenings)
                {
                    if (throwMessages)
                    {
                        Messages.Message("HVT_WontTargetAwakened2".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
            return base.Valid(target, throwMessages);
        }
    }
}
