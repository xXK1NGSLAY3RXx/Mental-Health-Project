using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SentenceManager))]
public class PhaseManager : MonoBehaviour
{
    [Tooltip("Your Flock instance for spawning boids")]
    public Flock flock;

    [Tooltip("Define each phase and its sentences here")]
    public PhaseDefinition[] phases;

    private SentenceManager sentenceMgr;
    private int currentPhase = -1;

    void Awake()
    {
        sentenceMgr = GetComponent<SentenceManager>();
    }

    void OnEnable()
    {
        sentenceMgr.onSentenceAbsorbed += OnSentenceAbsorbed;
    }

    void OnDisable()
    {
        sentenceMgr.onSentenceAbsorbed -= OnSentenceAbsorbed;
    }

    void Start()
    {
        NextPhase();
    }

    private void OnSentenceAbsorbed(SentenceInstance si)
    {
        // Defer phase change until next frame to avoid dictionary conflicts
        StartCoroutine(NextPhaseDelayed());
    }

    private IEnumerator NextPhaseDelayed()
    {
        yield return null;
        NextPhase();
    }

    private void NextPhase()
    {
        currentPhase++;
        if (currentPhase >= phases.Length)
        {
            Debug.Log("All phases complete!");
            // Clean up
            sentenceMgr.ResetPhase();
            flock.ClearAgents();
            enabled = false;
            return;
        }

        var def = phases[currentPhase];

        // Reset for new phase
        sentenceMgr.ResetPhase();
        sentenceMgr.AllowedSentences = def.sentences.ToList();

        // Build the word bag
        var bag = def.sentences
                     .SelectMany(sd => sd.wordsInOrder)
                     .Distinct()
                     .ToList();

        sentenceMgr.Words = bag;

        // Spawn one agent per word, assigning each its WordDefinition
        var polarities = bag.Select(wd => wd.Polarity).ToList();
        flock.SpawnAgentsFromPolarities(polarities, bag);

        Debug.Log($"Phase {currentPhase + 1} started with {bag.Count} words");
    }
}
