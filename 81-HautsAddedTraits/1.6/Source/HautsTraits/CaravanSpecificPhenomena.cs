using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace HautsTraits
{
    //used by multiple traits, including the Odyssey-dependent Globetrotter
    public class ThoughtWorker_InCaravan : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.IsFormingCaravan())
            {
                return ThoughtState.ActiveAtStage(0);
            }
            if (p.IsCaravanMember())
            {
                return ThoughtState.ActiveAtStage(1);
            }
            return ThoughtState.Inactive;
        }
    }
    /*provided you have a Skulker in your caravan, you can go to a world site via RMB float action menu option and receive a report about how many Ambush parts there are (or other hidden or threat point-based parts)
     * Scouting can fail if your skulkers have low sensory capabilities. See UtilityMethods.cs*/
    public class CaravanArrivalAction_ScoutForAmbushes : CaravanArrivalAction
    {
        public override string Label
        {
            get
            {
                return this.site.ApproachOrderString;
            }
        }
        public override string ReportString
        {
            get
            {
                return this.site.ApproachingReportString;
            }
        }
        public CaravanArrivalAction_ScoutForAmbushes()
        {
        }
        public CaravanArrivalAction_ScoutForAmbushes(Site site)
        {
            this.site = site;
        }
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (this.site != null && this.site.Tile != destinationTile)
            {
                return false;
            }
            return CaravanArrivalAction_VisitSite.CanVisit(caravan, this.site);
        }
        public override void Arrived(Caravan caravan)
        {
            this.ScoutForAmbushes(caravan, this.site);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Site>(ref this.site, "site", false);
        }
        public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, Site site)
        {
            if (site == null || !site.Spawned)
            {
                return false;
            }
            if (site.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(site.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true, false)));
            }
            return true;
        }
        public void ScoutForAmbushes(Caravan caravan, Site site)
        {
            float totalSkulkerPower = HVTUtility.ScoutingSkulkerPower(caravan);
            int ambushCount = 0;
            foreach (SitePart part in site.parts)
            {
                if ((part.def.defaultHidden || part.def.wantsThreatPoints) && Rand.Value < totalSkulkerPower * 0.4f)
                {
                    ambushCount++;
                }
            }
            TaggedString letterLabel;
            TaggedString letterText;
            LetterDef letterDef;
            if (ambushCount == 0)
            {
                letterLabel = "HVT_SFAletter1".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome1".Translate(site.LabelCap);
                letterDef = LetterDefOf.NeutralEvent;
            }
            else if (ambushCount == 1)
            {
                letterLabel = "HVT_SFAletter2".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome2".Translate(site.LabelCap);
                letterDef = LetterDefOf.ThreatSmall;
            }
            else
            {
                letterLabel = "HVT_SFAletter3".Translate(site.LabelCap);
                letterText = "HVT_SFAoutcome3".Translate(ambushCount, site.LabelCap);
                letterDef = LetterDefOf.ThreatSmall;
            }
            ChoiceLetter notification = LetterMaker.MakeLetter(
            letterLabel, letterText, letterDef, new LookTargets(site), null, null, null);
            Find.LetterStack.ReceiveLetter(notification, null);
        }
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, Site site)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitSite>(() => CaravanArrivalAction_VisitSite.CanVisit(caravan, site), () => new CaravanArrivalAction_VisitSite(site), site.ApproachOrderString, caravan, site.Tile, site, null);
        }
        private Site site;
    }
}
