// TestDialogueStarter.cs
using UnityEngine;

public class TestDialogueStarter : MonoBehaviour
{
    [Tooltip("Your DialogueSequence SO")]
    public DialogueSequenceSO dialogueSequence;

    [Tooltip("Your scene's DialogueManager")]
    public DialogueManager    dialogueManager;

    void Start()
    {
        if (dialogueSequence == null || dialogueManager == null)
        {
            Debug.LogError("Assign Sequence SO and DialogueManager in Inspector.");
            return;
        }

        dialogueManager.StartDialogue(dialogueSequence, () =>
        {
            Debug.Log("Dialogue finished – now start your level!");
            // e.g. GameFlowManager.Instance.EnterLevel();
        });
    }
}
