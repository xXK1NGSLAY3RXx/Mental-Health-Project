using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ClusterManager))]
public class SentenceManager : MonoBehaviour
{
    [Header("Label Prefabs")]
    public WordLabel wordLabelPrefab;
    public WordLabel sentenceLabelPrefab;            // Prefab with SentenceLabelUI attached
    [Header("Undefined-Sentence Prefab")]
    public WordLabel undefinedSentenceLabelPrefab;

    [Tooltip("Parent transform under which labels are spawned")]
    public Transform labelsParent;

    [Header("Gameplay")]
    public List<WordDefinition> Words;
    public int stableFrameThreshold = 2;
    public int totalScore;

    [HideInInspector]
    public List<SentenceDefinition> AllowedSentences = new List<SentenceDefinition>();

    public event Action<SentenceInstance> onSentenceAbsorbed;

    private ClusterManager clusterMgr;
    private Dictionary<string, int> stability = new Dictionary<string, int>();
    private Dictionary<string, WordLabel> activeWordLabels = new Dictionary<string, WordLabel>();
    private Dictionary<string, SentenceInstance> activeSentences = new Dictionary<string, SentenceInstance>();
    private Dictionary<string, SentenceInstance> activeUndefined = new Dictionary<string, SentenceInstance>();

    // Pools per prefab to avoid mixing label types
    private Dictionary<WordLabel, List<WordLabel>> pools = new Dictionary<WordLabel, List<WordLabel>>();

    void Awake()
    {
        clusterMgr = GetComponent<ClusterManager>();
        if (labelsParent == null)
            labelsParent = transform;
    }

    /// <summary>
    /// Clears all labels and timers for a new phase.
    /// </summary>
    public void ResetPhase()
    {
        stability.Clear();
        foreach (var kv in activeWordLabels)
            kv.Value.gameObject.SetActive(false);
        activeWordLabels.Clear();

        foreach (var kv in activeSentences)
            kv.Value.Label.gameObject.SetActive(false);
        activeSentences.Clear();

        foreach (var kv in activeUndefined)
            kv.Value.Label.gameObject.SetActive(false);
        activeUndefined.Clear();
    }

