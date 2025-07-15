using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ClusterManager))]
public class SentenceManager : MonoBehaviour
{
    [Header("Label Prefabs")]
    public WordLabel wordLabelPrefab;
    public WordLabel sentenceLabelPrefab;

    [Tooltip("Parent transform under which labels are spawned")]
    public Transform labelsParent;

    [Header("Gameplay")]
    [Tooltip("All word definitions (the 'bag' for the current phase)")]
    public List<WordDefinition> Words;

    [Tooltip("How many frames a cluster must persist before showing anything")]
    public int stableFrameThreshold = 2;

    [Tooltip("Accumulated player score")]
    public int totalScore;

    // Event for PhaseManager to advance when a sentence absorbs
    public event Action<SentenceInstance> onSentenceAbsorbed;

    ClusterManager clusterMgr;
    Dictionary<string,int> stability       = new();
    Dictionary<string,WordLabel> activeWordLabels     = new();
    Dictionary<string,SentenceInstance> activeSentences = new();
    List<WordLabel> labelPool                = new();

    void Awake()
    {
        clusterMgr = GetComponent<ClusterManager>();
        if (labelsParent == null) labelsParent = transform;
    }

    /// <summary>
    /// Clears all active labels & timers, ready for a new phase’s Words.
    /// </summary>
    public void ResetPhase()
    {
        stability.Clear();
        foreach (var kv in activeWordLabels)  kv.Value.gameObject.SetActive(false);
        activeWordLabels.Clear();
        foreach (var kv in activeSentences)   kv.Value.Label.gameObject.SetActive(false);
        activeSentences.Clear();
    }

    void Update()
    {
        var clusters = clusterMgr.BuildClusters();
        var seenKeys = new HashSet<string>();

        // Update stability counts
        foreach (var c in clusters)
        {
            var key = GetKey(c);
            seenKeys.Add(key);
            stability[key] = stability.TryGetValue(key, out var v) ? v + 1 : 1;
        }

        // Remove vanished clusters
        foreach (var old in stability.Keys.Except(seenKeys).ToList())
        {
            stability.Remove(old);
            if (activeWordLabels.TryGetValue(old, out var wlbl))
            {
                wlbl.gameObject.SetActive(false);
                activeWordLabels.Remove(old);
            }
            if (activeSentences.TryGetValue(old, out var sinst))
            {
                sinst.Label.gameObject.SetActive(false);
                activeSentences.Remove(old);
            }
        }

        // For each stable cluster decide word vs sentence vs none
        foreach (var c in clusters)
        {
            var key = GetKey(c);
            if (stability[key] < stableFrameThreshold) 
                continue;

            // Tally boids by polarity
            var counts = c.Members
                .GroupBy(a => a.polarity)
                .ToDictionary(g => g.Key, g => g.Count());

            // Which WordDefinitions match?
            var matches = Words
                .Where(w => counts.TryGetValue(w.Polarity, out int cnt) && cnt >= w.RequiredCount)
                .OrderBy(w => w.orderIndex)
                .ToList();

            // 2+ words → sentence
            if (matches.Count >= 2)
            {
                // hide any lone-word label
                if (activeWordLabels.TryGetValue(key, out var solo))
                {
                    solo.gameObject.SetActive(false);
                    activeWordLabels.Remove(key);
                }

                // existing or new sentence instance
                if (!activeSentences.TryGetValue(key, out var si))
                {
                    si = new SentenceInstance(key, c.Members, matches);
                    var lbl = GetLabelFromPool(sentenceLabelPrefab);
                    si.InitializeLabel(lbl);
                    activeSentences[key] = si;
                }
                else
                {
                    si.UpdateMembers(c.Members);
                    si.Tick(Time.deltaTime);

                    if (si.IsExpired())
                    {
                        foreach (var a in si.Members)
                            a.ParentFlock.RemoveAgent(a);
                        si.Label.gameObject.SetActive(false);
                        totalScore += si.SentenceScore;
                        onSentenceAbsorbed?.Invoke(si);
                        activeSentences.Remove(key);
                    }
                }
            }
            // Exactly 1 word → lone‐word label
            else if (matches.Count == 1)
            {
                // cancel any sentence
                if (activeSentences.TryGetValue(key, out var sinst))
                {
                    sinst.Label.gameObject.SetActive(false);
                    activeSentences.Remove(key);
                }

                if (!activeWordLabels.TryGetValue(key, out var wlbl2))
                {
                    wlbl2 = GetLabelFromPool(wordLabelPrefab);
                    activeWordLabels[key] = wlbl2;
                }
                wlbl2.SetText(matches[0].Text);
                wlbl2.transform.position = c.Centroid;
                wlbl2.gameObject.SetActive(true);
            }
            // no words → hide both
            else
            {
                if (activeWordLabels.TryGetValue(key, out var wlbl3))
                {
                    wlbl3.gameObject.SetActive(false);
                    activeWordLabels.Remove(key);
                }
                if (activeSentences.TryGetValue(key, out var si3))
                {
                    si3.Label.gameObject.SetActive(false);
                    activeSentences.Remove(key);
                }
            }
        }
    }

    private WordLabel GetLabelFromPool(WordLabel prefab)
    {
        foreach (var l in labelPool)
            if (!l.gameObject.activeSelf)
                return l;
        var nl = Instantiate(prefab, labelsParent);
        nl.gameObject.SetActive(false);
        labelPool.Add(nl);
        return nl;
    }

    private string GetKey(Cluster c) =>
        string.Join(",", c.Members.Select(a => a.GetInstanceID()).OrderBy(id => id));
}
