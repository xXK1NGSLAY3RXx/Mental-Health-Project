// StaticSentence.cs
using System;
using UnityEngine;

public class Sentence : MonoBehaviour
{
    [Tooltip("Definition SO for text thresholds and multipliers.")]
    public SentenceDefinition definition;

    public int PolarityScore { get; private set; }
    
    [HideInInspector]
    public float timer { get; private set; }

    public event Action<int> OnPolarityScoreChanged;
    public event Action<int> OnAbsorbed;

    [HideInInspector]
    public bool reachedEnd = false;

    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode  = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
    }

    void Update()
    {
        if (reachedEnd)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                DoAbsorption();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var agent = other.GetComponent<FlockAgent>();
        if (agent == null) return;

        // 1) Update raw polarity
        int delta = agent.polarityScore;
        PolarityScore += (agent.polarity == Polarity.Positive) ? delta : -delta;

        // 2) Clamp within [minThreshold, maxThreshold]
        var levels       = definition.levels;
        int maxThreshold = levels[0].threshold;
        int minThreshold = levels[levels.Count - 1].threshold;
        PolarityScore    = Mathf.Clamp(PolarityScore, minThreshold, maxThreshold);

        // 3) Notify any UI of the new (clamped) score
        OnPolarityScoreChanged?.Invoke(PolarityScore);

        // 4) Remove the boid
        agent.ParentFlock.RemoveAgent(agent);

        // 5) If we’ve reached the top threshold, absorb immediately
        if (PolarityScore >= maxThreshold)
            DoAbsorption();
    }

    private void DoAbsorption()
    {
        int points = CalculateAbsorptionPoints();
        OnAbsorbed?.Invoke(points);
        Destroy(gameObject);
    }

    public int CalculateAbsorptionPoints()
    {
        float m = definition.GetMultiplierForScore(PolarityScore);
        return Mathf.RoundToInt(definition.baseAbsorptionPoints * m);
    }

    public void SetLifetime(float amount)
    {
        timer = amount;
    }
}
