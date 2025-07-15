using UnityEngine;

public enum Polarity { Positive, Neutral, Negative }
public enum ScoreOperation
{
    Additive,       // word adds a fixed value
    Multiplicative  // word multiplies the running total
}

[CreateAssetMenu(menuName = "Words/Word Definition")]
public class WordDefinition : ScriptableObject
{
    [Tooltip("The text to display (one word or phrase)")]
    public string Text;

    [Tooltip("Which boid polarity this word is built from")]
    public Polarity Polarity;

    [Tooltip("Exact number of boids of that polarity required")]
    public int RequiredCount;

    [Tooltip("Lower indices come earlier in the formed sentence")]
    public int orderIndex;

    public int         additiveValue;    // used if operation == Additive
    public ScoreOperation operation;     // Additive or Multiplicative
    public float       multiplier = 1f;  // used if operation == Multiplicative
}
