using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace HautsTraitsRoyalty
{
    /*Psychic Aptenodytes (emperor penguins) cause Men in Black (or women, the gender has been explicitly unfixed in this case) to appear when any awakening condition is met. Kind of.
     * For conditions that can be triggered by meeting a passive requirement (e.g. Mastery's 15 levels in any skill), it's a small chance to trigger per hour.
     * -Mastery's 15 levels in any skill or Love's >=500 net positive opinion of others: 0.1% chance
     * -Life's being immune to an illness in a life-threatening stage: 7.5% chance (this is rarer to provoke, hence the much higher chance)
     * Although the Death awakening also works with Emperor Penguins, they don't instantly resurrect the way that Death-type Latent Psychics do; instead it's on a 6d delay. This is for balance reasons.*/
    public class Hediff_Aptenodytes : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta))
            {
                if (Rand.Chance(0.0001f))
                {
                    for (int i = 0; i < this.pawn.skills.skills.Count; i++)
                    {
                        if (this.pawn.skills.skills[i].GetLevel() >= 15)
                        {
                            PsychicPowerUtility.ColonyHuddle(this.pawn);
                            return;
                        }
                    }
                    if (this.Severity >= 500f)
                    {
                        PsychicPowerUtility.ColonyHuddle(this.pawn);
                    }
                }
                foreach (Hediff h in this.pawn.health.hediffSet.hediffs)
                {
                    if (h.CurStage != null && h.CurStage.lifeThreatening && h.FullyImmune())
                    {
                        if (Rand.Value <= 0.075f)
                        {
                            PsychicPowerUtility.ColonyHuddle(this.pawn);
                            return;
                        }
                    }
                }
            }
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Pawn pawnToRez = this.pawn.Corpse != null ? this.pawn.Corpse.InnerPawn : this.pawn;
            Messages.Message("HVT_PengwengCanRez".Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve(), this.pawn.Corpse != null ? this.pawn.Corpse : null, MessageTypeDefOf.NeutralEvent, true);
            HautsMiscUtility.StartDelayedResurrection(this.pawn, new IntRange(1440), "HVT_PengwengRez", true, true, true, null);
            PsychicPowerUtility.ColonyHuddle(this.pawn);
        }
        public override void Notify_Resurrected()
        {
            base.Notify_Resurrected();
            PsychicPowerUtility.ColonyHuddle(this.pawn);
        }
    }
    /*Every 24 hours, Locusts generate 1 Locustspawn per 100% psysens (rounded down). Locustspawn gain a trait that makes them physically weak, psychically sensitive, and also nonexistent after 1 day.
     * While the 1-day lifespan means you will only have one set of Locustspawn at a time, you could probably bypass that by sticking some in cryptosleep and then fishing them out later.
     * Locustspawn also can't have any psychic trait tree traits/genes or psylinks, because this is not 3.5e and using your magic to summon a guy who can do more magic is kind of stupid outside of the Dungeons and Dragons context.
     * Locustspawn have the Locust's faction and ideo. If they're not of the player faction, then they escort the Locust around.
     * They don't have any inventory, and their health gets a touch-up so they have a minimum guaranteed level of utility, their stuff is biocoded and locked if possible. There are tons of workarounds to this looting prevention though, esp with other mods*/
    public class Hediff_Apollyon : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.maxSeverity && this.pawn.SpawnedOrAnyParentSpawned)
            {
                this.pawnsToSpawn.Clear();
                this.Severity = 0.001f;
                for (int i = 0; i < (int)this.pawn.GetStatValue(StatDefOf.PsychicSensitivity); i++)
                {
                    Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.pawn.kindDef, this.pawn.Faction, PawnGenerationContext.NonPlayer, this.pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                    if (newPawn != null)
                    {
                        newPawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LocustClone));
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in newPawn.story.traits.allTraits)
                        {
                            if (t.def == HVTRoyaltyDefOf.HVT_LatentPsychic || PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def) || PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove)
                        {
                            newPawn.story.traits.RemoveTrait(t);
                        }
                        List<Gene> genesToRemove = new List<Gene>();
                        if (ModsConfig.BiotechActive && newPawn.genes != null)
                        {
                            foreach (Gene g in newPawn.genes.GenesListForReading)
                            {
                                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(g.def))
                                {
                                    genesToRemove.Add(g);
                                }
                            }
                        }
                        foreach (Gene g in genesToRemove)
                        {
                            newPawn.genes.RemoveGene(g);
                        }
                        Hediff_Psylink hediff_Psylink = newPawn.GetMainPsylinkSource();
                        if (hediff_Psylink != null)
                        {
                            newPawn.health.RemoveHediff(hediff_Psylink);
                        }
                        for (int j = 0; j < 5; j++)
                        {
                            HealthUtility.FixWorstHealthCondition(newPawn);
                        }
                        newPawn.inventory.DestroyAll();
                        if (newPawn.equipment != null)
                        {
                            foreach (ThingWithComps thingWithComps in newPawn.equipment.AllEquipmentListForReading)
                            {
                                CompBiocodable comp = thingWithComps.GetComp<CompBiocodable>();
                                if (comp != null && !comp.Biocoded)
                                {
                                    comp.CodeFor(newPawn);
                                }
                            }
                        }
                        foreach (Apparel apparel in newPawn.apparel.WornApparel)
                        {
                            PawnApparelGenerator.PostProcessApparel(apparel, newPawn);
                            CompBiocodable comp = apparel.TryGetComp<CompBiocodable>();
                            if (comp != null && !comp.Biocoded)
                            {
                                comp.CodeFor(newPawn);
                            }
                        }
                        newPawn.apparel.LockAll();
                        pawnsToSpawn.Add(newPawn);
                        if (ModsConfig.IdeologyActive && newPawn.ideo != null && this.pawn.ideo != null)
                        {
                            newPawn.ideo.SetIdeo(this.pawn.ideo.Ideo);
                        }
                    }
                }
                if (this.pawn.SpawnedOrAnyParentSpawned)
                {
                    foreach (Pawn p in pawnsToSpawn)
                    {
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.PositionHeld, this.pawn.MapHeld, 6, null);
                        GenPlace.TryPlaceThing(p, loc, this.pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                        FleckMaker.AttachedOverlay(p, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                        DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(p.PositionHeld, p.MapHeld, false));
                        if (!p.IsColonistPlayerControlled)
                        {
                            LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(p));
                        }
                    }
                }
                this.pawnsToSpawn.Clear();
            }
        }
        private List<Pawn> pawnsToSpawn = new List<Pawn>();
    }
    /*Lovebugs create a doppelganger of themselves. It's very like how Anomaly's duplication obelisk works, with tiny chances for small imperfections in the process e.g. different gender or favorite color.
     * They inherit all traits, except obviously for the trait that creates a doppelganger, and instead get a trait that prevents you from harvesting their bionics (there are probably workarounds to this too, off the top of my head
     *   Cooler Psycasts' organ skip could do it).
     * If a Doppel dies, you get a new one after a 1d cooldown, which can be sped up by the hediff's severity-changing comps (meditating or psycasting)*/
    public class Hediff_EchoKnight : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.maxSeverity && (this.doppelganger == null || this.doppelganger.Dead || this.doppelganger.Discarded || this.doppelganger.Destroyed) && this.pawn.Spawned)
            {
                this.MakeDoppelganger();
            }
        }
        private void MakeDoppelganger()
        {
            Pawn pawn = this.pawn;
            float ageBiologicalYearsFloat = pawn.ageTracker.AgeBiologicalYearsFloat;
            float num = pawn.ageTracker.AgeChronologicalYearsFloat;
            if (num > ageBiologicalYearsFloat)
            {
                num = ageBiologicalYearsFloat;
            }
            PawnKindDef kindDef = pawn.kindDef;
            Faction faction = pawn.Faction;
            PawnGenerationContext context = PawnGenerationContext.NonPlayer;
            int tile = -1;
            bool forceGenerateNewPawn = true;
            bool allowDead = false;
            bool allowDowned = false;
            bool canGeneratePawnRelations = false;
            bool mustBeCapableOfViolence = false;
            float colonistRelationChanceFactor = 0f;
            bool forceAddFreeWarmLayerIfNeeded = false;
            bool allowGay = true;
            bool allowPregnant = false;
            bool allowFood = true;
            bool allowAddictions = true;
            bool inhabitant = false;
            bool certainlyBeenInCryptosleep = false;
            bool forceRedressWorldPawnIfFormerColonist = false;
            bool worldPawnFactionDoesntMatter = false;
            float biocodeWeaponChance = 0f;
            float biocodeApparelChance = 0f;
            Pawn extraPawnForExtraRelationChance = null;
            float relationWithExtraPawnChanceFactor = 0f;
            Predicate<Pawn> validatorPreGear = null;
            Predicate<Pawn> validatorPostGear = null;
            IEnumerable<TraitDef> forcedTraits = null;
            IEnumerable<TraitDef> prohibitedTraits = null;
            float? minChanceToRedressWorldPawn = null;
            Gender? fixedGender = new Gender?((Rand.Chance(0.99f) ? pawn.gender : pawn.gender.Opposite()));
            Ideo ideo = pawn.Ideo;
            Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kindDef, faction, context, tile, forceGenerateNewPawn, allowDead, allowDowned, canGeneratePawnRelations, mustBeCapableOfViolence, colonistRelationChanceFactor, forceAddFreeWarmLayerIfNeeded, allowGay, allowPregnant, allowFood, allowAddictions, inhabitant, certainlyBeenInCryptosleep, forceRedressWorldPawnIfFormerColonist, worldPawnFactionDoesntMatter, biocodeWeaponChance, biocodeApparelChance, extraPawnForExtraRelationChance, relationWithExtraPawnChanceFactor, validatorPreGear, validatorPostGear, forcedTraits, prohibitedTraits, minChanceToRedressWorldPawn, new float?(ageBiologicalYearsFloat), new float?(num), fixedGender, null, null, null, ideo, false, false, false, false, null, null, pawn.genes.Xenotype, pawn.genes.CustomXenotype, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, true));
            newPawn.Name = NameTriple.FromString(pawn.Name.ToString(), false);
            newPawn.genes.xenotypeName = pawn.genes.xenotypeName;
            if (Rand.Chance(0.95f) && ModsConfig.IdeologyActive && pawn.story.favoriteColor != null)
            {
                newPawn.story.favoriteColor = pawn.story.favoriteColor;
            }
            newPawn.story.Childhood = pawn.story.Childhood;
            if (Rand.Chance(0.95f))
            {
                newPawn.story.Adulthood = pawn.story.Adulthood;
            }
            newPawn.story.traits.allTraits.Clear();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if ((!ModsConfig.BiotechActive || trait.sourceGene == null) && trait.def != HVTRoyaltyDefOf.HVT_TTraitLovebug)
                {
                    newPawn.story.traits.GainTrait(new Trait(trait.def, trait.Degree, trait.ScenForced), false);
                }
            }
            newPawn.story.traits.GainTrait(new Trait(HVTRoyaltyDefOf.HVT_LovebugDoppel));
            newPawn.genes.Endogenes.Clear();
            newPawn.genes.Xenogenes.Clear();
            foreach (Gene g in pawn.genes.Endogenes)
            {
                newPawn.genes.AddGene(g.def, false);
            }
            foreach (Gene g in pawn.genes.Xenogenes)
            {
                newPawn.genes.AddGene(g.def, true);
            }
            newPawn.story.headType = pawn.story.headType;
            newPawn.story.bodyType = pawn.story.bodyType;
            if (Rand.Chance(0.95f))
            {
                newPawn.story.hairDef = pawn.story.hairDef;
            }
            if (Rand.Chance(0.99f))
            {
                newPawn.story.HairColor = pawn.story.HairColor;
            }
            newPawn.story.SkinColorBase = pawn.story.SkinColorBase;
            newPawn.story.skinColorOverride = pawn.story.skinColorOverride;
            newPawn.story.furDef = pawn.story.furDef;
            if (Rand.Chance(0.95f))
            {
                newPawn.style.beardDef = pawn.style.beardDef;
            }
            if (ModsConfig.IdeologyActive && Rand.Chance(0.95f))
            {
                newPawn.style.BodyTattoo = pawn.style.BodyTattoo;
                newPawn.style.FaceTattoo = pawn.style.FaceTattoo;
            }
            newPawn.skills.skills.Clear();
            foreach (SkillRecord skillRecord in pawn.skills.skills)
            {
                SkillRecord item = new SkillRecord(newPawn, skillRecord.def)
                {
                    levelInt = skillRecord.levelInt,
                    passion = skillRecord.passion,
                    xpSinceLastLevel = skillRecord.xpSinceLastLevel,
                    xpSinceMidnight = skillRecord.xpSinceMidnight
                };
                newPawn.skills.skills.Add(item);
            }
            newPawn.health.hediffSet.hediffs.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in hediffs)
            {
                if (hediff.def.duplicationAllowed && (hediff.Part == null || newPawn.health.hediffSet.HasBodyPart(hediff.Part)))
                {
                    Hediff hediff2 = HediffMaker.MakeHediff(hediff.def, newPawn, hediff.Part);
                    hediff2.CopyFrom(hediff);
                    newPawn.health.hediffSet.AddDirect(hediff2, null, null);
                }
            }
            newPawn.needs.AllNeeds.Clear();
            foreach (Need need in pawn.needs.AllNeeds)
            {
                Need need2 = (Need)Activator.CreateInstance(need.def.needClass, new object[]
                {
                    newPawn
                });
                need2.def = need.def;
                newPawn.needs.AllNeeds.Add(need2);
                need2.SetInitialLevel();
                need2.CurLevel = need.CurLevel;
                newPawn.needs.BindDirectNeedFields();
            }
            if (pawn.needs.mood != null)
            {
                List<Thought_Memory> memories = newPawn.needs.mood.thoughts.memories.Memories;
                memories.Clear();
                foreach (Thought_Memory thought_Memory in pawn.needs.mood.thoughts.memories.Memories)
                {
                    Thought_Memory thought_Memory2 = (Thought_Memory)ThoughtMaker.MakeThought(thought_Memory.def);
                    thought_Memory2.CopyFrom(thought_Memory);
                    thought_Memory2.pawn = newPawn;
                    memories.Add(thought_Memory2);
                }
            }
            foreach (Ability ability in pawn.abilities.abilities)
            {
                if (newPawn.abilities.GetAbility(ability.def, false) == null)
                {
                    newPawn.abilities.GainAbility(ability.def);
                }
            }
            VEF.Abilities.CompAbilities comp = pawn.GetComp<VEF.Abilities.CompAbilities>();
            VEF.Abilities.CompAbilities comp2 = newPawn.GetComp<VEF.Abilities.CompAbilities>();
            if (comp != null && comp2 != null)
            {
                List<VEF.Abilities.Ability> learnedAbilities = HautsTraitsRoyalty.GetInstanceField(typeof(VEF.Abilities.CompAbilities), comp, "learnedAbilities") as List<VEF.Abilities.Ability>;
                for (int i = 0; i < learnedAbilities.Count; i++)
                {
                    if (!comp2.HasAbility(learnedAbilities[i].def))
                    {
                        comp2.GiveAbility(learnedAbilities[i].def);
                        ModCompatibilityUtility.VPEUnlockAbility(newPawn, learnedAbilities[i].def);
                    }
                }
            }
            List<Ability> abilities = newPawn.abilities.abilities;
            for (int i = abilities.Count - 1; i >= 0; i--)
            {
                Ability ability2 = abilities[i];
                if (pawn.abilities.GetAbility(ability2.def, false) == null)
                {
                    newPawn.abilities.RemoveAbility(ability2.def);
                }
            }
            if (pawn.guest != null)
            {
                newPawn.guest.Recruitable = pawn.guest.Recruitable;
            }
            newPawn.Drawer.renderer.SetAllGraphicsDirty();
            newPawn.Notify_DisabledWorkTypesChanged();
            if (pawn.SpawnedOrAnyParentSpawned)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.PositionHeld, pawn.MapHeld, 6, null);
                GenPlace.TryPlaceThing(newPawn, loc, pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                if (!newPawn.IsColonistPlayerControlled)
                {
                    LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                }
            }
            this.doppelganger = newPawn;
            this.Severity = 0.001f;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.doppelganger, "doppelganger", false);
        }
        private Pawn doppelganger;
    }
    /*oxpeckers have two distinct effects.
     * Randomly (MTB in days = tameMTBdaysBase divided by evaluation of their psysens on tameMTBcurve) they will tame an animal on the same map. If they are on a caravan, there are no tameable animals on the map, or a 10% random chance occurs,
     * they instead generate a new animal appropriate to the biome and tame it.
     * Second effect is to apply a hediff to animals affected by their psycasts. Hostile animals get hediffIfMean (this is a nerf), other animals get hediffIfNice (this is a buff).*/
    public class HediffCompProperties_Oxpeck : HediffCompProperties_ExtraOnHitEffects
    {
        public HediffCompProperties_Oxpeck()
        {
            this.compClass = typeof(HediffComp_Oxpeck);
        }
        public HediffDef hediffNice;
        public HediffDef hediffMean;
        public float baseSeverity;
        public float tameMTBdaysBase;
        public SimpleCurve tameMTBcurve;
    }
    public class HediffComp_Oxpeck : HediffComp_ExtraOnHitEffects
    {
        public new HediffCompProperties_Oxpeck Props
        {
            get
            {
                return (HediffCompProperties_Oxpeck)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (this.Pawn.IsHashIntervalTick(150, delta) && this.Props.tameMTBdaysBase > 0 && Rand.MTBEventOccurs(this.Props.tameMTBdaysBase / this.Props.tameMTBcurve.Evaluate(this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity)), 60000f, 150f))
            {
                if (this.Pawn.Spawned)
                {
                    bool addAnimal = true;
                    if (Rand.Chance(0.9f))
                    {
                        List<Pawn> animals = new List<Pawn>();
                        foreach (Pawn p in this.Pawn.Map.mapPawns.AllPawnsSpawned)
                        {
                            if (TameUtility.CanTame(p) && !p.Downed && !p.Position.Fogged(p.Map) && p.GetStatValue(StatDefOf.Wildness) > 0f)
                            {
                                animals.Add(p);
                            }
                        }
                        if (animals.Count > 0)
                        {
                            addAnimal = false;
                            this.TameAnimal(animals.RandomElement());
                        }
                    }
                    if (addAnimal)
                    {
                        Map map = this.Pawn.Map;
                        IntVec3 loc;
                        if (RCellFinder.TryFindRandomPawnEntryCell(out loc, map, CellFinder.EdgeRoadChance_Animal, true, (IntVec3 cell) => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false, false, false).WithFenceblocked(true))))
                        {
                            PawnKindDef pawnKindDef;
                            if (map.Biome.AllWildAnimals.Where((PawnKindDef a) => map.mapTemperature.SeasonAcceptableFor(a.race, 0f)).TryRandomElementByWeight((PawnKindDef def) => this.CommonalityOfAnimalNow(map, def), out pawnKindDef))
                            {
                                IntVec3 iv3 = CellFinder.RandomClosewalkCellNear(loc, map, 4, null);
                                Pawn animal = (Pawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(pawnKindDef, null), iv3, map, WipeMode.Vanish);
                                this.TameAnimal(animal);
                            }
                        }
                    }
                }
                else
                {
                    Caravan c = this.Pawn.GetCaravan();
                    if (c != null)
                    {
                        BiomeDef b = Find.WorldGrid[c.Tile].PrimaryBiome;
                        if (b != null)
                        {
                            PawnKindDef pawnKindDef;
                            if (b.AllWildAnimals.TryRandomElementByWeight((PawnKindDef def) => this.CommonalityOfAnimalNow(c, b, def), out pawnKindDef))
                            {
                                Pawn animal = PawnGenerator.GeneratePawn(pawnKindDef, null);
                                c.AddPawn(animal, true);
                                Find.WorldPawns.PassToWorld(animal, PawnDiscardDecideMode.Decide);
                                this.TameAnimal(animal);
                            }
                        }
                    }
                }
            }
            PsychicPowerUtility.AddMoreFish(this.Pawn, delta, 0.01f);
        }
        public float CommonalityOfAnimalNow(Map map, PawnKindDef def)
        {
            return ((ModsConfig.BiotechActive && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[map.Tile].pollution)) ? map.Biome.CommonalityOfPollutionAnimal(def) : map.Biome.CommonalityOfAnimal(def)) / def.wildGroupSize.Average;
        }
        public float CommonalityOfAnimalNow(Caravan caravan, BiomeDef b, PawnKindDef def)
        {
            return ((ModsConfig.BiotechActive && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[caravan.Tile].pollution)) ? b.CommonalityOfPollutionAnimal(def) : b.CommonalityOfAnimal(def)) / def.wildGroupSize.Average;
        }
        public void TameAnimal(Pawn pawn)
        {
            string text = pawn.LabelIndefinite();
            bool flag = pawn.Name != null;
            pawn.SetFaction(Faction.OfPlayer, null);
            string text2;
            if (!flag && pawn.Name != null)
            {
                if (pawn.Name.Numerical)
                {
                    text2 = "LetterAnimalSelfTameAndNameNumerical".Translate(text, pawn.Name.ToStringFull, pawn.Named("ANIMAL")).CapitalizeFirst();
                }
                else
                {
                    text2 = "LetterAnimalSelfTameAndName".Translate(text, pawn.Name.ToStringFull, pawn.Named("ANIMAL")).CapitalizeFirst();
                }
            }
            else
            {
                text2 = "LetterAnimalSelfTame".Translate(pawn).CapitalizeFirst();
            }
            ChoiceLetter notification = LetterMaker.MakeLetter(
            "LetterLabelAnimalSelfTame".Translate(pawn.KindLabel, pawn).CapitalizeFirst(), text2, LetterDefOf.PositiveEvent, pawn, null, null, null);
            Find.LetterStack.ReceiveLetter(notification, null);
        }
        public override void DoExtraEffects(Pawn victim, float valueToScale, BodyPartRecord hitPart = null)
        {
            base.DoExtraEffects(victim, valueToScale, hitPart);
            Hediff hediff = HediffMaker.MakeHediff(this.Pawn.HostileTo(victim) ? this.Props.hediffMean : this.Props.hediffNice, victim);
            hediff.Severity = this.Props.baseSeverity;
            victim.health.AddHediff(hediff);
        }
    }
    //Storks use a PsyfocusSpentTracker derivative to make healthy, non-trans non-woke, same-faction same-ideo pawns of their own pawnkinddef after spending a bunch of psyfocus.
    public class HediffCompProperties_InASwaddle : HediffCompProperties_PsyfocusSpentTracker
    {
        public HediffCompProperties_InASwaddle()
        {
            this.compClass = typeof(HediffComp_InASwaddle);
        }
        public float severityToTrigger;
    }
    public class HediffComp_InASwaddle : HediffComp_PsyfocusSpentTracker
    {
        public new HediffCompProperties_InASwaddle Props
        {
            get
            {
                return (HediffCompProperties_InASwaddle)this.props;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                return base.CompTipStringExtra + (ModCompatibilityUtility.IsHighFantasy() ? "Hauts_PsyfocusSpentTrackerTooltipFantasy".Translate(this.parent.Severity, this.Props.severityToTrigger) : "Hauts_PsyfocusSpentTrackerTooltip".Translate(this.parent.Severity, this.Props.severityToTrigger));
            }
        }
        public override void UpdatePsyfocusExpenditure(float offset)
        {
            base.UpdatePsyfocusExpenditure(offset);
            if (this.Pawn.SpawnedOrAnyParentSpawned)
            {
                while (this.parent.Severity >= this.Props.severityToTrigger)
                {
                    Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.Pawn.kindDef, this.Pawn.Faction, PawnGenerationContext.NonPlayer, this.Pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                    if (newPawn != null)
                    {
                        List<Trait> traitsToRemove = new List<Trait>();
                        foreach (Trait t in newPawn.story.traits.allTraits)
                        {
                            if (PsychicTraitAndGeneCheckUtility.IsAwakenedTrait(t.def) || PsychicTraitAndGeneCheckUtility.IsTranscendentTrait(t.def))
                            {
                                traitsToRemove.Add(t);
                            }
                        }
                        foreach (Trait t in traitsToRemove)
                        {
                            newPawn.story.traits.RemoveTrait(t);
                        }
                        List<Gene> genesToRemove = new List<Gene>();
                        if (ModsConfig.BiotechActive && newPawn.genes != null)
                        {
                            foreach (Gene g in newPawn.genes.GenesListForReading)
                            {
                                if (PsychicTraitAndGeneCheckUtility.IsAwakenedPsychicGene(g.def))
                                {
                                    genesToRemove.Add(g);
                                }
                            }
                        }
                        foreach (Gene g in genesToRemove)
                        {
                            newPawn.genes.RemoveGene(g);
                        }
                        for (int j = 0; j < 5; j++)
                        {
                            HealthUtility.FixWorstHealthCondition(newPawn);
                        }
                        if (ModsConfig.IdeologyActive && newPawn.ideo != null && this.Pawn.ideo != null)
                        {
                            newPawn.ideo.SetIdeo(this.Pawn.ideo.Ideo);
                        }
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.Pawn.PositionHeld, this.Pawn.MapHeld, 6, null);
                        GenPlace.TryPlaceThing(newPawn, loc, this.Pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                        FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                        DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                        if (!newPawn.IsColonistPlayerControlled)
                        {
                            LordMaker.MakeNewLord(this.Pawn.Faction, new LordJob_EscortPawn(this.Pawn), this.Pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                        }
                    }
                    this.parent.Severity -= this.Props.severityToTrigger;
                }
            }
        }
    }
    /*War Queens generate one random type of non-dryad trainable animal. They start fully trained. The net body size of all (living) spawned animals can't exceed twice your psylink level.
     * animalType: the animal this War Queen generates.    spawnedAnimals: all the animals currently created by this War Queen.       sumBodySize: ewisott of the spawned animals
     * Has a God Mode option to reroll the animal type because someone requested it in the comments after getting a Gallatross. Sure, happy to provide utility... WTF tho? You won the lottery and don't want it!??!??? I'm still baffled by that one.*/
    public class Hediff_INeedMoreSteel : HediffWithComps
    {
        public override string Label
        {
            get
            {
                return base.Label + " (" + this.animalType.label + ")";
            }
        }
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.DetermineAnimalType();
        }
        public void DetermineAnimalType()
        {
            PawnKindDef oldType = this.animalType;
            this.animalType = (from pawnKind in DefDatabase<PawnKindDef>.AllDefsListForReading
                               where pawnKind.RaceProps.Animal && !pawnKind.RaceProps.Dryad && pawnKind.RaceProps.trainability.intelligenceOrder >= 20
                               select pawnKind).RandomElement();
            if (oldType != this.animalType && !this.spawnedAnimals.NullOrEmpty())
            {
                for (int i = this.spawnedAnimals.Count - 1; i >= 0; i--)
                {
                    this.spawnedAnimals[i].Destroy();
                    this.RemoveAnimal(this.spawnedAnimals[i]);
                }
            }
            this.Severity = this.def.initialSeverity;
            this.RecalculateMax();
        }
        public void RecalculateMax()
        {
            this.sumBodySize = 0f;
            foreach (Pawn p in this.spawnedAnimals)
            {
                this.sumBodySize += p.BodySize;
            }
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (!this.pawn.Spawned)
            {
                return;
            }
            if (this.pawn.IsHashIntervalTick(2500, delta))
            {
                this.RecalculateMax();
            }
            if (this.Severity == this.def.maxSeverity && this.pawn.SpawnedOrAnyParentSpawned && this.sumBodySize <= 2f * this.pawn.GetPsylinkLevel())
            {
                this.Severity = 0.001f;
                Pawn newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.animalType, this.pawn.Faction, PawnGenerationContext.NonPlayer, this.pawn.Map.Tile, true, false, false, false, false, 0f, true, true, true, false, true, false, false, false, false, 1f, 1f, null, 0f, null, null, null, null, new float?(0.2f), null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
                if (newPawn != null)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        HealthUtility.FixWorstHealthCondition(newPawn);
                    }
                }
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(this.pawn.PositionHeld, this.pawn.MapHeld, 6, null);
                GenPlace.TryPlaceThing(newPawn, loc, this.pawn.MapHeld, ThingPlaceMode.Near, null, null, default);
                FleckMaker.AttachedOverlay(newPawn, FleckDefOf.PsycastSkipInnerExit, Vector3.zero, 1f, -1f);
                DefDatabase<SoundDef>.GetNamed("Hive_Spawn").PlayOneShot(new TargetInfo(newPawn.PositionHeld, newPawn.MapHeld, false));
                if (newPawn.training != null)
                {
                    this.TrainAnimal(newPawn);
                }
                if (newPawn.Faction != Faction.OfPlayerSilentFail)
                {
                    LordMaker.MakeNewLord(this.pawn.Faction, new LordJob_EscortPawn(this.pawn), this.pawn.Map, Gen.YieldSingle<Pawn>(newPawn));
                }
                Hediff_WQBond hediff = (Hediff_WQBond)HediffMaker.MakeHediff(HVTRoyaltyDefOf.HVT_WQBond, newPawn);
                hediff.creator = this.pawn;
                hediff.wqHediff = this;
                newPawn.health.AddHediff(hediff);
                this.spawnedAnimals.Add(newPawn);
                this.RecalculateMax();
            }
            PsychicPowerUtility.AddMoreFish(this.pawn, delta, 0.01f);
        }
        private void TrainAnimal(Pawn newPawn)
        {
            foreach (TrainableDef td in DefDatabase<TrainableDef>.AllDefs)
            {
                if (newPawn.training.CanAssignToTrain(td))
                {
                    newPawn.training.SetWantedRecursive(td, true);
                    newPawn.training.Train(td, null, true);
                }
            }
        }
        public void RemoveAnimal(Pawn pawn)
        {
            this.spawnedAnimals.Remove(pawn);
            this.RecalculateMax();
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos && this.animalType != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "HVT_DetermineNewAnimal".Translate(),
                    action = delegate
                    {
                        this.DetermineAnimalType();
                    }
                };
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<PawnKindDef>(ref this.animalType, "animalType");
            Scribe_Values.Look<float>(ref this.sumBodySize, "sumBodySize", 0f, false);
            Scribe_Collections.Look<Pawn>(ref this.spawnedAnimals, "spawnedAnimals", LookMode.Reference, Array.Empty<object>());
        }
        private PawnKindDef animalType = PawnKindDefOf.Thrumbo;
        private float sumBodySize = 0f;
        private List<Pawn> spawnedAnimals = new List<Pawn>();
    }
    //whenever a WarQueen-generated animal dies, we want it removed from the 'memory' of the hediff that generated it, which will make more room for replacements
    public class Hediff_WQBond : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (this.wqHediff != null)
            {
                this.wqHediff.RemoveAnimal(this.pawn);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.creator, "creator", false);
            Scribe_References.Look<Hediff_INeedMoreSteel>(ref this.wqHediff, "wqHediff", false);
        }
        public Pawn creator;
        public Hediff_INeedMoreSteel wqHediff;
    }
}
