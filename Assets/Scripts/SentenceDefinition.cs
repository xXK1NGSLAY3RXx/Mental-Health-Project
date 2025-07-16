using System.Collections.Generic;
using System.Linq;
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
    // Gather exactly which WordDefinitions are in this cluster
    var assignedWords = cluster.Members
        .Select(a => a.AssignedWord)
        .Where(wd => wd != null)
        .ToList();

    // Count occurrences of each WordDefinition
    var counts = assignedWords
        .GroupBy(wd => wd)
        .ToDictionary(g => g.Key, g => g.Count());

    // Now ensure *each* word in your template is really present
    foreach (var wd in wordsInOrder)
    {
        if (!counts.TryGetValue(wd, out int have) || have < wd.RequiredCount)
            return false;
    }
    return true;
}
}
