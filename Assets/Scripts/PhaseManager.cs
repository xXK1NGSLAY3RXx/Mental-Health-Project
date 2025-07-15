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
        NextPhase();
    }

    private void NextPhase()
    {
        currentPhase++;
        if (currentPhase >= phases.Length)
        {
            Debug.Log("All phases complete!");
            return;
        }

        var def = phases[currentPhase];

        // 1) Reset everything in the SentenceManager
        sentenceMgr.ResetPhase();

        // 2) Build the unique bag of WordDefinitions
        var bag = new HashSet<WordDefinition>();
        foreach (var sd in def.sentences)
            foreach (var wd in sd.wordsInOrder)
                bag.Add(wd);

        // 3) Tell SentenceManager which words to use this phase
        sentenceMgr.Words = bag.ToList();

        // 4) Spawn exactly one boid per word in that bag
        var polarities = bag.Select(wd => wd.Polarity).ToList();
        flock.SpawnAgentsFromPolarities(polarities);

        Debug.Log($"Phase {currentPhase+1} started with {bag.Count} words");
    }
}
