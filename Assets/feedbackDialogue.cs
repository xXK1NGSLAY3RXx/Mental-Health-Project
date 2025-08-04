// FeedbackDialogue.cs
using System;
using UnityEngine;

public class FeedbackDialogue : MonoBehaviour
{
    [Tooltip("Dialogue to play when player hits the highest threshold")]
    public DialogueSequenceSO goodFeedbackSequence;

    [Tooltip("Dialogue to play when player does NOT hit the highest threshold")]
    public DialogueSequenceSO hintFeedbackSequence;

    [Tooltip("Your scene's DialogueManager")]
    public DialogueManager dialogueManager;

    /// <summary>
    /// Plays the appropriate dialogue. 
    /// onComplete is invoked after the dialogue finishes.
    /// </summary>
    public void TriggerFeedback(bool isGood, Action onComplete = null)
    {
        if (dialogueManager == null)
        {
            Debug.LogError("FeedbackDialogue: missing DialogueManager reference.");
            onComplete?.Invoke();
            return;
        }

        var seq = isGood ? goodFeedbackSequence : hintFeedbackSequence;
        if (seq == null)
        {
            Debug.LogError($"FeedbackDialogue: {(isGood ? "good" : "hint")} sequence not assigned.");
            onComplete?.Invoke();
            return;
        }

        dialogueManager.StartDialogue(seq, () =>
        {
            onComplete?.Invoke();
        });
    }
}
