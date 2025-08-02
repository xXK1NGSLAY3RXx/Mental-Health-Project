using System;
using UnityEngine;

/// <summary>
/// Handles timer, trigger detection, polarity scoring, and absorption logic.
/// </summary>
public class StaticSentence : MonoBehaviour
{
    [Tooltip("Definition SO for text thresholds and multipliers.")]
    public StaticSentenceDefinition definition;


    /// <summary> Current accumulated polarity score from boid collisions. </summary>
    public int PolarityScore { get; private set; }

    private float timer;

    /// <summary> Fired whenever PolarityScore changes. </summary>
    public event Action<int> OnPolarityScoreChanged;

    /// <summary> Fired when the sentence absorbs, passing computed points. </summary>
    public event Action<int> OnAbsorbed;

    [HideInInspector]
    public bool reachedEnd = false;

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
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

        int delta = agent.polarityScore;
        if (agent.polarity == Polarity.Positive)
            PolarityScore += delta;
        else
            PolarityScore -= delta;

        OnPolarityScoreChanged?.Invoke(PolarityScore);

        // remove the agent
        agent.ParentFlock.RemoveAgent(agent);
    }

    private void DoAbsorption()
    {
        int points = CalculateAbsorptionPoints();
        OnAbsorbed?.Invoke(points);
        Destroy(gameObject);
    }

    /// <summary>
    /// Compute absorption based on baseAbsorptionPoints and current level multiplier.
    /// </summary>
    public int CalculateAbsorptionPoints()
    {
        float mult = definition.GetMultiplierForScore(PolarityScore);
        return Mathf.RoundToInt(definition.baseAbsorptionPoints * mult);
    }
    
    public void SetLifetime(float amount)
    {
        timer = amount;
       
    }
}




