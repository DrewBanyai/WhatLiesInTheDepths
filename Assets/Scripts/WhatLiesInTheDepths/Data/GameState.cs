// What Lies In The Depths — the scene's handle on the dream.
//
// Everything the dream is and does lives in Dream, a plain class with no Unity lifetime, so
// the whole path can be played headlessly (a test, a balancing bot) exactly as the scene
// plays it. This component only owns one, drives it from the clocks, and forwards its
// surface so every view keeps reading GameState.I the way it always has.
using System;
using System.Collections.Generic;
using UnityEngine;
using Ursine;
using Ursine.Economy;

namespace WhatLiesInTheDepths.Data
{
    public sealed class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        [Tooltip("Debug: load the spec set's mid-game snapshot with everything shown, instead of "
               + "starting a new dream from the first journal entry.")]
        public bool startMidGame;

        public Dream Dream { get; private set; }

        // ---- the dream, forwarded ---------------------------------------------------

        public Ledger ledger => Dream.ledger;
        public Unlocks unlocks => Dream.unlocks;
        public List<FocusTask> tasks => Dream.tasks;
        public List<ConstructDef> constructs => Dream.constructs;
        public List<RevelationDef> revelations => Dream.revelations;
        public List<string> absorbed => Dream.absorbed;
        public List<VisionDef> visions => Dream.visions;
        public List<VisionDef> visionsAbsorbed => Dream.visionsAbsorbed;
        public List<RoadLocation> road => Dream.road;
        public List<UnitDef> units => Dream.units;
        public Dictionary<string, UnitDef> unitsUnlockedBy => Dream.unitsUnlockedBy;
        public List<JournalEntry> journal => Dream.journal;
        public List<VeilDef> veils => Dream.veils;
        public VeilDef veil => Dream.veil;
        public bool HasNextVeil => Dream.HasNextVeil;

        public string attendedTaskId { get => Dream.attendedTaskId; set => Dream.attendedTaskId = value; }
        public bool attendingDive { get => Dream.attendingDive; set => Dream.attendingDive = value; }
        public double musterCostScale { get => Dream.musterCostScale; set => Dream.musterCostScale = value; }

        public List<Res> resources => Dream.resources;

        public event Action Changed
        {
            add => Dream.Changed += value;
            remove => Dream.Changed -= value;
        }

        public void Dirty() => Dream.Dirty();

        public Res Find(string k) => Dream.Find(k);
        public double Held(string k) => Dream.Held(k);
        public double Ceiling(string k) => Dream.Ceiling(k);
        public bool Short(string k, double n) => Dream.Short(k, n);
        public bool AboveCeiling(string k, double n) => Dream.AboveCeiling(k, n);
        public Refusal Judge(IEnumerable<Amount> cost) => Dream.Judge(cost);
        public string ReasonLine(IEnumerable<Amount> cost) => Dream.ReasonLine(cost);
        public bool Spend(IEnumerable<Amount> cost) => Dream.Spend(cost);
        public void Grant(IEnumerable<Amount> gain) => Dream.Grant(gain);

        public int OneiriTotal => Dream.OneiriTotal;
        public int OneiriBound => Dream.OneiriBound;
        public int OneiriFree => Dream.OneiriFree;

        public bool Shown(Res r) => Dream.Shown(r);
        public bool Shown(FocusTask t) => Dream.Shown(t);
        public bool Shown(ConstructDef c) => Dream.Shown(c);
        public bool Shown(RevelationDef r) => Dream.Shown(r);
        public bool Shown(VisionDef v) => Dream.Shown(v);
        public bool Shown(UnitDef u) => Dream.Shown(u);
        public bool LedgerOpen => Dream.LedgerOpen;
        public bool GaugeOpen => Dream.GaugeOpen;
        public bool BindingOpen => Dream.BindingOpen;
        public bool MenuOpen(string destination) => Dream.MenuOpen(destination);
        public IEnumerable<Res> ShownResources => Dream.ShownResources;
        public IEnumerable<FocusTask> ShownTasks => Dream.ShownTasks;
        public IEnumerable<ConstructDef> ShownConstructs => Dream.ShownConstructs;
        public IEnumerable<RevelationDef> ShownRevelations => Dream.ShownRevelations;

        public List<AchievementDef> achievements => Dream.achievements;
        public bool Earned(AchievementDef a) => Dream.Earned(a);
        public bool AchievementsOpen => Dream.AchievementsOpen;
        public Hold HoldOf(FocusTask t) => Dream.HoldOf(t);
        public Hold HoldOfDive => Dream.HoldOfDive;

        public int Version => Dream.Version;
        public bool Withdrawn(RevelationDef r) => Dream.Withdrawn(r);
        public bool Open(RoadLocation l) => Dream.Open(l);
        public void TakePlace(RoadLocation l, double army) => Dream.TakePlace(l, army);
        public void LosePlace(RoadLocation l) => Dream.LosePlace(l);

        public bool Build(ConstructDef c) => Dream.Build(c);
        public bool Realize(RevelationDef r) => Dream.Realize(r);
        public bool PartVeil() => Dream.PartVeil();

        // ---- lifetime -------------------------------------------------------------

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            Dream = new Dream(startMidGame);
        }

        void Start()
        {
            // Subscribed here rather than in OnEnable so the clock's Awake has certainly run.
            if (GameClock.I != null) GameClock.I.Tick += OnTick;
        }

        void OnDestroy()
        {
            if (GameClock.I != null) GameClock.I.Tick -= OnTick;
            if (I == this) I = null;
        }

        void Update() => Dream?.Step(Time.deltaTime);

        void OnTick() => Dream?.Tick();
    }
}
