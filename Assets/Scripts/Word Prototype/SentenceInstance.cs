using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SentenceInstance
{
    public string ClusterKey;                // the unique cluster identifier
    public List<FlockAgent> Members;         // current agents in the cluster
    public List<WordDefinition> Words;       // matched words
    public WordLabel Label;                  // the UI label showing the sentence
    public float LockTimer;                  // countdown until absorb
    public int SentenceScore;                // computed score

    const float DefaultLockTime = 3f;        // or whatever default you like

    public SentenceInstance(string clusterKey,
                            IEnumerable<FlockAgent> members,
                            List<WordDefinition> words)
    {
        ClusterKey = clusterKey;
        Members    = members.ToList();
        Words      = words;
        LockTimer  = DefaultLockTime;
        ComputeScore();
    }

    /// <summary>
    /// If the cluster’s membership changes, call this each frame
    /// so we keep our agent list up to date.
    /// </summary>
    public void UpdateMembers(IEnumerable<FlockAgent> members)
    {
        Members = members.ToList();
    }

    public void ComputeScore()
    {
        // Example: additive then multiplicative
        float total = 0f;
        // Additives
        foreach (var wd in Words.Where(w => w.operation == ScoreOperation.Additive))
            total += wd.additiveValue;
        // Multipliers
        foreach (var wd in Words.Where(w => w.operation == ScoreOperation.Multiplicative))
            total *= wd.multiplier;
        SentenceScore = Mathf.RoundToInt(total);
    }

    public string GetText()
    {
        // e.g. group by orderIndex, join with "and"
        return string.Join(" ",
            Words.GroupBy(w => w.orderIndex)
                 .OrderBy(g => g.Key)
                 .Select(g => g.Count() > 1
                     ? string.Join(" and ", g.Select(w => w.Text))
                     : g.First().Text
                 )
        );
    }

    public void InitializeLabel(WordLabel lbl)
    {
        Label = lbl;
        Label.SetText(GetText());
        Label.transform.position = GetCentroid();
        Label.gameObject.SetActive(true);
    }

    public void Tick(float dt)
    {
        LockTimer -= dt;
        // keep the label following the cluster
        if (Label != null)
            Label.transform.position = GetCentroid();
    }

    public bool IsExpired() => LockTimer <= 0f;

    Vector2 GetCentroid() =>
        Members.Aggregate(Vector2.zero, (sum, a) => sum + (Vector2)a.transform.position)
        / Members.Count;
}
