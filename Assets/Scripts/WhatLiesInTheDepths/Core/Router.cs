// What Lies In The Depths — what the center column is showing, and where the underline lives.
// Spec sections 3, 10 and 15. The underline lives where the button is, not where the
// content is, and exactly one exists across all three bars at any moment.
using System;
using UnityEngine;

namespace WhatLiesInTheDepths.Core
{
    /// <summary>The six center destinations, in reading order. Order is authored: a new
    /// destination lands in its slot, it is never appended because it is newest.</summary>
    public enum Destination
    {
        Journal = 0,
        Focus = 1,
        Constructs = 2,
        Revelations = 3,
        Visions = 4,
        Assault = 5
    }

    /// <summary>What the center column is actually showing. The utilities live in the right
    /// bar and display here, which is the one genuinely unusual thing about this system.</summary>
    public enum CenterSurface
    {
        Destination = 0,
        Options = 1,
        ExitQuestion = 2,
        HardResetQuestion = 3,
        Achievements = 4
    }

    public sealed class Router : MonoBehaviour
    {
        public static Router I { get; private set; }

        public event Action Changed;

        /// <summary>The ending is not a surface of the center column — it is the only thing
        /// that is not a column at all, and the columns fade out beneath it.</summary>
        public event Action<bool> EndingShown;

        [SerializeField] Destination _destination = Destination.Journal;
        CenterSurface _surface = CenterSurface.Destination;
        Destination _beforeUtility = Destination.Journal;
        bool _ending;

        /// <summary>The open center destination is remembered across a session.
        /// Options is not — a session never starts in Options.</summary>
        public Destination Destination => _destination;
        public CenterSurface Surface => _surface;
        public bool EndingOpen => _ending;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            _surface = CenterSurface.Destination;
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Pressing a center destination. It leaves either utility page at once,
        /// with no question asked — Exit is a question about leaving the game, not the panel.</summary>
        public void Open(Destination d)
        {
            _destination = d;
            _surface = CenterSurface.Destination;
            Changed?.Invoke();
        }

        /// <summary>Pressing Options. A second press returns to whatever was open before.</summary>
        public void ToggleOptions()
        {
            if (_surface == CenterSurface.Options) { BackToGame(); return; }
            if (_surface == CenterSurface.Destination) _beforeUtility = _destination;
            _surface = CenterSurface.Options;
            Changed?.Invoke();
        }

        /// <summary>Exit asks first. The question is a panel in the center column, not a window.</summary>
        /// <summary>Achievements works exactly as Options does: pressing it again puts it away,
        /// and pressing any center destination leaves it at once.</summary>
        public void ToggleAchievements()
        {
            if (_surface == CenterSurface.Achievements) { BackToGame(); return; }
            if (_surface == CenterSurface.Destination) _beforeUtility = _destination;
            _surface = CenterSurface.Achievements;
            Changed?.Invoke();
        }

        public void AskExit()
        {
            if (_surface == CenterSurface.Destination) _beforeUtility = _destination;
            _surface = CenterSurface.ExitQuestion;
            Changed?.Invoke();
        }

        /// <summary>The Hard reset question replaces the Options page rather than opening over it.</summary>
        public void AskHardReset()
        {
            _surface = CenterSurface.HardResetQuestion;
            Changed?.Invoke();
        }

        /// <summary>"No, stay" hands the center column back to whatever was open before;
        /// it is a navigation, not a dismissal.</summary>
        public void BackToGame()
        {
            _destination = _beforeUtility;
            _surface = CenterSurface.Destination;
            Changed?.Invoke();
        }

        /// <summary>"No, go back" returns to the Options page with everything as it was.</summary>
        public void BackToOptions()
        {
            _surface = CenterSurface.Options;
            Changed?.Invoke();
        }

        public void ShowEnding(bool on)
        {
            if (_ending == on) return;
            _ending = on;
            EndingShown?.Invoke(on);
        }

        /// <summary>The underline rule, in one place. A destination is lit only when the
        /// center is showing it; Options is lit whenever any utility page is open.</summary>
        public bool IsLit(Destination d) => _surface == CenterSurface.Destination && _destination == d;

        public bool IsOptionsLit() => _surface == CenterSurface.Options
                                   || _surface == CenterSurface.ExitQuestion
                                   || _surface == CenterSurface.HardResetQuestion;

        public bool IsAchievementsLit() => _surface == CenterSurface.Achievements;
    }
}
