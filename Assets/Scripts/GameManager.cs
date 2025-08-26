using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state + star logic + phase gating.
/// - Tracks score and maxScore
/// - Owns star thresholds (1–3)
/// - Raises events when score changes and stars are reached
/// - Stores absorbed sentence final versions (+ whether top threshold was reached)
/// - Controls phase lifecycle so other systems can gate spawning on AllowSpawning
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score & Stars")]
    [Tooltip("Computed/assigned max score for this level.")]
    public int maxScore = 300;

    [Tooltip("If true, compute maxScore from SentenceSpawner definitions when they register.")]
    public bool autoComputeMaxScore = true;

    [Tooltip("Score thresholds for 1, 2, and 3 stars (ascending).")]
    public int[] starThresholds = new int[3] { 100, 200, 300 };

    public int CurrentScore { get; private set; }

    [Header("Phase / Level State")]
    /// <summary>True once EndLevel() is called.</summary>
    public bool LevelHasEnded { get; private set; } = false;
    /// <summary>True between BeginPhase() and CompletePhase().</summary>
    public bool PhaseActive   { get; private set; } = false;
    public int  CurrentPhaseIndex { get; private set; } = -1;
    public int  TotalPhases       { get; private set; } = 0;

    /// <summary>Convenience for spawners: only spawn while a phase is active and the level isn't over.</summary>
    public bool AllowSpawning => PhaseActive && !LevelHasEnded;

    float _phaseStartTime;
    public float PhaseElapsed => PhaseActive ? Time.time - _phaseStartTime : 0f;

    [Header("Debug")]
    [Tooltip("Print extra logs about phases/level to the Console.")]
    public bool verboseLogging = false;

    // --- Events ---
    public event Action<int,int> OnScoreChanged;   // (current, max)
    public event Action<int,int> OnStarReached;    // (starIndex 0..2, threshold)
    public event Action          OnLevelEnded;     // once per level
    public event Action<int>     OnPhaseStarted;   // phaseIndex
    public event Action<int>     OnPhaseEnded;     // phaseIndex

    // --- Absorbed sentences for reporting ---
    public struct AbsorbedSentence
    {
        public string text;   // displayed text at absorb time
        public bool   isTop;  // reached highest threshold?
        public AbsorbedSentence(string t, bool top) { text = t; isTop = top; }
    }
    private readonly List<AbsorbedSentence> _absorbed = new();

    private readonly HashSet<int> _starsAwarded = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
    }

    void Start()
    {
        // Announce initial state so HUDs can initialize (e.g., score bar).
        OnScoreChanged?.Invoke(CurrentScore, maxScore);
    }

    // ----------------- Score -----------------
    public void AddScore(int points)
    {
        CurrentScore = Mathf.Clamp(CurrentScore + Mathf.Max(0, points), 0, maxScore);
        OnScoreChanged?.Invoke(CurrentScore, maxScore);

        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (!_starsAwarded.Contains(i) && CurrentScore >= starThresholds[i])
            {
                _starsAwarded.Add(i);
                OnStarReached?.Invoke(i, starThresholds[i]);
            }
        }
    }

    public void RecordAbsorbedSentence(string displayedText, bool isTop)
    {
        if (!string.IsNullOrEmpty(displayedText))
            _absorbed.Add(new AbsorbedSentence(displayedText, isTop));
    }

    /// <summary>Legacy helper kept for compatibility (assumes not top).</summary>
    public void RecordAbsorbedSentenceVersion(string displayedText)
    {
        if (!string.IsNullOrEmpty(displayedText))
            _absorbed.Add(new AbsorbedSentence(displayedText, false));
    }

    public IReadOnlyList<AbsorbedSentence> GetAbsorbedSentences() => _absorbed.AsReadOnly();

    public int GetStarsEarned()
    {
        int earned = 0;
        for (int i = 0; i < starThresholds.Length; i++)
            if (CurrentScore >= starThresholds[i]) earned++;
        return Mathf.Clamp(earned, 0, 3);
    }

    public int[] GetStarThresholds() => starThresholds;

    // ------------- Phase / Level lifecycle -------------
    /// <summary>Call when the sentence for this phase reaches its on-screen position.</summary>
    public void BeginPhase(int phaseIndex, int totalPhases)
    {
        if (LevelHasEnded) return;
        CurrentPhaseIndex = phaseIndex;
        TotalPhases       = totalPhases;
        PhaseActive       = true;
        _phaseStartTime   = Time.time;

        if (verboseLogging) Debug.Log($"[GM] Phase START {phaseIndex}/{totalPhases - 1}");
        OnPhaseStarted?.Invoke(CurrentPhaseIndex);
    }

    /// <summary>Call when the sentence absorbs (end of the phase).</summary>
    public void CompletePhase(int phaseIndex)
    {
        if (!PhaseActive || phaseIndex != CurrentPhaseIndex) return;

        PhaseActive = false;
        if (verboseLogging) Debug.Log($"[GM] Phase END {phaseIndex}");
        OnPhaseEnded?.Invoke(phaseIndex);

        // By design: clear boids between phases.
        ClearAllBoids();
    }

    /// <summary>Call when there are no more phases (sentences) left.</summary>
    public void EndLevel()
    {
        if (LevelHasEnded) return;

        PhaseActive   = false;
        LevelHasEnded = true;

        if (verboseLogging) Debug.Log("[GM] Level ENDED");
        OnLevelEnded?.Invoke();
    }

    /// <summary>Clear all agents in the scene.</summary>
    public void ClearAllBoids()
    {
        foreach (var flock in FindObjectsOfType<Flock>(true))
            if (flock) flock.ClearAgents();

        
    }

    // ------------- maxScore computation -------------
    /// <summary>Let a spawner register its definitions so we can compute maxScore for this level.</summary>
    public void RegisterDefinitions(SentenceDefinition[] defs)
    {
        if (!autoComputeMaxScore || defs == null || defs.Length == 0) return;

        maxScore = ComputeMaxScoreFromDefinitions(defs);

        Array.Sort(starThresholds);
        for (int i = 0; i < starThresholds.Length; i++)
            starThresholds[i] = Mathf.Clamp(starThresholds[i], 0, Mathf.Max(1, maxScore));

        OnScoreChanged?.Invoke(CurrentScore, maxScore);
    }

    private int ComputeMaxScoreFromDefinitions(SentenceDefinition[] defs)
    {
        if (defs == null || defs.Length == 0) return Mathf.Max(1, maxScore);

        int sum = 0;
        foreach (var def in defs)
        {
            if (def == null) continue;

            float maxMult = 1f;
            foreach (var lvl in def.levels)
                if (lvl.multiplier > maxMult) maxMult = lvl.multiplier;

            sum += Mathf.RoundToInt(def.baseAbsorptionPoints * maxMult);
        }
        return Mathf.Max(1, sum);
    }
}
