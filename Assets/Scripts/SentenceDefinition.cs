using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Words/Sentence Definition")]
public class SentenceDefinition : ScriptableObject
{
    [Tooltip("The words, in reading order, that make up this sentence")]
    public WordDefinition[] wordsInOrder;

    [Tooltip("Base score for this sentence (before any multipliers)")]
    public int baseScore = 0;

    [Tooltip("Lock time (seconds) before this sentence absorbs")]
    public float lockTime = 3f;

    /// <summary>
    /// Returns true if the given cluster has at least the required boids
    /// for every word in this template (by Polarity & RequiredCount).
    /// </summary>
    public bool MatchesCluster(Cluster cluster)
    {
        // tally boids by polarity
        var counts = new Dictionary<Polarity,int>();
        foreach (var agent in cluster.Members)
        {
            if (!counts.ContainsKey(agent.polarity)) counts[agent.polarity] = 0;
            counts[agent.polarity]++;
        }

        // ensure every word in this template can be formed
        foreach (var wd in wordsInOrder)
        {
            if (!counts.TryGetValue(wd.Polarity, out int c) || c < wd.RequiredCount)
                return false;
        }
        return true;
    }
}
