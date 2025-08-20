using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName="Sentence/Sentence Definition")]
public class SentenceDefinition : ScriptableObject {
    [Tooltip("Base absorption points awarded when this sentence absorbs.")]
    public int baseAbsorptionPoints = 0;
    [Tooltip("Lifetime in seconds before absorption.")]
    public float lifetime = 5f;

    [Serializable]
    public class Level {
        [Tooltip("Score threshold for this level.")]
        public int threshold;
        [Tooltip("Text to display at this level.")]
        public string text;
        [Tooltip("Multiplier applied to baseAbsorptionPoints when absorbing at this level.")]
        public float multiplier = 1f;
    }

    [Tooltip("List of levels, in descending threshold order (highest first). One must have threshold==0.")]
    public List<Level> levels = new List<Level> { new Level { threshold = 0, text = "Default", multiplier = 1f } };

#if UNITY_EDITOR
    void OnValidate() {
        if (levels == null || levels.Count == 0) {
            levels = new List<Level> { new Level { threshold = 0, text = "Default", multiplier = 1f } };
        }
        bool hasZero = false;
        var seen = new HashSet<int>();
        for (int i = 0; i < levels.Count; i++) {
            var lvl = levels[i];
            if (lvl.threshold == 0) hasZero = true;
            if (!seen.Add(lvl.threshold)) {
                Debug.LogError($"{name}: duplicate threshold {lvl.threshold} at index {i}", this);
            }
            if (i > 0 && levels[i-1].threshold < lvl.threshold) {
                Debug.LogError($"{name}: levels must be in descending threshold order: idx {i-1}({levels[i-1].threshold}) >= idx {i}({lvl.threshold})", this);
            }
        }
        if (!hasZero) {
            Debug.LogError($"{name}: must have a level with threshold == 0", this);
        }
    }
#endif

    /// <summary>
    /// Returns the Level corresponding to the given polarity score.
    /// </summary>
    public Level GetLevelForScore(int score) {
        Level result = levels.Find(l => l.threshold == 0);
        // positive thresholds (first match)
        foreach (var lvl in levels) {
            if (lvl.threshold > 0 && score >= lvl.threshold) {
                result = lvl;
                break;
            }
        }
        // negative thresholds (last match)
        for (int i = levels.Count - 1; i >= 0; i--) {
            var lvl = levels[i];
            if (lvl.threshold < 0 && score <= lvl.threshold) {
                result = lvl;
                break;
            }
        }
        return result;
    }

    public string GetTextForScore(int score) {
        return GetLevelForScore(score).text;
    }

    public float GetMultiplierForScore(int score) {
        return GetLevelForScore(score).multiplier;
    }
}

/// <summary>
/// Attach to your sentence prefab (with a TMP_Text) to display and absorb.
/// </summary>
public class SentenceDisplay : MonoBehaviour {
    [Tooltip("SO defining levels, text, and base absorption points.")]
    public SentenceDefinition definition;

    [Tooltip("TMP component for displaying sentence text.")]
    public TMP_Text sentenceText;

    /// <summary>
    /// Fires when this sentence absorbs; passes final absorption points.
    /// </summary>
    public event Action<int> onAbsorbed;

    int polarityScore = 0;
    string lastText;

    void Start() {
        UpdateText();
    }

    /// <summary>
    /// Call when a boid collides: positive or negative polarity effect.
    /// </summary>
    public void AddPolarityScore(int delta) {
        polarityScore += delta;
        UpdateText();
    }

    void UpdateText() {
        var text = definition.GetTextForScore(polarityScore);
        if (text != lastText) {
            sentenceText.text = text;
            lastText = text;
        }
    }

    /// <summary>
    /// Computes absorption points based on baseAbsorptionPoints and current level multiplier.
    /// </summary>
    public int CalculateAbsorptionPoints() {
        float mult = definition.GetMultiplierForScore(polarityScore);
        return Mathf.RoundToInt(definition.baseAbsorptionPoints * mult);
    }

    /// <summary>
    /// Call when sentence finishes: invokes onAbsorbed with computed points.
    /// </summary>
    public void Absorb() {
        int points = CalculateAbsorptionPoints();
        onAbsorbed?.Invoke(points);
    }
}
