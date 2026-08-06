using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsTraits
{
    /*anarchists have a moodlet that is extremely negative while they belong to a, let's call them for future reference, "power-centralized" faction.
     * These are factions that either have royal titles or are tagged with the AnarchistHatedFaction DME.
     * If they don't belong to such a faction, they 
     * Apply the DME to any faction that lacks royal titles, but is nevertheless an obviously hierarchical institution with rigid power structures.
     * you can also just use a Harmony patch to add additional qualifiers to Thought_Situational_RelationsWithEmpire.ShouldHateFaction - see the compat folder for Custom Republic as an example*/
    public class Thought_Situational_RelationsWithEmpire : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (this.pawn.Faction.def.HasRoyalTitles || this.pawn.Faction.def.HasModExtension<AnarchistHatedFaction>())
            {
                return -20f;
            }
            float num = 0f;
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (Thought_Situational_RelationsWithEmpire.ShouldHateFaction(f))
                {
                    num -= this.pawn.Faction.GoodwillWith(f) / 10;
                }
            }
            return Math.Min(Math.Max(this.BaseMoodOffset * num,-20),20);
        }
        public static bool ShouldHateFaction(Faction f)
        {
            return f.def.HasRoyalTitles || f.def.HasModExtension<AnarchistHatedFaction>();
        }
    }
    public class AnarchistHatedFaction : DefModExtension
    {
        public AnarchistHatedFaction()
        {
        }
    }
    public class ThoughtWorker_Imperial : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (other.Faction == null || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (Thought_Situational_RelationsWithEmpire.ShouldHateFaction(other.Faction))
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_Leader : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (other.royalty.MainTitle() != null || ModsConfig.IdeologyActive && other.Ideo != null && other.Ideo.GetRole(other) != null)
            {
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
    public class ThoughtWorker_HVT_AnarchistVsServile : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, other) || !other.story.traits.HasTrait(HVTDefOf.HVT_Servile))
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_HVT_ServileVsAnarchist : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(p, other) || !other.story.traits.HasTrait(HVTDefOf.HVT_Anarchist))
            {
                return false;
            }
            return true;
        }
    }
    public class Thought_Situational_LeadersInColony : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (this.pawn.Faction == null || ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            int num = 0;
            if (this.pawn.royalty.MainTitle() != null || (ModsConfig.IdeologyActive && this.pawn.ideo != null && this.pawn.ideo.Ideo.GetRole(this.pawn) != null))
            {
                return -12f;
            }
            if (this.pawn.Map != null)
            {
                using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != this.pawn && enumerator.Current.royalty.MainTitle() != null)
                        {
                            num += enumerator.Current.royalty.MostSeniorTitle.def.seniority / 100;
                        }
                    }
                }
            } else if (pawn.GetCaravan() != null) {
                foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                {
                    if (p != this.pawn && p.Faction != null && this.pawn.Faction == p.Faction)
                    {
                        num += p.royalty.MostSeniorTitle.def.seniority / 100;
                    }
                }
            }
            if (ModsConfig.IdeologyActive && this.pawn.ideo != null)
            {
                if (this.pawn.Map != null)
                {
                    using (List<Pawn>.Enumerator enumerator = this.pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(this.pawn.Faction).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current != this.pawn && enumerator.Current.ideo != null && enumerator.Current.ideo.Ideo == this.pawn.ideo.Ideo && enumerator.Current.ideo.Ideo.GetRole(enumerator.Current) != null)
                            {
                                num += 1;
                            }
                        }
                    }
                } else if (pawn.GetCaravan() != null) {
                    foreach (Pawn p in pawn.GetCaravan().pawns.InnerListForReading)
                    {
                        if (p != this.pawn && p.Faction != null && this.pawn.Faction == p.Faction && p.ideo != null && p.ideo.Ideo == this.pawn.ideo.Ideo && p.ideo.Ideo.GetRole(p) != null)
                        {
                            num += 1;
                        }
                    }
                }
            }
            if (num <= 0)
            {
                return -8f;
            }
            if (num > 10)
            {
                num = 10;
            }
            return this.BaseMoodOffset * num;
        }
    }
}
