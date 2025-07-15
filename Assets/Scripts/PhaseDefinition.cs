using UnityEngine;

[CreateAssetMenu(menuName="Game/Phase Definition")]
public class PhaseDefinition : ScriptableObject
{
    [Tooltip("Which SentenceDefinitions belong to this phase")]
    public SentenceDefinition[] sentences;
}
