using RimWorld;
using Verse;

namespace HautsTraits
{
    [DefOf]
    public static class HVTDefOf
    {
        static HVTDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HVTDefOf));
        }

        public static TraitDef HVT_Aestheticist;
        public static TraitDef HVT_Agrizoophobe;
        public static TraitDef HVT_Allegiant;
        public static TraitDef HVT_Asocial;
        public static TraitDef HVT_Bookworm;
        public static TraitDef HVT_Champion;
        public static TraitDef HVT_Conversationalist;
        public static TraitDef HVT_Curmudgeon;
        public static TraitDef HVT_Daydreamer;
        public static TraitDef HVT_Forgettable;
        public static TraitDef HVT_Graver;
        public static TraitDef HVT_Hedonist;
        public static TraitDef HVT_Judgemental;
        public static TraitDef HVT_Lech;
        public static TraitDef HVT_Lovesick;
        public static TraitDef HVT_MetabolicFreak;
        public static TraitDef HVT_Outdoorsy;
        public static TraitDef HVT_RepressedRage;
        public static TraitDef HVT_Reveller;
        public static TraitDef HVT_Sadist;
        public static TraitDef HVT_Skulker;
        public static TraitDef HVT_Sniper;
        public static TraitDef HVT_Staid;
        public static TraitDef HVT_Tempestophile;
        public static TraitDef HVT_Tranquil0;
        public static TraitDef HVT_Tranquil;
        public static TraitDef HVT_Vain;
        public static TraitDef HVT_Winsome;
        public static TraitDef HVT_Strong;
        public static TraitDef HVT_Humble;
        public static TraitDef HVT_Prideful;
        [MayRequireIdeology]
        public static TraitDef HVT_Doubtful;
        [MayRequireIdeology]
        public static TraitDef HVT_Intolerant;
        [MayRequireIdeology]
        public static TraitDef HVT_Tolerant;
        [MayRequireIdeology]
        public static TraitDef HVT_Subjugator;
        [MayRequireIdeology]
        public static TraitDef HVT_Conformist;
        [MayRequireBiotech]
        public static TraitDef HVT_Caretaker;
        [MayRequireBiotech]
        public static TraitDef HVT_Misopedist;
        [MayRequireBiotech]
        public static TraitDef HVT_Mechaphile;
        [MayRequireBiotech]
        public static TraitDef HVT_Mechaphobe;
        [MayRequireBiotech]
        public static TraitDef HVT_Environmentalist;
        [MayRequireBiotech]
        public static TraitDef HVT_GenePurist;
        [MayRequireBiotech]
        public static TraitDef HVT_Mentor;
        [MayRequireRoyalty]
        public static TraitDef HVT_Anarchist;
        [MayRequireRoyalty]
        public static TraitDef HVT_Servile;
        [MayRequireAnomaly]
        public static TraitDef HVT_MonsterHunter;
        [MayRequireAnomaly]
        public static TraitDef HVT_MonsterLover;
        [MayRequireAnomaly]
        public static TraitDef HVT_Twisted;
        [MayRequireOdyssey]
        public static TraitDef HVT_Earthborne;
        [MayRequireOdyssey]
        public static TraitDef HVT_Skybound;
        [MayRequireOdyssey]
        public static TraitDef HVT_Angler;
        [MayRequireOdyssey]
        public static TraitDef HVT_Pescatarian;
        [MayRequireOdyssey]
        public static TraitDef HVT_Scavenger;

        public static ThoughtDef HVT_Bibliophilia;
        public static ThoughtDef HVT_StimulatingConversation;
        public static ThoughtDef HVT_CurmudgeonlyMood;
        public static ThoughtDef HVT_CurmudgeonlyDislike;
        public static ThoughtDef HVT_ObservedLayingCorpseGraver;
        public static ThoughtDef HVT_LovesickLetdown;
        public static ThoughtDef HVT_RebuffedALovesick;
        public static ThoughtDef HVT_SadistSawMentalBreak;
        public static ThoughtDef HVT_SadistBad;
        [MayRequireBiotech]
        public static ThoughtDef HVT_MechaphileWitnessedMechDeath;
        [MayRequireBiotech]
        public static ThoughtDef HVT_MechaphobeKilledMech;
        [MayRequireAnomaly]
        public static ThoughtDef HVT_MonsterHunterWorld;
        [MayRequireOdyssey]
        public static ThoughtDef HVT_IHateFlying;
        [MayRequireOdyssey]
        public static ThoughtDef HVT_ILoveFlying;
        [MayRequireOdyssey]
        public static ThoughtDef HVT_FishinsReelyFun;
        [MayRequireOdyssey]
        public static ThoughtDef HVT_TheSnackThatSmilesBack;

        public static JobDef HVT_UseTraitGiverSerum;
        public static JobDef HVT_DisarmExplosive;

        [MayRequireAnomaly]
        public static MentalStateDef HVT_HumanityBreak;

        public static HediffDef HVT_RRUnleashed;
        public static HediffDef HVT_SkulkerSurpriseStealth;
        public static HediffDef HVT_BurgleCooldown;
        [MayRequireIdeology]
        public static HediffDef HVT_RadThinkerBuff;
        [MayRequireBiotech]
        public static HediffDef HVT_DoubleGrowthMoments;

        public static ThingDef Hauts_SabotageIED_HighExplosive;
        public static ThingDef Hauts_SabotageIED_AntigrainWarhead;

        public static PawnsArrivalModeDef HVT_SkulkIn;
        public static PawnsArrivalModeDef HVT_SkulkInBaseCluster;
        public static PawnsArrivalModeDef HVT_SkulkInBaseSplitUp;
        public static PawnsArrivalModeDef HVT_Assassins;
        public static PawnsArrivalModeDef HVT_SabotagePAM;
    }
}
