using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Sentence/Sentence Definition")]
public class SentenceDefinition : ScriptableObject
{
    [Tooltip("Base absorption points awarded when this sentence absorbs.")]
    public int baseAbsorptionPoints = 0;

    [Tooltip("Lifetime in seconds before absorption.")]
    public float lifetime = 5f;

    [Serializable]
    public class Level
    {
        [Tooltip("Score threshold for this level (>= 0).")]
        public int threshold;
        [Tooltip("Text to display at this level.")]
        public string text;
        [Tooltip("Multiplier applied to baseAbsorptionPoints when absorbing at this level.")]
        public float multiplier = 1f;
    }

    [Tooltip("Levels in DESCENDING threshold order (highest first). MUST include a level with threshold==0.")]
    public List<Level> levels = new List<Level> { new Level { threshold = 0, text = "Default", multiplier = 1f } };

#if UNITY_EDITOR
    void OnValidate()
    {
        if (levels == null || levels.Count == 0)
            levels = new List<Level> { new Level { threshold = 0, text = "Default", multiplier = 1f } };

        bool hasZero = false;
        var seen = new HashSet<int>();

        for (int i = 0; i < levels.Count; i++)
        {
            var lvl = levels[i];

            if (lvl.threshold < 0)
                Debug.LogError($"{name}: thresholds must be >= 0 (found {lvl.threshold} at index {i})", this);

            if (lvl.threshold == 0) hasZero = true;

            if (!seen.Add(lvl.threshold))
                Debug.LogError($"{name}: duplicate threshold {lvl.threshold} at index {i}", this);

            if (i > 0 && levels[i - 1].threshold < lvl.threshold)
                Debug.LogError($"{name}: levels must be in DESCENDING order: idx {i-1}({levels[i-1].threshold}) >= idx {i}({lvl.threshold})", this);
        }

        if (!hasZero)
            Debug.LogError($"{name}: must have a level with threshold == 0", this);
    }
#endif

    public Level GetLevelForScore(int score)
    {
        if (levels == null || levels.Count == 0)
            return new Level { threshold = 0, text = "Default", multiplier = 1f };

        score = Mathf.Max(0, score); // no negatives

        // pick first (highest) whose threshold <= score
        for (int i = 0; i < levels.Count; i++)
            if (score >= levels[i].threshold)
                return levels[i];

        // fallback: find the 0-threshold entry
        for (int i = levels.Count - 1; i >= 0; i--)
            if (levels[i].threshold == 0) return levels[i];

        return levels[levels.Count - 1];
    }

    public string GetTextForScore(int score)        => GetLevelForScore(Mathf.Max(0, score)).text;
    public float  GetMultiplierForScore(int score)  => GetLevelForScore(Mathf.Max(0, score)).multiplier;
}