    void Update()
    {
        var clusters = clusterMgr.BuildClusters();
        var seenKeys = new HashSet<string>();

        // 1) Track cluster stability
        foreach (var c in clusters)
        {
            var key = GetKey(c);
            seenKeys.Add(key);
            stability[key] = stability.TryGetValue(key, out int v) ? v + 1 : 1;
        }

        // 2) Remove vanished clusters
        foreach (var old in stability.Keys.Except(seenKeys).ToList())
        {
            stability.Remove(old);
            if (activeWordLabels.Remove(old, out var wlbl))
                wlbl.gameObject.SetActive(false);
            if (activeSentences.Remove(old, out var sinst))
                sinst.Label.gameObject.SetActive(false);
            if (activeUndefined.Remove(old, out var uinst))
                uinst.Label.gameObject.SetActive(false);
        }

        // 3) Process each stable cluster
        foreach (var c in clusters)
        {
            var key = GetKey(c);
            if (stability[key] < stableFrameThreshold)
                continue;

            // A) Defined sentence detection (longest first)
            var matchedDefs = AllowedSentences
                .Where(sd => sd.MatchesCluster(c))
                .OrderByDescending(sd => sd.wordsInOrder.Length)
                .ToList();

            if (matchedDefs.Count > 0)
            {
                // Hide other labels
                if (activeWordLabels.Remove(key, out var wold))
                    wold.gameObject.SetActive(false);
                if (activeUndefined.Remove(key, out var uold))
                    uold.Label.gameObject.SetActive(false);

                var sd = matchedDefs[0];
                if (!activeSentences.TryGetValue(key, out var si))
                {
                    // Create new sentence instance
                    si = new SentenceInstance(key, c.Members, sd.wordsInOrder.ToList());
                    si.LockTimer = sd.lockTime;
                    si.ComputeScore();

                    // Instantiate label and initialize progress UI
                    var lbl = GetLabelFromPool(sentenceLabelPrefab);
                    var ui  = lbl.GetComponent<SentenceLabelUI>();
                    if (ui != null)
                        ui.Initialize(si.GetText(), sd.lockTime);

                    si.InitializeLabel(lbl);
                    activeSentences[key] = si;
                }
                else
                {
                    // Update existing
                    si.UpdateMembers(c.Members);
                    si.Tick(Time.deltaTime);

                    // Update progress bar
                    if (si.Label != null)
                    {
                        var ui = si.Label.GetComponent<SentenceLabelUI>();
                        if (ui != null)
                            ui.UpdateProgress(si.LockTimer);
                    }

                    // Check expiration
                    if (si.IsExpired())
                    {
                        foreach (var a in si.Members)
                            a.ParentFlock.RemoveAgent(a);
                        si.Label?.gameObject.SetActive(false);
                        totalScore += si.SentenceScore;
                        onSentenceAbsorbed?.Invoke(si);
                        activeSentences.Remove(key);
                    }
                }
            }
            else
            {
                // B) Single-word or undefined clusters
                var assignedWords = c.Members
                    .Select(a => a.AssignedWord)
                    .Where(wd => wd != null)
                    .OrderBy(wd => wd.orderIndex)
                    .ToList();

                if (assignedWords.Count == 1)
                {
                    // Show only that word
                    if (activeSentences.Remove(key, out var rs))
                        rs.Label.gameObject.SetActive(false);
                    if (activeUndefined.Remove(key, out var ud))
                        ud.Label.gameObject.SetActive(false);

                    if (!activeWordLabels.TryGetValue(key, out var wlbl))
                    {
                        wlbl = GetLabelFromPool(wordLabelPrefab);
                        activeWordLabels[key] = wlbl;
                    }
                    wlbl.SetText(assignedWords[0].Text);
                    wlbl.transform.position = c.Centroid;
                    wlbl.gameObject.SetActive(true);
                }
                else if (assignedWords.Count >= 2)
                {
                    // Show undefined sentence
                    if (activeWordLabels.Remove(key, out var wold2))
                        wold2.gameObject.SetActive(false);
                    if (activeSentences.Remove(key, out var sold))
                        sold.Label.gameObject.SetActive(false);

                    if (!activeUndefined.TryGetValue(key, out var uiInst))
                    {
                        uiInst = new SentenceInstance(key, c.Members, assignedWords);
                        var lbl = GetLabelFromPool(undefinedSentenceLabelPrefab);
                        uiInst.InitializeLabel(lbl);
                        activeUndefined[key] = uiInst;
                    }
                    else
                    {
                        uiInst.UpdateMembers(c.Members);
                        uiInst.Label.transform.position = c.Centroid;
                    }
                }
                else
                {
                    // Hide all if no words
                    if (activeWordLabels.Remove(key, out var w))
                        w.gameObject.SetActive(false);
                    if (activeSentences.Remove(key, out var s))
                        s.Label.gameObject.SetActive(false);
                    if (activeUndefined.Remove(key, out var u))
                        u.Label.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Retrieves an inactive instance of the specified prefab, or instantiates one if none are available.
    /// </summary>
    private WordLabel GetLabelFromPool(WordLabel prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new List<WordLabel>();
            pools[prefab] = pool;
        }
        foreach (var lbl in pool)
            if (!lbl.gameObject.activeSelf)
                return lbl;

        var nl = Instantiate(prefab, labelsParent);
        nl.gameObject.SetActive(false);
        pool.Add(nl);
        return nl;
    }

    private string GetKey(Cluster c)
        => string.Join(",", c.Members.Select(a => a.GetInstanceID()).OrderBy(id => id));
}
