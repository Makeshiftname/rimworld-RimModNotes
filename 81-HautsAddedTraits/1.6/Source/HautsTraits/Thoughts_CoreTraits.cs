using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HautsTraits
{
    //Agrizoophobe dislikes being near wild or aggro animals
    public class ThoughtWorker_Agrizoophobia : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.story == null || !p.story.traits.HasTrait(HVTDefOf.HVT_Agrizoophobe) || !HVTUtility.NearWildAnimal(p))
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(0);
        }
    }
    //gravers get mood from seeing skullspikes
    public class ThoughtWorker_Skullspike : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.surroundings == null)
            {
                return false;
            }
            int val = p.surroundings.NumSkullspikeSightings();
            if (val > 8)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            if (val > 3)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (val > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return false;
        }
    }
    //hysteric aura can't affect pawns who have limited hearing and sight
    public class Thought_ContagiousHysteria : Thought_Memory
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = this.CurStage.baseMoodEffect;
                if (!this.pawn.Spawned || (this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) < 0.2f && this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) < 0.2f))
                {
                    num = 0;
                }
                return num;
            }
        }
    }
    //lech gains an opinion bonus of anyone over 16 of a gender they're attracted to, and such pawns gain an opinion malus of the lech
    public class ThoughtWorker_CreptOn : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Lech) || !RelationsUtility.AttractedToGender(other, pawn.gender) || pawn.ageTracker.AgeBiologicalYearsFloat < 16f)
            {
                return false;
            }
            return true;
        }
    }
    public class ThoughtWorker_ManYouCreepin : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!RelationsUtility.AttractedToGender(pawn, other.gender) || other.ageTracker.AgeBiologicalYearsFloat < 16f)
            {
                return false;
            }
            return true;
        }
    }
    //lovesick loses mood if no romaence partner, loses less mood if they do have one but aren't on the same map, or otherwise gain mood. They also gain a huge opinion boost of their lover
    public class ThoughtWorker_SickForLove : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.DevelopmentalStage.Juvenile())
            {
                return ThoughtState.Inactive;
            }
            DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(p, false);
            if (directPawnRelation == null)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            Pawn other = directPawnRelation.otherPawn;
            if (other.Faction != p.Faction || p.Spawned != other.Spawned || p.Tile != other.Tile || !directPawnRelation.otherPawn.relations.everSeenByPlayer)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return ThoughtState.ActiveAtStage(2);
        }
    }
    public class ThoughtWorker_UrMyEverything : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            foreach (DirectPawnRelation dpr in LovePartnerRelationUtility.ExistingLovePartners(pawn))
            {
                if (dpr.otherPawn == other)
                {
                    return true;
                }
            }
            return false;
        }
    }
    //mariners like being in water cells or exposed to rain
    public class ThoughtWorker_ILoveWater : ThoughtWorker
    {
        public override float MoodMultiplier(Pawn p)
        {
            if (p.Position.GetTerrain(p.Map).IsWater)
            {
                return 3f * base.MoodMultiplier(p);
            }
            if (!p.Position.Roofed(p.Map))
            {
                return this.rainCurve.Evaluate(p.Map.weatherManager.CurWeatherLerped.rainRate);
            }
            return 0f;
        }
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.Spawned)
            {
                if (p.Position.GetTerrain(p.Map).IsWater)
                {
                    return ThoughtState.ActiveDefault;
                }
                if (!p.Position.Roofed(p.Map) && p.Map.weatherManager.curWeather.rainRate >= 0.5f)
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
        protected SimpleCurve rainCurve = new SimpleCurve(new CurvePoint[]
        {
            new CurvePoint(0.5f, 1f),
            new CurvePoint(1f, 3f),
            new CurvePoint(3f, 5f)
        });
    }
    //people pleasers gain or lose mood in proportion to how much everyone in their social card likes or dislikes them. The effect can't exceed +/-20
    public class Thought_Lovefool : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                List<Pawn> list = SocialCardUtility.PawnsForSocialInfo(pawn);
                float howImThoughtOf = 0f;
                foreach (Pawn p in list)
                {
                    if (p.relations != null)
                    {
                        howImThoughtOf += p.relations.OpinionOf(this.pawn);
                    }
                }
                return Math.Min(20f, Math.Max(-20f, this.CurStage.baseMoodEffect * howImThoughtOf));
            }
        }
    }
    //guess. i'm not gonna hold your hand for this one.
    public class ThoughtWorker_HVT_SkulkerIsInvisible : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.IsPsychologicallyInvisible())
            {
                return ThoughtState.ActiveDefault;
            }
            return ThoughtState.Inactive;
        }
    }
    //snipers experience the opposite feeling brawlers have - you avoid a mood loss for having a ranged weapon
    public class ThoughtWorker_SniperRangedWeapon : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveDefault;
        }
    }
    /*tempestophile gains stacking mood for every game condition tagged with TempestophileLikedCondition (there are tons of modded and nilla GCs out there that aren't atmospheric phenomena, so a whitelist approach makes more sense here)
     * as well as a flat mood boost if the current weather isn't tagged with TempestophileDislikeCondition (Clear, Underground, or possibly a few weather types added by mods. Most weather IS weather so a blacklist approach is good)
     * In order for any of this to trigger, you gotta be exposed to the weather i.e. not under a roof*/
    public class Thought_TempestWeather : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = 0f;
                if (this.pawn.Spawned && this.pawn.Map != null && this.pawn.Position.GetRoof(this.pawn.Map) == null)
                {
                    for (int i = 0; i < this.pawn.Map.GameConditionManager.ActiveConditions.Count; i++)
                    {
                        GameCondition gc = this.pawn.Map.GameConditionManager.ActiveConditions[i];
                        if (gc.def.HasModExtension<TempestophileLikedCondition>())
                        {
                            num += 10;
                        }
                    }
                    if (!this.pawn.Map.weatherManager.curWeather.HasModExtension<TempestophileDisLikedCondition>())
                    {
                        num += 4;
                    }
                }
                return num;
            }
        }
    }
    public class TempestophileLikedCondition : DefModExtension
    {
        public TempestophileLikedCondition()
        {
        }
    }
    public class TempestophileDisLikedCondition : DefModExtension
    {
        public TempestophileDisLikedCondition()
        {
        }
    }
    //textile dislikes being nude, and has an opinion malus of people who don't cover their groins or chest. Despite the latter's code being for a precept thoughtWorker, it works without needing Ideology.
    public class ThoughtWorker_ClothistVsSelf : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p.apparel.PsychologicallyNude;
        }
    }
    public class ThoughtWorker_BroYourGroinOrChestIsUncovered : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!p.story.CaresAboutOthersAppearance)
            {
                return false;
            }
            return ThoughtWorker_Precept_GroinOrChestUncovered.HasUncoveredGroinOrChest(otherPawn);
        }
    }
    /*vain loses mood for having any Normal- or worse-quality apparel (worse is worse).
     * They gain 5x their beauty as mood, and have a flat mood bonus per masterwork or legendary item worn.
     * Items carried or wielded-as-weapons are not part of these calculations. Only items worn are.*/
    public class ThoughtWorker_VainApparelQuality : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            int stage = -1;
            for (int i = 0; i < p.apparel.WornApparelCount; i++)
            {
                Apparel apparel = p.apparel.WornApparel[i];
                if (apparel.TryGetQuality(out QualityCategory cat))
                {
                    if (cat == QualityCategory.Awful)
                    {
                        return ThoughtState.ActiveAtStage(0);
                    }
                    else if (cat == QualityCategory.Poor)
                    {
                        stage = 1;
                    }
                    else if (cat == QualityCategory.Normal && stage != 1)
                    {
                        stage = 2;
                    }
                }
            }
            if (stage == -1)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(stage);
        }
    }
    public class Thought_VanityBeautyAndGreatItems : Thought_Situational
    {
        protected override float BaseMoodOffset
        {
            get
            {
                float num = Math.Max(-40f, Math.Min(40f, 5f * pawn.GetStatValue(StatDefOf.PawnBeauty, true, -1)));
                for (int i = 0; i < this.pawn.apparel.WornApparelCount; i++)
                {
                    Apparel apparel = this.pawn.apparel.WornApparel[i];
                    if (apparel.TryGetQuality(out QualityCategory cat))
                    {
                        if (cat == QualityCategory.Masterwork)
                        {
                            num += 3f;
                        }
                        else if (cat == QualityCategory.Legendary)
                        {
                            num += 6f;
                        }
                    }
                }
                return num;
            }
        }
    }
    //weapon artists like having unique weapons. I am aware of the apparent irony of the worker's name, but the procedural generation of unique weapons is shallow enough that they're only slightly more unique than some PoE uniques, which are infamously the furthest thing from unique.
    public class ThoughtWorker_ButThisOneIsMine : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (ModsConfig.OdysseyActive & p.equipment != null)
            {
                ThingWithComps twc = p.equipment.Primary;
                if (twc != null && twc.HasComp<CompUniqueWeapon>())
                {
                    return ThoughtState.ActiveDefault;
                }
            }
            return ThoughtState.Inactive;
        }
    }
    //if you have the winsome trait, people like you more. no other conditions, except as set in XML via nullifyingTraits or the like
    public class ThoughtWorker_Winsome : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (!other.story.traits.HasTrait(HVTDefOf.HVT_Winsome))
            {
                return false;
            }
            return true;
        }
    }
}
