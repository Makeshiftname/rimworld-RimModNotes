using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace HautsTraits
{
    //agrizoophobe basically works like Biotech pyrophobia fleeing, just using wild or aggro-mental state animals instead of fire
    public class MentalState_PanicFleeAnimals : MentalState
    {
        protected override bool CanEndBeforeMaxDurationNow
        {
            get
            {
                return false;
            }
        }
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);
            if (this.pawn.IsHashIntervalTick(30, delta))
            {
                if (this.lastWASeenTick < 0 || HVTUtility.NearWildAnimal(this.pawn))
                {
                    this.lastWASeenTick = Find.TickManager.TicksGame;
                }
                if (this.lastWASeenTick >= 0 && Find.TickManager.TicksGame >= this.lastWASeenTick + this.def.minTicksBeforeRecovery)
                {
                    base.RecoverFromState();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.lastWASeenTick, "lastWASeenTick", -1, false);
        }
        private int lastWASeenTick = -1;
    }
    public class MentalStateWorker_PanicFleeAnimals : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (pawn.Spawned && pawn.story != null && pawn.story.traits.HasTrait(HVTDefOf.HVT_Agrizoophobe))
            {
                if (pawn.Faction != Faction.OfPlayerSilentFail && Rand.Chance(0.9f))
                {
                    return false;
                }
            }
            return base.StateCanOccur(pawn) && HVTUtility.NearWildAnimal(pawn);
        }
    }
    //rambunctious' uncontrollable run is LIKE a normal Wander mental state, but it has a lower threshold at which they stop to rest, they use a faster locomotion urgency, and they spend less time picking a new point to go after reaching the target point
    public class JobGiver_WanderFast : JobGiver_Wander
    {
        public JobGiver_WanderFast()
        {
            this.wanderRadius = 7f;
            this.locomotionUrgency = LocomotionUrgency.Sprint;
            this.ticksBetweenWandersRange = new IntRange(125, 160);
        }
        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return pawn.Position;
        }
    }
    public class MentalStateWorker_WanderFast : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            if (pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.1f)
            {
                return false;
            }
            return true;
        }
    }
    //visionary's schematic generation can be handled by an MTB, so I decided to handle it via traits' native random mental break MTB
    public class MentalStateWorker_Eureka : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_Eureka : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            CompProperties_Book cpb = ThingDefOf.Schematic.GetCompProperties<CompProperties_Book>();
            if (cpb != null)
            {
                foreach (ReadingOutcomeProperties rop in cpb.doers)
                {
                    if (rop is BookOutcomeProperties_GainResearch bopgr)
                    {
                        List<ResearchTabDef> tabs = new List<ResearchTabDef>();
                        if (bopgr.tab != null)
                        {
                            tabs.Add(bopgr.tab);
                        }
                        if (bopgr.tabs != null)
                        {
                            foreach (BookOutcomeProperties_GainResearch.BookTabItem bti in bopgr.tabs)
                            {
                                if (!tabs.Contains(bti.tab))
                                {
                                    tabs.Add(bti.tab);
                                }
                            }
                        }
                        bool doNovel = true;
                        foreach (ResearchProjectDef rpd in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
                        {
                            if (!rpd.IsFinished && tabs.Contains(rpd.tab) && rpd.techprintCount == 0 && (bopgr.exclude.Count == 0 || !bopgr.exclude.ContainsAny((BookOutcomeProperties_GainResearch.BookResearchItem i) => i.project == rpd)))
                            {
                                doNovel = false;
                                break;
                            }
                        }
                        if (doNovel)
                        {
                            this.GenerateBook(ThingDefOf.Novel, "HVT_VisionaryNovelTitle", "HVT_VisionaryNovelDesc");
                            return;
                        }
                        break;
                    }
                }
            }
            this.GenerateBook(ThingDefOf.Schematic, "HVT_VisionarySchematicTitle", "HVT_VisionarySchematicDesc");
        }
        private void GenerateBook(ThingDef bookDef, string titleKey, string descKey)
        {
            Book book = (Book)ThingMaker.MakeThing(bookDef);
            CompQuality compQuality = book.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory q = QualityUtility.GenerateQualityCreatedByPawn(this.pawn, SkillDefOf.Intellectual);
                compQuality.SetQuality(q, null);
            }
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, true);
            typeof(Book).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, titleKey.Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve());
            typeof(Book).GetField("description", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, descKey.Translate().CapitalizeFirst().Formatted(this.pawn.Named("PAWN")).AdjustedFor(this.pawn, "PAWN", true).Resolve());
            typeof(Book).GetField("descCanBeInvalidated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, false);
            typeof(Book).GetField("descriptionFlavor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, null);
            this.pawn.inventory.innerContainer.TryAdd(book, true);
            if (this.pawn.skills != null)
            {
                this.pawn.skills.Learn(SkillDefOf.Intellectual, this.pawn.skills.GetSkill(SkillDefOf.Intellectual).XpRequiredForLevelUp / 10f, true, true);
            }
        }
    }
    //ditto for radical thinkers, although this one doesn't have multiple points on the mood curve. This runs the entire ideo generation process (unless there have been recent changes) to create a new one, then grants a super-ideo stat buff to the pawn
    public class MentalStateWorker_RadThinker : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_RadThinker : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            if (ModsConfig.IdeologyActive && !this.pawn.Ideo.classicMode)
            {
                this.oldIdeo = this.pawn.Ideo;
                foreach (MemeDef m in DefDatabase<MemeDef>.AllDefsListForReading)
                {
                    if (m.disagreeableTraits != null && m.disagreeableTraits.Count > 0)
                    {
                        bool addToList = true;
                        foreach (TraitRequirement t in m.disagreeableTraits)
                        {
                            if (this.pawn.story.traits.HasTrait(t.def))
                            {
                                addToList = false;
                                this.disagreedMemes.Add(m);
                                break;
                            }
                        }
                        if (!addToList)
                        {
                            continue;
                        }
                    }
                    if (m.agreeableTraits != null && m.agreeableTraits.Count > 0)
                    {
                        foreach (TraitRequirement t in m.agreeableTraits)
                        {
                            if (this.pawn.story.traits.HasTrait(t.def))
                            {
                                this.agreedMemes.Add(m);
                            }
                        }
                    }
                }
                IdeoGenerationParms parms;
                List<MemeDef> forcedMeme = new List<MemeDef>();
                if (this.agreedMemes.Count > 0 && Rand.Value <= 0.66f)
                {
                    forcedMeme.Add(this.agreedMemes.RandomElement<MemeDef>());
                    parms = new IdeoGenerationParms(Faction.OfPlayer.def, false, null, this.disagreedMemes, forcedMeme);
                }
                else
                {
                    parms = new IdeoGenerationParms(Faction.OfPlayer.def, false, null, this.disagreedMemes);
                }
                this.newIdeo = IdeoGenerator.MakeIdeo(DefDatabase<IdeoFoundationDef>.AllDefs.RandomElement<IdeoFoundationDef>());
                this.newIdeo.culture = this.oldIdeo.culture;
                this.newIdeo.foundation.RandomizePlace();
                this.newIdeo.memes.Clear();
                this.newIdeo.memes.AddRange(IdeoUtility.GenerateRandomMemes(parms));
                this.newIdeo.SortMemesInDisplayOrder();
                this.newIdeo.classicExtraMode = parms.classicExtra;
                IdeoFoundation_Deity ideoFoundation_Deity;
                if ((ideoFoundation_Deity = (this.newIdeo.foundation as IdeoFoundation_Deity)) != null)
                {
                    ideoFoundation_Deity.GenerateDeities();
                }
                this.newIdeo.foundation.GenerateTextSymbols();
                this.newIdeo.foundation.GenerateLeaderTitle();
                this.newIdeo.foundation.RandomizeIcon();
                this.newIdeo.foundation.RandomizePrecepts(true, parms);
                this.newIdeo.RegenerateDescription(true);
                this.newIdeo.foundation.RandomizeStyles();
                this.pawn.ideo.SetIdeo(this.newIdeo);
                Find.IdeoManager.Add(this.newIdeo);
                Hediff hediff = HediffMaker.MakeHediff(HVTDefOf.HVT_RadThinkerBuff, this.pawn);
                this.pawn.health.AddHediff(hediff, this.pawn.health.hediffSet.GetBrain());
            }
        }
        private Ideo oldIdeo;
        private Ideo newIdeo;
        private readonly List<MemeDef> agreedMemes = new List<MemeDef>();
        private readonly List<MemeDef> disagreedMemes = new List<MemeDef>();
    }
    /*haunted's random sightstealer attacks are ALSO handled by the mental break MTB.
     * The nice thing about using this mechanic for these three traits is that, since the mental state interrupts whatever they're doing before they snap out of it, it's as if they get struck by a sudden feeling.
     * In this case, the feeling is otherworldly alarm and terror.*/
    public class MentalStateWorker_SightstealerAttack : MentalStateWorker
    {
        public override bool StateCanOccur(Pawn pawn)
        {
            if (!ModsConfig.AnomalyActive || !base.StateCanOccur(pawn) || (pawn.Faction != null && (pawn.Faction == Faction.OfEntities || pawn.Faction == Faction.OfHoraxCult)) || !pawn.SpawnedOrAnyParentSpawned || pawn.GetStatValue(StatDefOf.PsychicSensitivity) < float.Epsilon)
            {
                return false;
            }
            return true;
        }
    }
    public class MentalState_SightstealerAttack : MentalState
    {
        public override void PreStart()
        {
            base.PreStart();
            if (this.pawn.MapHeld != null)
            {
                Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_SightstealerAssault(), this.pawn.MapHeld, null);
                float num = StorytellerUtility.DefaultThreatPointsNow(this.pawn.MapHeld) * new FloatRange(0.2f, 0.55f).RandomInRange;
                num = Mathf.Max(Faction.OfEntities.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Sightstealers, null), num);
                List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                {
                    faction = Faction.OfEntities,
                    groupKind = PawnGroupKindDefOf.Sightstealers,
                    points = num,
                    tile = this.pawn.MapHeld.Tile
                }, true).ToList<Pawn>();
                foreach (Pair<List<Pawn>, IntVec3> pair in PawnsArrivalModeWorkerUtility.SplitIntoRandomGroupsNearMapEdge(list, this.pawn.MapHeld, false))
                {
                    foreach (Thing newThing in pair.First)
                    {
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(pair.Second, this.pawn.MapHeld, 8, null);
                        GenSpawn.Spawn(newThing, loc, this.pawn.MapHeld, WipeMode.Vanish);
                    }
                }
                foreach (Pawn p in list)
                {
                    lord.AddPawn(p);
                }
                SoundDefOf.Sightstealer_SummonedHowl.PlayOneShot(this.pawn);
            }
        }
    }
}
