using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsTraitsRoyalty
{
    //Doves end psychic drones on any map they're in, and constantly cause psychic soothes on any map they're in
    public class Hediff_Dove : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(25, delta) && this.pawn.MapHeld != null)
            {
                GameCondition activeCondition = this.pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.PsychicDrone);
                if (activeCondition != null)
                {
                    activeCondition.Duration = 0;
                }
                if (this.pawn.Map.gameConditionManager.GetActiveCondition(GameConditionDefOf.PsychicSoothe) == null)
                {
                    IncidentParms parms = new IncidentParms
                    {
                        target = this.pawn.MapHeld
                    };
                    IncidentDef soothe = DefDatabase<IncidentDef>.GetNamed("PsychicSoothe");
                    soothe.Worker.TryExecute(parms);
                }
            }
        }
    }
    /*Indigo Buntings can generate good events by looking in telescopes. The chance per 250 ticks (or whatever periodicity is set to) is "likelihood".
     * It tracks whether a telescope is being used by seeing if there's any joy tolerance buildup for telescope use (which it will then remove right after, because it's a BoredomAdjustment derivative).*/
    public class HediffCompProperties_Astrology : HediffCompProperties_BoredomAdjustment
    {
        public HediffCompProperties_Astrology()
        {
            this.compClass = typeof(HediffComp_Astrology);
        }
        public float likelihood;
    }
    public class HediffComp_Astrology : HediffComp_BoredomAdjustment
    {
        public new HediffCompProperties_Astrology Props
        {
            get
            {
                return (HediffCompProperties_Astrology)this.props;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (this.Pawn.IsHashIntervalTick(this.Props.ticks, delta) && Rand.Chance(this.Props.likelihood))
            {
                if (this.Pawn.needs.joy != null)
                {
                    foreach (JoyKindDef jkd in this.Props.boredoms.Keys)
                    {
                        float tolerance = this.Pawn.needs.joy.tolerances[jkd];
                        if (tolerance > 0f)
                        {
                            GoodAndBadIncidentsUtility.MakeGoodEvent(this.Pawn,0,null);
                            return;
                        }
                    }
                }
            }
            base.CompPostTickInterval(ref severityAdjustment, delta);
        }
    }
    //This reduces the possible complications that would prevent Indigo Buntings from having a Joy need (traits, genes, or hediffs that have Joy in their disablesNeeds), and therefore prevent them from being ABLE to use a telescope
    public class Hediff_EnsureJoyNeed : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.EnsureJoy();
        }
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.pawn.IsHashIntervalTick(2500, delta))
            {
                this.EnsureJoy();
            }
        }
        public void EnsureJoy()
        {
            NeedDef nd = DefDatabase<NeedDef>.GetNamed("Joy");
            if (this.pawn.story != null)
            {
                for (int i = this.pawn.story.traits.TraitsSorted.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.story.traits.TraitsSorted[i].CurrentData.disablesNeeds != null && this.pawn.story.traits.TraitsSorted[i].CurrentData.disablesNeeds.Contains(nd))
                    {
                        this.pawn.story.traits.RemoveTrait(this.pawn.story.traits.TraitsSorted[i]);
                    }
                }
            }
            if (ModsConfig.BiotechActive && this.pawn.genes != null)
            {
                for (int i = this.pawn.genes.GenesListForReading.Count - 1; i >= 0; i--)
                {
                    if (this.pawn.genes.GenesListForReading[i].def.disablesNeeds != null && this.pawn.genes.GenesListForReading[i].def.disablesNeeds.Contains(nd))
                    {
                        this.pawn.genes.RemoveGene(this.pawn.genes.GenesListForReading[i]);
                    }
                }
            }
            for (int i = this.pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
            {
                if (this.pawn.health.hediffSet.hediffs[i].CurStage != null && this.pawn.health.hediffSet.hediffs[i].CurStage.disablesNeeds != null && this.pawn.health.hediffSet.hediffs[i].CurStage.disablesNeeds.Contains(nd))
                {
                    this.pawn.health.RemoveHediff(this.pawn.health.hediffSet.hediffs[i]);
                }
            }
        }
    }
    //Jackdaws create a good event (from the Framework good event list) upon attaining max severity
    public class Hediff_Jackdaw : HediffWithComps
    {
        public override void PostTickInterval(int delta)
        {
            base.PostTickInterval(delta);
            if (this.Severity == this.def.maxSeverity)
            {
                this.Severity = 0.001f;
                GoodAndBadIncidentsUtility.MakeGoodEvent(this.pawn,0,null);
            }
        }
    }
    /*I AM STORM, MISTRESS OF THE ELEMENTS well you're not as cool as Storm, exactly. Her power level's a little too high for one transcendence, and for RimWorld in general. You'd need uhhhhhh Shearwater, Electrophorus, Albatross,
     * and one of the rapid travel transes like Quelea, Swift, or Arctic Tern just to get close.
     * Anyways this is a Weather Controller, in hediff form, which is why its code strongly resembles the code for a weather controller. The gizmo is even identical to the one that shows up if you have God Mode on when you have one selected!
     * However, we want to be able to create MOST weathers in the game, so we permit all of them unless they have the NotStormable DME (e.g. "Underground" is not really a weather, obviously).
     * Some weathers are actually just small portions of a larger game condition (specifically talking about Anomaly's blood rain here), and if you just force the weather, it doesn't work right. If you want Shearwaters to be able to use such a
     *   weather type, it needs to be tagged with StormCreateCondition, specifying the conditionDef it's normally partnered with. This will make the Shearwater create that condition on creating that weather.
     * This bullies normal CompCauseGameCondition_ForceWeather things, forcing their forced weather to be the same as this forced weather. And it has an AoE on the world map, so you can "defeat" a weather controller site by just swinging by it
     * and telling it what to do.
     * No special clauses for what happens when you're in a pocket map or a different Odyssey layer, so have fun making it thunder underground or rain in space. It's weird and kind of logic breaking, but not actually game breaking.*/
    public class Hediff_Ororo : HediffWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            this.weather = WeatherDefOf.Clear;
        }
        public GameConditionDef ConditionDef
        {
            get
            {
                return GameConditionDefOf.WeatherController;
            }
        }
        public IEnumerable<GameCondition> CausedConditions
        {
            get
            {
                return this.causedConditions.Values;
            }
        }
        public PlanetTile MyTile
        {
            get
            {
                if (this.pawn.SpawnedOrAnyParentSpawned)
                {
                    return this.pawn.Tile;
                }
                else if (this.pawn.GetCaravan() != null)
                {
                    return this.pawn.GetCaravan().Tile;
                }
                return PlanetTile.Invalid;
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if ((this.pawn.IsPlayerControlled || DebugSettings.ShowDevGizmos) && !this.pawn.DeadOrDowned && !this.pawn.Suspended && !this.pawn.InMentalState && this.pawn.GetStatValue(StatDefOf.PsychicSensitivity) > float.Epsilon)
            {
                yield return new Command_Action
                {
                    defaultLabel = this.weather.LabelCap,
                    defaultDesc = "HVT_ChangeWeather".Translate(),
                    icon = ContentFinder<Texture2D>.Get("PsychicTraits/Abilities/HVT_PkWeatherControl", true),
                    action = delegate
                    {
                        List<WeatherDef> allDefsListForReading = new List<WeatherDef>();
                        foreach (WeatherDef wd in DefDatabase<WeatherDef>.AllDefsListForReading)
                        {
                            if (!wd.HasModExtension<NotStormable>())
                            {
                                allDefsListForReading.Add(wd);
                            }
                        }
                        int num = allDefsListForReading.FindIndex((WeatherDef w) => w == this.weather);
                        num++;
                        if (num >= allDefsListForReading.Count)
                        {
                            num = 0;
                        }
                        GameConditionDef oldConsequent = this.consequent;
                        this.weather = allDefsListForReading[num];
                        this.ReSetupAllConditions(oldConsequent);
                    },
                    hotKey = KeyBindingDefOf.Misc1
                };
            }
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
        }
        public bool InAoE(PlanetTile tile)
        {
            return this.MyTile.Valid && tile.Valid && !tile.Tile.PrimaryBiome.inVacuum && (tile == this.MyTile || (tile.Layer == this.MyTile.Layer && Find.WorldGrid.ApproxDistanceInTiles(tile, this.MyTile) < Math.Max(6f, (float)this.pawn.GetPsylinkLevel())));
        }
        public GameCondition GetAndNullifyConditionInstance(Map map)
        {
            GameCondition activeCondition = null;
            for (int i = map.GameConditionManager.ActiveConditions.Count - 1; i >= 0; i--)
            {
                GameCondition gc = map.GameConditionManager.ActiveConditions[i];
                if (gc.def == this.ConditionDef)
                {
                    if (gc.conditionCauser != null)
                    {
                        if (gc.conditionCauser == this.pawn)
                        {
                            activeCondition = gc;
                            this.SetupCondition(activeCondition, map);
                            if (!this.causedConditions.ContainsKey(map))
                            {
                                this.causedConditions.Add(map, activeCondition);
                            }
                        }
                        else
                        {
                            CompCauseGameCondition_ForceWeather ccgcfw = gc.conditionCauser.TryGetComp<CompCauseGameCondition_ForceWeather>();
                            if (ccgcfw != null)
                            {
                                if (ccgcfw.weather != this.weather)
                                {
                                    ccgcfw.weather = this.weather;
                                    this.SetupCondition(gc, map);
                                }
                            }
                            else if (!gc.conditionCauser.DestroyedOrNull())
                            {
                                gc.conditionCauser.Kill();
                            }
                        }
                    }
                }
                else if (gc.def.weatherDef != null && gc.def.weatherDef != this.weather)
                {
                    gc.End();
                }
            }
            return activeCondition;
        }
        public override void PostTick()
        {
            base.PostTick();
            if (!this.pawn.Downed)
            {
                foreach (Map map in Find.Maps)
                {
                    if (this.InAoE(map.Tile))
                    {
                        this.EnforceConditionOn(map);
                    }
                }
            }
            Hediff_Ororo.tmpDeadConditionMaps.Clear();
            foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
            {
                if (keyValuePair.Value.Expired || !keyValuePair.Key.GameConditionManager.ConditionIsActive(keyValuePair.Value.def))
                {
                    Hediff_Ororo.tmpDeadConditionMaps.Add(keyValuePair.Key);
                }
            }
            foreach (Map map2 in Hediff_Ororo.tmpDeadConditionMaps)
            {
                this.causedConditions.Remove(map2);
            }
        }
        private GameCondition EnforceConditionOn(Map map)
        {
            GameCondition gameCondition = this.GetAndNullifyConditionInstance(map);
            if (gameCondition == null)
            {
                gameCondition = this.CreateConditionOn(map);
            }
            else
            {
                gameCondition.TicksLeft = gameCondition.TransitionTicks;
            }
            StormCreateCondition scc = this.weather.GetModExtension<StormCreateCondition>();
            if (scc != null)
            {
                this.consequent = scc.conditionDef;
                if (map.gameConditionManager.GetActiveCondition(scc.conditionDef) == null)
                {
                    GameCondition gc = GameConditionMaker.MakeConditionPermanent(scc.conditionDef);
                    gc.conditionCauser = this.pawn;
                    map.gameConditionManager.RegisterCondition(gc);
                    this.causedConditionsConsequent.Add(map, gc);
                    gc.suppressEndMessage = true;
                }
            }
            return gameCondition;
        }
        protected virtual GameCondition CreateConditionOn(Map map)
        {
            if (this.causedConditions.ContainsKey(map))
            {
                return this.causedConditions[map];
            }
            GameCondition gameCondition = GameConditionMaker.MakeCondition(this.ConditionDef, -1);
            gameCondition.Duration = gameCondition.TransitionTicks;
            gameCondition.conditionCauser = this.pawn;
            map.gameConditionManager.RegisterCondition(gameCondition);
            this.causedConditions.Add(map, gameCondition);
            this.SetupCondition(gameCondition, map);
            return gameCondition;
        }
        protected virtual void SetupCondition(GameCondition condition, Map map)
        {
            condition.suppressEndMessage = true;
            ((GameCondition_ForceWeather)condition).weather = this.weather;
        }
        protected void ReSetupAllConditions(GameConditionDef oldConsequent)
        {
            foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
            {
                this.SetupCondition(keyValuePair.Value, keyValuePair.Key);
            }
            for (int i = this.causedConditionsConsequent.Count - 1; i >= 0; i--)
            {
                GameCondition gc = this.causedConditionsConsequent.TryGetValue(this.causedConditionsConsequent.Keys.ToList()[i]);
                if (gc != null && gc.def != this.consequent)
                {
                    gc.End();
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.causedConditions.RemoveAll((KeyValuePair<Map, GameCondition> x) => !Find.Maps.Contains(x.Key));
            }
            Scribe_Collections.Look<Map, GameCondition>(ref this.causedConditions, "causedConditions", LookMode.Reference, LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.causedConditions.RemoveAll((KeyValuePair<Map, GameCondition> x) => x.Value == null);
                foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditions)
                {
                    keyValuePair.Value.conditionCauser = this.pawn;
                }
            }
            Scribe_Collections.Look<Map, GameCondition>(ref this.causedConditionsConsequent, "causedConditionsConsequent", LookMode.Reference, LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                this.causedConditionsConsequent.RemoveAll((KeyValuePair<Map, GameCondition> x) => x.Value == null);
                foreach (KeyValuePair<Map, GameCondition> keyValuePair in this.causedConditionsConsequent)
                {
                    keyValuePair.Value.conditionCauser = this.pawn;
                }
            }
            Scribe_Defs.Look<WeatherDef>(ref this.weather, "weather");
            Scribe_Defs.Look<GameConditionDef>(ref this.consequent, "consequent");
        }
        public WeatherDef weather;
        public GameConditionDef consequent;
        private Dictionary<Map, GameCondition> causedConditions = new Dictionary<Map, GameCondition>();
        private Dictionary<Map, GameCondition> causedConditionsConsequent = new Dictionary<Map, GameCondition>();
        private static List<Map> tmpDeadConditionMaps = new List<Map>();
    }
    public class NotStormable : DefModExtension
    {
        public NotStormable()
        {

        }
    }
    public class StormCreateCondition : DefModExtension
    {
        public StormCreateCondition()
        {

        }
        public GameConditionDef conditionDef;
    }
}
