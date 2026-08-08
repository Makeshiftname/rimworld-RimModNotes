using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsTraits
{
    //aerospace spectrum, thoughts that trigger while in a space layer
    public class ThoughtWorker_NotInKansasAnymore : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.OdysseyActive || p.Tile == null || p.Tile.Layer == null)
            {
                return ThoughtState.Inactive;
            }
            return p.Tile.Layer.Def.isSpace ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }
    }
    //angler gets a thought if in a map with no fish-containing bodies of water
    public class ThoughtWorker_AnglerYearning : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.OdysseyActive || p.MapHeld == null)
            {
                return ThoughtState.Inactive;
            }
            return (p.MapHeld.waterBodyTracker == null || !p.MapHeld.waterBodyTracker.AnyBodyContainsFish) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }
    }
    //globetrotter stores all the unique landmarks and biomes it's been to in a hediff. The size of those lists scales its severity, which scales the mood they get from this thought
    public class ThoughtWorker_GlobetrotHediff : ThoughtWorker_Hediff
    {
        public override float MoodMultiplier(Pawn p)
        {
            Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(this.def.hediff, false);
            if (firstHediffOfDef != null)
            {
                return base.MoodMultiplier(p) * firstHediffOfDef.Severity;
            }
            return base.MoodMultiplier(p);
        }
    }
    [StaticConstructorOnStartup]
    public class Hediff_IveBeenEverywhereMan : HediffWithComps
    {
        public override void TickInterval(int delta)
        {
            if (this.pawn.IsHashIntervalTick(250, delta))
            {
                if (this.pawn.Spawned && !this.pawn.Downed && !this.pawn.Suspended && this.pawn.Tile != null && this.pawn.Tile.Valid && this.pawn.Tile.Tile != null)
                {
                    Tile tile = this.pawn.Tile.Tile;
                    if (tile.PrimaryBiome != null && !this.witnessedBiomes.Contains(tile.PrimaryBiome))
                    {
                        this.witnessedBiomes.Add(tile.PrimaryBiome);
                    }
                    if (!tile.Mutators.NullOrEmpty())
                    {
                        foreach (TileMutatorDef tmd in tile.Mutators)
                        {
                            if (!this.witnessedTileMutators.Contains(tmd))
                            {
                                this.witnessedTileMutators.Add(tmd);
                            }
                        }
                    }
                }
                this.Severity = this.witnessedBiomes.Count() + this.witnessedTileMutators.Count();
            }
            base.TickInterval(delta);
        }
        public override string GetTooltip(Pawn pawn, bool showHediffsDebugInfo)
        {
            string result = base.GetTooltip(pawn, showHediffsDebugInfo) + "\n";
            if (!this.witnessedBiomes.NullOrEmpty())
            {
                result += "\n" + "HVT_GlobetrotterBiomeLister".Translate() + ":\n";
                foreach (BiomeDef biome in this.witnessedBiomes)
                {
                    result += "-" + biome.LabelCap + "\n";
                }
            }
            if (!this.witnessedTileMutators.NullOrEmpty())
            {
                result += "\n" + "HVT_GlobetrotterMutatorLister".Translate() + ":\n";
                foreach (TileMutatorDef tmd in this.witnessedTileMutators)
                {
                    result += "-" + tmd.LabelCap + "\n";
                }
            }
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<BiomeDef>(ref this.witnessedBiomes, "witnessedBiomes", LookMode.Def, Array.Empty<object>());
            Scribe_Collections.Look<TileMutatorDef>(ref this.witnessedTileMutators, "witnessedTileMutators", LookMode.Def, Array.Empty<object>());
        }
        public List<BiomeDef> witnessedBiomes = new List<BiomeDef>();
        public List<TileMutatorDef> witnessedTileMutators = new List<TileMutatorDef>();
        private static readonly Texture2D DisarmTexture = ContentFinder<Texture2D>.Get("UI/Commands/Hack", true);
    }
}
