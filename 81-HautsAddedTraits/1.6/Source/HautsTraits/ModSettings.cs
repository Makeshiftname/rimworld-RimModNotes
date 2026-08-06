using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace HautsTraits
{
    public class HVT_Settings : ModSettings
    {
        //as per Steam user request, you can disable Mighty and Herculean turning thin/average bodies into hulks
        public bool strengthTraitsChangeBodyType = true;
        /*people downloading a trait mod don't necessarily expect it to contain new challenges, especially not new ways to make raids harder. So, you can turn that feature off if you don't like it.
         * I DO want people to be surprised by skulker raids though, so the "easier" variants start on by default, and the "OH GOD OH FUCK" variants (Assassination, Sabotage) require a deliberate opt-in.*/
        public bool disableStealthRaids = false;
        public bool disableHardStealthRaids = true;
        //arising from a discussion on the balance of personality neuroformatters - you can now opt for them to be nerfed (by starting with less charges).
        public float pnfStartingCharges = 10;
        //adjust the price of personality neuroformatter.
        public float pnfCostFactor = 1f;
        /*I don't have a solid idea for how many transcendences a single pawn should be able to have. Originally, the idea was that you'd only ever have one at once. But around the time I hit twentiesh transes
         * I started thinking about synergies. Specifically, what if you had a Scarab (incoming damage is taken as neural heat gain if you wouldn't exceed the limiter) Hornet (consumes neural heat to fire explosive bolts)?
         * There's a handful of other clear two-trans synergies (T4Ts?) which has led me to settle on 2 as the default. But I think you should be able to stack an obscene number of transes on one pawn if you're
         * very lucky and/or knowledgeable and dedicated to doing that, which is why you can go even further beyond.*/
        public float maxTranscendences = 2f;
        /*woke genes have to be weaker than woke traits in one or more ways, since you can stack them whereas you can't ordinarily stack woke traits. Part of what I settled on is that they should be less likely to
         * grant you transes. However, should you feel like this is a bullshit arbitrary hidden limitation, you can bump the setting up all the way to 1 (or if you feel it's simply a difference of the wrong severity, otherwise
         * adjust it); and I've actually heard arguments that the woke genes need to be WEAKER so perhaps the people advancing those arguments might actually move this slider all the way down to 0.*/
        public float wokeGeneTransSuccessChance = 0.6f;
        /*several comments have expressed that the persistent Bestower warble around trans pawns is visually distracting, so here's an option to turn it off.
         * Considering getting rid of this mod setting and just making it a per-pawn toggle the way auras are. This would be nice because, much like auras, it would allow the warble to still be around NPCs
         * and therefore provide an immediate signal that some new friend (or more likely foe) entering your map has friggin' superpowers, while also allowing people who find it annoying to minimize its
         * incidence in their games. However, it would add a whole other button to trans pawns which yeah now that I type that out it sounds like a really weak argument against. Ok, I'll get around to doing this overhaul*/
        public bool visibleTransEffect = true;
        /*Transcendence did not used to be described anywhere in the game. A commenter pointed this out to me (or, rather, we had a debate and it helped me realize this), so a solution was implemented:
         * you learn that there's something else you can do with woke pawns once you HAVE one (either a colonist awakens, or you recruit someone who's already woke). This comes in the form of a "Revelation" letter
         * that tells you a specific method of achieving transcendence. On the technical side, this is handled by HediffCompProperties_Transcendenceinator, a comp of a hediff all woke pawns have.
         * The letter preserves the mystery, it lets players cultivate metaknowledge (which is always a fun feeling), it builds anticipation, it provides a goal... I don't feel it's the best way to have handled this,
         * but it is effective and I have yet to think of a better one.*/
        public bool enableTranscendenceHints = true;
        /*The name "MAX_TRAITS" is shamelessly copied from [KV] More Trait Slots, whose code I read to start understanding how to make ModSettings and their attendant windows. The code for min and max traits, however,
         * is completely different, in no small part because I did not understand how anything MTS was doing to traits worked (I still don't, but that might be because I haven't looked at that code since like, early 2023).*/
        public int MAX_TRAITS = -1;
        /*1 and 3 are the defaults because that's the vanilla experience. Obviously, a Harmony patch handles these. Read the tooltips for these mod settings to get the full gist.*/
        public float traitsMin = 1f;
        public float traitsMax = 3f;
        /*makes Xenotypists hate anyone of a different species. While this logically makes sense, it's false by default because it's not an advertised premise of the trait and a literal, RAW reading of Xenotypist would
         * theoretically indicate that they have no issue with other species, so RAW-readers might get upset if this was true by default.*/
        public bool genePuristsHateAliens = false;
        //Biotech integration for an Anomaly trait; monster lovers like other xenotypes, working exactly the opposite of how xenotypists work
        public bool monsterLoveForXenos = false;
        //as genePuristsHateAliens, but it's for Monster Lovers, and it's positive
        public bool monsterLoveForAliens = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref strengthTraitsChangeBodyType, "strengthTraitsChangeBodyType", true);
            Scribe_Values.Look(ref disableStealthRaids, "disableStealthRaids", false);
            Scribe_Values.Look(ref disableHardStealthRaids, "disableHardStealthRaids", true);
            Scribe_Values.Look(ref pnfStartingCharges, "pnfStartingCharges", 10f);
            Scribe_Values.Look(ref pnfCostFactor, "pnfCostFactor", 1f);
            Scribe_Values.Look(ref maxTranscendences, "maxTranscendences", 2f);
            Scribe_Values.Look(ref wokeGeneTransSuccessChance, "wokeGeneTransSuccessChance", 0.6f);
            Scribe_Values.Look(ref visibleTransEffect, "visibleTransEffect", true);
            Scribe_Values.Look(ref enableTranscendenceHints, "enableTranscendenceHints", true);
            Scribe_Values.Look(ref traitsMin, "traitsMin", 1);
            if (traitsMax < traitsMin)
            {
                traitsMax = traitsMin;
            }
            Scribe_Values.Look(ref traitsMax, "traitsMax", 3);
            Scribe_Values.Look(ref genePuristsHateAliens, "genePuristsHateAliens", false);
            Scribe_Values.Look(ref monsterLoveForXenos, "monsterLoveForXenos", false);
            Scribe_Values.Look(ref monsterLoveForAliens, "monsterLoveForAliens", false);
            base.ExposeData();
        }
    }
    public class HVT_Mod : Mod
    {
        public HVT_Mod(ModContentPack content) : base(content)
        {
            HVT_Mod.settings = GetSettings<HVT_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            //strength body type and skulker event settings
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("HVT_SettingSkulkerEvents".Translate(), ref settings.disableStealthRaids, "HVT_TooltipSkulkerEvents".Translate());
            listingStandard.CheckboxLabeled("HVT_SettingSkulkerEvents2".Translate(), ref settings.disableHardStealthRaids, "HVT_TooltipSkulkerEvents2".Translate());
            listingStandard.CheckboxLabeled("HVT_SettingStrengthBodyType".Translate(), ref settings.strengthTraitsChangeBodyType, "HVT_TooltipStrengthBodyType".Translate());
            if (ModsConfig.BiotechActive)
            {
                listingStandard.CheckboxLabeled("HVT_GenePuristsHAR".Translate(), ref settings.genePuristsHateAliens, "HVT_GenePuristsHARTooltip".Translate());
                if (ModsConfig.AnomalyActive)
                {
                    listingStandard.CheckboxLabeled("HVT_MonsterLoveXeno".Translate(), ref settings.monsterLoveForXenos, "HVT_MonsterLoveXenoTooltip".Translate());
                }
            }
            if (ModsConfig.AnomalyActive)
            {
                listingStandard.CheckboxLabeled("HVT_MonsterLoveHAR".Translate(), ref settings.monsterLoveForAliens, "HVT_MonsterLoveHARTooltip".Translate());
            }
            listingStandard.End();
            //traits-per-pawn settings
            if (settings.MAX_TRAITS == -1)
            {
                settings.MAX_TRAITS = DefDatabase<TraitDef>.AllDefsListForReading.Count();
            }
            displayMin = ((int)settings.traitsMin).ToString();
            displayMax = ((int)settings.traitsMax).ToString();
            float x = inRect.xMin, y = inRect.yMin + 155, halfWidth = inRect.width * 0.5f;
            float orig = settings.traitsMin;
            Rect traitsMinRect = new Rect(x + 10, y, halfWidth - 15, 32);
            settings.traitsMin = Widgets.HorizontalSlider(traitsMinRect, settings.traitsMin, 1f, 3f, true, "HVT_SettingMinTraits".Translate(), "1", "3", 1f);
            TooltipHandler.TipRegion(traitsMinRect.LeftPart(1f), "HVT_TooltipMinTraits".Translate());
            if (orig != settings.traitsMin)
            {
                displayMin = ((int)settings.traitsMin).ToString();
            }
            y += 32;
            string origString = displayMin;
            displayMin = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayMin);
            if (!displayMin.Equals(origString))
            {
                this.ParseInput(displayMin, settings.traitsMin, 3, out settings.traitsMin);
            }
            if (settings.traitsMin > settings.traitsMax)
            {
                settings.traitsMax = settings.traitsMin;
                displayMax = ((int)settings.traitsMax).ToString();
            }
            y -= 32;
            orig = settings.traitsMax;
            Rect traitsMaxRect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            settings.traitsMax = Widgets.HorizontalSlider(traitsMaxRect, settings.traitsMax, 3f, 12f, true, "HVT_SettingMaxTraits".Translate(), "3", "12", 1f);
            TooltipHandler.TipRegion(traitsMaxRect.LeftPart(1f), "HVT_TooltipMaxTraits".Translate());
            if (orig != settings.traitsMax)
            {
                displayMax = ((int)settings.traitsMax).ToString();
            }
            y += 32;
            origString = displayMax;
            displayMax = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayMax);
            if (!displayMax.Equals(origString))
            {
                this.ParseInput(displayMax, settings.traitsMax, 6, out settings.traitsMax);
            }
            if (settings.traitsMax < settings.traitsMin)
            {
                settings.traitsMin = settings.traitsMax;
                displayMin = ((int)settings.traitsMin).ToString();
            }
            y += 32;
            Rect pnfRect = new Rect(x + 10, y, halfWidth - 15, 32);
            displayPNF = ((int)settings.pnfStartingCharges).ToString();
            float origPNF = settings.pnfStartingCharges;
            settings.pnfStartingCharges = Widgets.HorizontalSlider(pnfRect, settings.pnfStartingCharges, 1f, 10f, true, "HVT_SettingPNFStartingCharges".Translate(), "1", "10", 1f);
            TooltipHandler.TipRegion(pnfRect.LeftPart(1f), "HVT_TooltipPNFStartingCharges".Translate());
            if (origPNF != settings.pnfStartingCharges)
            {
                displayPNF = ((int)settings.pnfStartingCharges).ToString();
            }
            y += 32;
            string origStringPNF = displayPNF;
            displayPNF = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayPNF);
            if (!displayPNF.Equals(origStringPNF))
            {
                this.ParseInput(displayPNF, settings.pnfStartingCharges, 10, out settings.pnfStartingCharges);
            }
            y -= 32;
            Rect pnf2Rect = new Rect(x + 5 + halfWidth, y, halfWidth - 15, 32);
            float origPNF2 = settings.pnfCostFactor;
            settings.pnfCostFactor = Widgets.HorizontalSlider(pnf2Rect, settings.pnfCostFactor, 1f, 25f, true, "HVT_SettingPNFCostFactor".Translate(), "1x", "25x", 0.01f);
            TooltipHandler.TipRegion(pnf2Rect.LeftPart(1f), "HVT_TooltipPNFCostFactor".Translate());
            if (origPNF2 != settings.pnfCostFactor)
            {
                displayPNF2 = settings.pnfCostFactor.ToStringByStyle(ToStringStyle.FloatTwo);
            }
            y += 32;
            string origStringPNF2 = displayPNF2;
            displayPNF2 = Widgets.TextField(new Rect(x + 5 + halfWidth, y, 50, 32), displayPNF2);
            if (!displayPNF2.Equals(origStringPNF2))
            {
                this.ParseInput(displayPNF2, settings.pnfCostFactor, 6, out settings.pnfCostFactor);
            }
            //transcendence settings
            y += 70;
            if (ModsConfig.RoyaltyActive)
            {
                displayTransMax = ((int)settings.maxTranscendences).ToString();
                displayWokeGeneChance = (settings.wokeGeneTransSuccessChance).ToString();
                float origR = settings.maxTranscendences;
                Rect transMaxRect = new Rect(x + 10, y, halfWidth - 15, 32);
                settings.maxTranscendences = Widgets.HorizontalSlider(transMaxRect, settings.maxTranscendences, 1f, 5f, true, "HVT_SettingMaxTrans".Translate(), "1", "5", 1f);
                TooltipHandler.TipRegion(transMaxRect.LeftPart(1f), "HVT_TooltipMaxTrans".Translate());
                if (origR != settings.maxTranscendences)
                {
                    displayTransMax = ((int)settings.maxTranscendences).ToString();
                }
                y += 32;
                string origStringR = displayTransMax;
                displayTransMax = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayTransMax);
                if (!displayTransMax.Equals(origStringR))
                {
                    this.ParseInput(displayTransMax, settings.maxTranscendences, 5, out settings.maxTranscendences);
                }
                y += 35;
                Rect inRect2 = new Rect(x, y, inRect.width, 65);
                Listing_Standard l2 = new Listing_Standard();
                l2.Begin(inRect2);
                l2.CheckboxLabeled("HVT_SettingVisibleFXTrans".Translate(), ref settings.visibleTransEffect, "HVT_TooltipVisibleFXTrans".Translate());
                l2.CheckboxLabeled("HVT_SettingTransHints".Translate(), ref settings.enableTranscendenceHints, "HVT_TooltipTransHints".Translate());
                l2.End();
                y += 70;
                if (ModsConfig.BiotechActive)
                {
                    origR = settings.wokeGeneTransSuccessChance;
                    Rect wokeGeneRect = new Rect(x + 10, y, halfWidth - 15, 32);
                    settings.wokeGeneTransSuccessChance = Widgets.HorizontalSlider(wokeGeneRect, settings.wokeGeneTransSuccessChance, 0f, 1f, true, "HVT_SettingWokeGenes".Translate(), "0%", "100%");
                    TooltipHandler.TipRegion(wokeGeneRect.LeftPart(1f), "HVT_TooltipWokeGenes".Translate());
                    if (origR != settings.wokeGeneTransSuccessChance)
                    {
                        displayWokeGeneChance = ((int)settings.wokeGeneTransSuccessChance).ToString();
                    }
                    y += 32;
                    origStringR = displayWokeGeneChance;
                    displayWokeGeneChance = Widgets.TextField(new Rect(x + 10, y, 50, 32), displayWokeGeneChance);
                    if (!displayWokeGeneChance.Equals(origStringR))
                    {
                        this.ParseInput(displayWokeGeneChance, settings.wokeGeneTransSuccessChance, 100, out settings.wokeGeneTransSuccessChance);
                    }
                }
            }
            base.DoSettingsWindowContents(inRect);
        }
        private void ParseInput(string buffer, float origValue, float maxValue, out float newValue)
        {
            if (!float.TryParse(buffer, out newValue))
                newValue = origValue;
            if (newValue < 0 || newValue > maxValue)
                newValue = origValue;
        }
        public override string SettingsCategory()
        {
            return "Hauts' Added Traits";
        }
        public static HVT_Settings settings;
        public string displayMin, displayMax, displayPNF, displayPNF2, displayTransMax, displayWokeGeneChance;
    }
}
