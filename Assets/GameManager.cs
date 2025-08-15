using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state + star logic.
/// - Tracks score and maxScore
/// - Owns star thresholds (1–3)
/// - Raises events when score changes and stars are reached
/// - Stores absorbed sentence final versions + whether top threshold was reached
/// - Remembers if the level already ended so late UI can catch up
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score & Stars")]
    [Tooltip("Computed max score for this level. If Auto Compute is false, set this manually.")]
    public int maxScore = 300;

    [Tooltip("If true, compute maxScore from SentenceSpawner definitions at startup or when a spawner registers.")]
    public bool autoComputeMaxScore = true;

    [Tooltip("Score thresholds for 1, 2, and 3 stars (ascending)")]
    public int[] starThresholds = new int[3] { 100, 200, 300 };

    public int CurrentScore { get; private set; }

    // Events
    public event Action<int,int> OnScoreChanged;   // (current, max)
    public event Action<int,int> OnStarReached;    // (starIndex 0..2, threshold)
    public event Action OnLevelEnded;              // raised by EndLevel()

    /// <summary>True if EndLevel() has already been called.</summary>
    public bool LevelHasEnded { get; private set; } = false;

    // --- Absorbed sentences for reporting ---
    public struct AbsorbedSentence
    {
        public string text;     // displayed text at absorb time
        public bool   isTop;    // true if reached highest threshold
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
        if (autoComputeMaxScore)
        {
            var spawner = FindObjectOfType<SentenceSpawner>();
            if (spawner != null)
            {
                maxScore = ComputeMaxScoreFromDefinitions(spawner.definitions);
            }
            else
            {
                Debug.LogWarning("GameManager: no SentenceSpawner found for auto-compute; using configured maxScore.", this);
            }
        }

        // Ensure star thresholds are ascending and capped to maxScore
        Array.Sort(starThresholds);
        for (int i = 0; i < starThresholds.Length; i++)
            starThresholds[i] = Mathf.Clamp(starThresholds[i], 0, Mathf.Max(1, maxScore));

        // Announce initial state
        OnScoreChanged?.Invoke(CurrentScore, maxScore);
    }

    /// <summary>Adds to the player's score and raises star events when thresholds are crossed.</summary>
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

    /// <summary>Records final sentence text and whether it reached the top threshold.</summary>
    public void RecordAbsorbedSentence(string displayedText, bool isTop)
    {
        if (!string.IsNullOrEmpty(displayedText))
            _absorbed.Add(new AbsorbedSentence(displayedText, isTop));
    }

    /// <summary>Legacy call kept for compatibility (assumes not top).</summary>
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

    /// <summary>Ends the level and notifies listeners (only once).</summary>
    public void EndLevel()
    {
        if (LevelHasEnded) return;
        LevelHasEnded = true;
        OnLevelEnded?.Invoke();
    }

    /// <summary>Allow a spawner to register its definitions so we can compute maxScore.</summary>
    public void RegisterDefinitions(StaticSentenceDefinition[] defs)
    {
        if (!autoComputeMaxScore || defs == null || defs.Length == 0) return;
        maxScore = ComputeMaxScoreFromDefinitions(defs);

        // Re-clamp thresholds against new max
        Array.Sort(starThresholds);
        for (int i = 0; i < starThresholds.Length; i++)
            starThresholds[i] = Mathf.Clamp(starThresholds[i], 0, Mathf.Max(1, maxScore));

        OnScoreChanged?.Invoke(CurrentScore, maxScore);
    }

    // --- Helpers ---
    private int ComputeMaxScoreFromDefinitions(StaticSentenceDefinition[] defs)
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
