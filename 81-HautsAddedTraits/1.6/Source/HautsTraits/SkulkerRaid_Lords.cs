using HautsFramework;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace HautsTraits
{
    //burglars start out already in the 'taking what they can and leaving' phase. Any hurt in combat decide to fight, and if you get three into a fight they all transition into a normal raid
    public class LordToil_Burgle : LordToil_DoOpportunisticTaskOrCover
    {
        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }
        protected override DutyDef DutyDef
        {
            get
            {
                return DutyDefOf.Steal;
            }
        }
        public override bool AllowSelfTend
        {
            get
            {
                return false;
            }
        }
        public override void Notify_PawnDamaged(Pawn victim, DamageInfo dinfo)
        {
            base.Notify_PawnDamaged(victim, dinfo);
            LordJob_StealFromColony sfc = (LordJob_StealFromColony)this.lord.LordJob;
            if (sfc != null)
            {
                sfc.maxPawnsAttackedBeforeRaid--;
                if (sfc.maxPawnsAttackedBeforeRaid < 0)
                {
                    Lord lord = LordMaker.MakeNewLord(victim.Faction, new LordJob_AssaultColony(victim.Faction, true, true, false, false, false, false, true), victim.Map, null);
                    List<Pawn> transferPawns = new List<Pawn>();
                    foreach (Pawn p in lord.ownedPawns)
                    {
                        transferPawns.Add(p);
                    }
                    lord.RemoveAllPawns();
                    foreach (Pawn p in transferPawns)
                    {
                        this.PutPawnInAssaultMode(lord, p);
                    }
                } else if (!victim.Dead) {
                    this.lord.RemovePawn(victim);
                    Lord lord = LordMaker.MakeNewLord(victim.Faction, new LordJob_AssaultColony(victim.Faction, true, true, false, false, false, false, true), victim.Map, null);
                    this.PutPawnInAssaultMode(lord, victim);
                }
            }
        }
        public void PutPawnInAssaultMode(Lord lord, Pawn p)
        {
            if (p.carryTracker.CarriedThing != null)
            {
                p.carryTracker.TryDropCarriedThing(p.Position, ThingPlaceMode.Near, out Thing thing, null);
            }
            lord.AddPawn(p);
            p.mindState.duty.def = DutyDefOf.AssaultColony;
        }
        protected override bool TryFindGoodOpportunisticTaskTarget(Pawn pawn, out Thing target, List<Thing> alreadyTakenTargets)
        {
            if (pawn.mindState.duty != null && pawn.mindState.duty.def == this.DutyDef && pawn.carryTracker.CarriedThing != null)
            {
                target = pawn.carryTracker.CarriedThing;
                return true;
            }
            return StealAIUtility.TryFindBestItemToSteal(pawn.Position, pawn.Map, 7f, out target, pawn, alreadyTakenTargets);
        }
        public override void UpdateAllDuties()
        {
            List<Thing> list = null;
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                Pawn pawn = this.lord.ownedPawns[i];
                Thing item = null;
                if (!this.cover || (this.TryFindGoodOpportunisticTaskTarget(pawn, out item, list) && !GenAI.InDangerousCombat(pawn)))
                {
                    if (pawn.mindState.duty == null || pawn.mindState.duty.def != this.DutyDef)
                    {
                        pawn.mindState.duty = new PawnDuty(this.DutyDef);
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                    }
                    if (list == null)
                    {
                        list = new List<Thing>();
                    }
                    list.Add(item);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Steal);
                }
            }
        }
        public override void LordToilTick()
        {
            if (this.cover && Find.TickManager.TicksGame % 181 == 0)
            {
                List<Thing> list = null;
                for (int i = 0; i < this.lord.ownedPawns.Count; i++)
                {
                    Pawn pawn = this.lord.ownedPawns[i];
                    if (!pawn.Downed && pawn.mindState.duty.def == DutyDefOf.AssaultColony)
                    {
                        if (this.TryFindGoodOpportunisticTaskTarget(pawn, out Thing thing, list) && !base.Map.reservationManager.IsReservedByAnyoneOf(thing, this.lord.faction) && !GenAI.InDangerousCombat(pawn))
                        {
                            pawn.mindState.duty = new PawnDuty(this.DutyDef);
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                            if (list == null)
                            {
                                list = new List<Thing>();
                            }
                            list.Add(thing);
                        }
                    }
                }
            }
        }
        public new bool cover = true;
    }
    public class LordJob_StealFromColony : LordJob
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_StealFromColony()
        {
        }
        public LordJob_StealFromColony(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_StealFromColony(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Burgle lordToil_StealCover = new LordToil_Burgle
            {
                useAvoidGrid = useAvoidGridSmart
            };
            stateGraph.AddToil(lordToil_StealCover);
            LordToil_Burgle lordToil_StealCover2 = new LordToil_Burgle
            {
                cover = false,
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_StealCover2);
            Transition transition = new Transition(lordToil_StealCover, lordToil_StealCover2, false, true);
            transition.AddTrigger(new Trigger_TicksPassedAndNoRecentHarm(1200));
            stateGraph.AddTransition(transition, false);
            LordToil_ExitMapAndDefendSelf lordToil_ExitMap = new LordToil_ExitMapAndDefendSelf
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_ExitMap);
            if (this.canTimeoutOrFlee)
            {
                Transition transition2 = new Transition(lordToil_StealCover, lordToil_ExitMap, false, true);
                transition2.AddSources(lordToil_StealCover2);
                transition2.AddTrigger(new Trigger_TicksPassed(BurgleTimeBeforeGiveUp.RandomInRange));
                transition2.AddTrigger(new Trigger_PawnHarmed(1, true, null));
                stateGraph.AddTransition(transition2, false);
            }
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }

        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
        public int maxPawnsAttackedBeforeRaid = 2;
        private static readonly IntRange BurgleTimeBeforeGiveUp = new IntRange(26000, 38000);
    }
    //they got into your base somehow, so doors shouldn't be an obstacle for them. THe rest of Assassins is handled by its unique PAM
    public class LordJob_Assassinate : LordJob_AssaultColony
    {
        public LordJob_Assassinate()
        {
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
    }
    /*50-50 chance for each Espionage pawn to act like normal raiders, or attempt to flee. Granted raid points are handled by assigning an Espionage hediff, which feeds the SpyPoints faction comp (see Framework).
     * A little convoluted, but this was literally the first NPC behavior work I did for RimWorld.*/
    public class LordToil_HalfRunHalfFight : LordToil
    {
        public override bool ForceHighStoryDanger
        {
            get
            {
                return true;
            }
        }
        public LordToil_HalfRunHalfFight(bool isEspionage = false, bool useAvoidGridSmart = false)
        {
            espionage = isEspionage;
            useAvoidGrid = useAvoidGridSmart;
        }
        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }
        public override void Init()
        {
            base.Init();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
        }
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                this.lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest)
                {
                    attackDownedIfStarving = false,
                    pickupOpportunisticWeapon = false
                };
                if (espionage)
                {
                    Hediff hediff = HediffMaker.MakeHediff(HautsDefOf.HVT_Spy, this.lord.ownedPawns[i], null);
                    this.lord.ownedPawns[i].health.AddHediff(hediff, null, null, null);
                }
            }
            if (this.lord.ownedPawns.Count > 1)
            {
                Lord lordStayBehindAndFight = LordMaker.MakeNewLord(this.lord.faction, new LordJob_AssaultColony(this.lord.faction, true, true, false, true), this.lord.Map, null);
                List<Pawn> thoseStayingBehind = new List<Pawn>();
                for (int i = 1; i < this.lord.ownedPawns.Count; i++)
                {
                    if (Rand.Value < 0.5f)
                    {
                        thoseStayingBehind.Add(this.lord.ownedPawns[i]);
                    }
                }
                for (int i = 1; i < thoseStayingBehind.Count; i++)
                {
                    this.lord.RemovePawn(thoseStayingBehind[i]);
                    lordStayBehindAndFight.AddPawn(thoseStayingBehind[i]);
                    thoseStayingBehind[i].mindState.duty.def = DutyDefOf.AssaultColony;
                }
            }
        }
        readonly bool espionage = false;
    }
    public class LordJob_Espionage : LordJob
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_Espionage()
        {
        }
        public LordJob_Espionage(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_Espionage(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_HalfRunHalfFight lordToil_Espionage = new LordToil_HalfRunHalfFight(true, useAvoidGridSmart);
            stateGraph.AddToil(lordToil_Espionage);
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }
        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
    }
    //they planted the mines somehow, so doors are clearly not an obstacle here either. They fight you for a bit, then they try to leave because the explosivesre gonna blow, then they try to book it real style
    public class LordJob_Sabotage : LordJob_AssaultColony
    {
        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public LordJob_Sabotage()
        {
        }
        public LordJob_Sabotage(SpawnedPawnParams parms)
        {
            this.assaulterFaction = parms.spawnerThing.Faction;
            this.canKidnap = false;
            this.canTimeoutOrFlee = false;
        }
        public LordJob_Sabotage(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool useAvoidGridSmart = false, bool canPickUpOpportunisticWeapons = false)
        {
            this.assaulterFaction = assaulterFaction;
            this.canKidnap = canKidnap;
            this.canTimeoutOrFlee = canTimeoutOrFlee;
            this.useAvoidGridSmart = useAvoidGridSmart;
            this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil assaultColony = new LordToil_AssaultColony(false, canPickUpOpportunisticWeapons);
            if (this.useAvoidGridSmart)
            {
                assaultColony.useAvoidGrid = true;
            }
            stateGraph.AddToil(assaultColony);
            LordToil_ExitMapAndDefendSelf exitMapAndDefendSelf = new LordToil_ExitMapAndDefendSelf
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(exitMapAndDefendSelf);
            Transition transition = new Transition(assaultColony, exitMapAndDefendSelf, false, true);
            transition.AddTrigger(new Trigger_TicksPassed(new IntRange(1250, 2500).RandomInRange));
            stateGraph.AddTransition(transition);
            LordToil_ExitMap exitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
            stateGraph.AddToil(exitMap);
            Transition transition2 = new Transition(exitMapAndDefendSelf, exitMap, false, true);
            transition.AddTrigger(new Trigger_TicksPassed(new IntRange(3500, 5000).RandomInRange));
            stateGraph.AddTransition(transition2);
            return stateGraph;
        }
        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
            Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
            Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
            Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
            Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
        }

        private Faction assaulterFaction;
        private bool canKidnap = true;
        private bool canTimeoutOrFlee = true;
        private bool useAvoidGridSmart = false;
        private bool canPickUpOpportunisticWeapons = false;
    }
}
