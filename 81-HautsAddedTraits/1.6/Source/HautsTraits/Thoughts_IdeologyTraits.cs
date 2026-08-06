using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsTraits
{
    //faith spectrum - all the other traits in it (the actual spectrum-as-defined-in-XML) dislike Doubtfuls
    public class ThoughtWorker_FaithVDoubt : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Doubtful))
            {
                return false;
            }
            return true;
        }
    }
    /*freedom sprectrum
     * Liberators gain mood if you have no slaves or prisoners. They don't lose mood if you have prisoners but no slaves. They lose mood for every slave you have.
     * Subjugators lose mood unless they are free and either 1) have a royal title, 2) have an ideoligious role, or 3) have slaves. In the last case, they get a mood bonus, scaling with slave count.
     */
    public class ThoughtWorker_CravingLiberation : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Faction faction = p.HostFaction ?? p.Faction;
            if (faction == null)
            {
                return ThoughtState.Inactive;
            }
            if (FactionUtility.GetSlavesInFactionCount(faction) > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(p);
            foreach (Pawn other in list)
            {
                if (other.GuestStatus == GuestStatus.Prisoner)
                {
                    return ThoughtState.Inactive;
                }
            }
            if (p.IsPrisoner || p.IsSlave)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(1);
        }
    }
    public class Thought_CravingLiberation : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                Faction faction = this.pawn.HostFaction ?? this.pawn.Faction;
                return faction != null ? this.CurStage.baseMoodEffect * Math.Max((float)FactionUtility.GetSlavesInFactionCount(faction), 1f) : this.CurStage.baseMoodEffect;
            }
        }
    }
    public class Thought_CravingSubjugation : Thought_Situational
    {
        public override float MoodOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (!this.pawn.IsSlave)
            {
                if (FactionUtility.GetSlavesInFactionCount(this.pawn.Faction) > 0)
                {
                    return 2f * (float)FactionUtility.GetSlavesInFactionCount(this.pawn.Faction);
                }
                else if ((this.pawn.Ideo != null && this.pawn.Ideo.GetRole(this.pawn) != null) || this.pawn.royalty.AllTitlesForReading.Count > 0)
                {
                    return 0f;
                }
            }
            return -10f;
        }
    }
    //intolerant dislikes pawns of different ideos
    public class ThoughtWorker_OfOtherIdeo : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!ModsConfig.IdeologyActive || other.Ideo == null || !RelationsUtility.PawnsKnowEachOther(pawn, other) || pawn.Ideo == null)
            {
                return false;
            }
            if (other.Ideo != pawn.Ideo)
            {
                return true;
            }
            return false;
        }
    }
}
