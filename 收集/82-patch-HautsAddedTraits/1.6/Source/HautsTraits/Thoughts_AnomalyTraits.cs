using HautsFramework;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace HautsTraits
{
    /*catastrophist gains mood from a lot of stuff:
     * -depending on whether Anomaly content is active, basically, which in turn is evaluated differently based on the current run's selected Anomaly playstyle (for modded APDs, see the Framework's CustomAnomalyPlaystyleLevels.cs)
     * -if a planetkiller will strike in 10 years (only counts planetkillers in the world GC manager, because that's the only way they spawn naturally)
     * -per GC affecting the current map that has the CatastrophistLikedCondition DME. Since not every GC is a catastrophe of some kind, a specified whitelist is the way to go*/
    public class ThoughtWorker_AnomalyActivityLevel : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.AnomalyActive)
            {
                if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
                {
                    return ThoughtState.ActiveDefault;
                }
                int level = Find.Anomaly.Level;
                CustomAnomalyPlaystyleActivityLevels capal = Find.Storyteller.difficulty.AnomalyPlaystyleDef.GetModExtension<CustomAnomalyPlaystyleActivityLevels>();
                if (capal != null)
                {
                    level = capal.Worker.CurrentLevel(capal);
                }
                if (level > 0 && level != 6)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
        public override float MoodMultiplier(Pawn p)
        {
            if (Find.Storyteller.difficulty.AnomalyPlaystyleDef == DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail("AmbientHorror"))
            {
                return base.MoodMultiplier(p);
            }
            int level = Find.Anomaly.Level;
            CustomAnomalyPlaystyleActivityLevels capal = Find.Storyteller.difficulty.AnomalyPlaystyleDef.GetModExtension<CustomAnomalyPlaystyleActivityLevels>();
            if (capal != null)
            {
                level = capal.Worker.CurrentLevel(capal);
            }
            return base.MoodMultiplier(p) * Math.Min(level, 4f);
        }
    }
    public class ThoughtWorker_Planetkiller : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            foreach (GameCondition gc in Find.World.GameConditionManager.ActiveConditions)
            {
                if (gc is GameCondition_Planetkiller gcp && gcp.TicksLeft <= 36000000)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
    }
    public class Thought_Planetkiller : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                foreach (GameCondition gc in Find.World.GameConditionManager.ActiveConditions)
                {
                    if (gc is GameCondition_Planetkiller gcp && gcp.TicksLeft <= 36000000)
                    {
                        num = Math.Max(10f - Mathf.RoundToInt(gcp.TicksLeft / 3600000), 0f);
                        break;
                    }
                }
                return num;
            }
        }
    }
    public class Thought_CatastrophicClimate : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                if (this.pawn.Spawned && this.pawn.Map != null)
                {
                    for (int i = 0; i < this.pawn.Map.GameConditionManager.ActiveConditions.Count; i++)
                    {
                        GameCondition gc = this.pawn.Map.GameConditionManager.ActiveConditions[i];
                        if (gc.def.HasModExtension<CatastrophistLikedCondition>())
                        {
                            num += 3;
                        }
                    }
                }
                return num;
            }
        }
    }
    public class CatastrophistLikedCondition : DefModExtension
    {
        public CatastrophistLikedCondition()
        {
        }
    }
    //monster hunter/lover: these traits experience a mood change depending on whether the current Anomaly playstyle allows for anomalous events to occur.
    public class ThoughtWorker_AnomalyInactive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.AnomalyActive && p.story != null)
            {
                bool anomalyIsntActive = ((Find.Storyteller.difficulty.AnomalyPlaystyleDef.generateMonolith && (Find.Anomaly.Level == 0 || Find.Anomaly.Level == 6)) || !Find.Storyteller.difficulty.AnomalyPlaystyleDef.enableAnomalyContent);
                if (!anomalyIsntActive)
                {
                    CustomAnomalyPlaystyleActivityLevels capal = Find.Storyteller.difficulty.AnomalyPlaystyleDef.GetModExtension<CustomAnomalyPlaystyleActivityLevels>();
                    if (capal != null)
                    {
                        int level = capal.Worker.CurrentLevel(capal);
                        anomalyIsntActive = level < 1 || level > 5;
                    }
                }
                if (anomalyIsntActive)
                {
                    if (p.story.traits.HasTrait(HVTDefOf.HVT_MonsterHunter))
                    {
                        return ThoughtState.ActiveAtStage(0);
                    } else if (p.story.traits.HasTrait(HVTDefOf.HVT_MonsterLover)) {
                        return ThoughtState.ActiveAtStage(1);
                    }
                }
            }
            return ThoughtState.Inactive;
        }
    }
    //monster lovers can be made to like different xenotypes, or different racedefs, with two separate mod settings. This handles them both
    public class ThoughtWorker_PseudoMonsterLove : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.kindDef.race == pawn.kindDef.race)
            {
                if (ModsConfig.BiotechActive && HVT_Mod.settings.monsterLoveForXenos)
                {
                    if (pawn.genes == null)
                    {
                        if (other.genes != null)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    }
                    else if (other.genes != null)
                    {
                        if (other.genes.hybrid)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                        if (pawn.genes.CustomXenotype != null)
                        {
                            if (other.genes.CustomXenotype == null || other.genes.CustomXenotype != pawn.genes.CustomXenotype)
                            {
                                return ThoughtState.ActiveDefault;
                            }
                        }
                        else if (pawn.genes.Xenotype != other.genes.Xenotype || pawn.genes.xenotypeName != other.genes.xenotypeName)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    }
                    else
                    {
                        return ThoughtState.Inactive;
                    }
                }
            }
            else if (HVT_Mod.settings.monsterLoveForAliens)
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    //twisted has an opinion boost towards inhumanized pawns, and has a mood malus for not being inhumanized so that they are easier to humanity break
    public class ThoughtWorker_ThourtInhumanized : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!otherPawn.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            return otherPawn.Inhumanized() ? ThoughtState.ActiveAtStage(1) : ThoughtState.ActiveAtStage(0);
        }
    }
    public class ThoughtWorker_TwistedYearning : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.AnomalyActive)
            {
                return ThoughtState.Inactive;
            }
            return p.Inhumanized() ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(0);
        }
    }
}
