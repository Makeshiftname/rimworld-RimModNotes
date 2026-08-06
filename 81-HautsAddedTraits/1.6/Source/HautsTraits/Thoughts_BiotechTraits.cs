using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsTraits
{
    //childcare spectrum: opinion of children
    public class ThoughtWorker_Child : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || other.DevelopmentalStage == DevelopmentalStage.Adult || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.DevelopmentalStage == DevelopmentalStage.Baby || other.DevelopmentalStage == DevelopmentalStage.Child)
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    //misopedists gain a mood malus per child of same faction spawned in map. As it does not care about non-spawned humanlikes, children in growth vats shouldn't count
    public class Thought_Situational_HateChildrenInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.DevelopmentalStage != DevelopmentalStage.Adult)
                        {
                            num--;
                        }
                    }
                }
            }
            else if (pawn.GetCaravan() != null)
            {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.RaceProps.Humanlike && p.DevelopmentalStage != DevelopmentalStage.Adult)
                    {
                        num--;
                    }
                }
            }
            if (num <= -15)
            {
                num = -15;
            }
            return num;
        }
    }
    //caretakers work the opposite, except they also get a mood malus if they don't have a faction or there are no triggering children
    public class Thought_Situational_NeedChildrenInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (this.pawn.Faction == null)
            {
                return -10f;
            }
            int num = 0;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.DevelopmentalStage != DevelopmentalStage.Adult)
                        {
                            num++;
                        }
                    }
                }
            }
            else if (pawn.GetCaravan() != null)
            {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.RaceProps.Humanlike && p.DevelopmentalStage != DevelopmentalStage.Adult)
                    {
                        num++;
                    }
                }
            }
            if (num == 0)
            {
                num = -10;
            }
            else if (num > 15)
            {
                num = 15;
            }
            return num;
        }
    }
    //childcare spectrum: opinion and mood modifiers towards/for being pregonate
    public class ThoughtWorker_Pregnant : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.health.hediffSet.HasPregnancyHediff())
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_IsPregnant : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.BiotechActive && p.story != null && p.health.hediffSet.HasPregnancyHediff())
            {
                if (p.story.traits.HasTrait(HVTDefOf.HVT_Misopedist))
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if (p.story.traits.HasTrait(HVTDefOf.HVT_Caretaker))
                {
                    return ThoughtState.ActiveAtStage(1);
                }
            }
            return ThoughtState.Inactive;
        }
    }
    //mech spectrum and environmentalist: these traits have opinion and mood modifiers towards/for being mechanitors
    public class ThoughtWorker_VsMechanitor : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || other.mechanitor == null || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            return ThoughtState.ActiveDefault;
        }
    }
    public class ThoughtWorker_IsMechanitor : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.BiotechActive || p.mechanitor != null)
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    /*mech spectrum: these traits have a mood modifier based on the sum bandwidth of controlled mechs on the map.
     * However, the way this technically works is that it looks for spawned mechanitors on the map/caravan (who are, 90+% of the time, the only valid source of bandwidth for controlled mechs)
     * and just sums their bandwidth used values. There are likely loopholes you can abuse to avoid the mood effects.
     * Hard-capped at specific values due to a complaint that it was too easy to pump up Mechaphile mood. I disagree, in the sense that using a lot of mechs is already putting the game on ez mode,
     * but there isn't precedent for infinitely-scaling moodlets in unmodded RimWorld and this is the easiest one in HAT to stack by far, so the motion to introduce a limit passed.*/
    public class Thought_Situational_MechsInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            float num = 0f;
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.mechanitor != null)
                        {
                            num += enumerator.Current.mechanitor.UsedBandwidthFromSubjects;
                        }
                    }
                }
            }
            else if (pawn.GetCaravan() != null)
            {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p.mechanitor != null)
                    {
                        num += p.mechanitor.UsedBandwidthFromSubjects;
                    }
                }
            }
            num *= this.BaseMoodOffset;
            num = Math.Max(Math.Min(num, 20f), -40f);
            return num;
        }
    }
    //environmentalist
    public class Thought_Situational_PollutionOnTile : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            PollutionLevel pollution;
            if (this.pawn.Map != null && this.pawn.Map.Tile != null && this.pawn.Map.Tile.Valid)
            {
                pollution = Find.WorldGrid[this.pawn.Map.Tile].PollutionLevel();
            }
            else if (pawn.GetCaravan() != null)
            {
                pollution = Find.WorldGrid[pawn.GetCaravan().GetTileCurrentlyOver()].PollutionLevel();
            }
            else
            {
                return 0f;
            }
            if (pollution == PollutionLevel.Light)
            {
                num -= 15;
            }
            else if (pollution == PollutionLevel.Moderate)
            {
                num -= 26;
            }
            else if (pollution == PollutionLevel.Extreme)
            {
                num -= 37;
            }
            return this.BaseMoodOffset + num;
        }
    }
    public class ThoughtWorker_NoxiousHazeUndefeatable : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (NoxiousHazeUtility.IsExposedToNoxiousHaze(p))
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    //xenotypist
    public class ThoughtWorker_GenePurism : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.BiotechActive || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.kindDef.race == pawn.kindDef.race)
            {
                if (pawn.genes == null)
                {
                    if (other.genes != null)
                    {
                        return ThoughtState.ActiveDefault;
                    }
                    return ThoughtState.Inactive;
                }
                else if (other.genes != null)
                {
                    if (other.genes.hybrid)
                    {
                        return ThoughtState.ActiveDefault;
                    }
                    if (pawn.genes.CustomXenotype != null)
                    {
                        if (other.genes.CustomXenotype == null)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                        else if (other.genes.CustomXenotype != pawn.genes.CustomXenotype)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    }
                    else
                    {
                        if (pawn.genes.Xenotype != other.genes.Xenotype)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                        if (pawn.genes.xenotypeName != other.genes.xenotypeName)
                        {
                            return ThoughtState.ActiveDefault;
                        }
                    }
                }
                else
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            else if (HVT_Mod.settings.genePuristsHateAliens)
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
}
