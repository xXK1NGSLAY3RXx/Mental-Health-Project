using UnityEngine;
using UnityEngine.SceneManagement;

public class CutSceneDialogueStarter : MonoBehaviour
{
    [Tooltip("Your DialogueSequence SO")]
    public DialogueSequenceSO dialogueSequence;

    [Tooltip("Your scene's DialogueManager")]
    public DialogueManager dialogueManager;

    [Header("Scene Loading")]
    [Tooltip("Either specify a scene name to load, or leave empty to use 'nextSceneBuildIndex'")]
    public string nextSceneName;
    [Tooltip("Build index of the scene to load (used if 'nextSceneName' is empty)")]
    public int nextSceneBuildIndex = -1;

    void Start()
    {
        if (dialogueSequence == null || dialogueManager == null)
        {
            Debug.LogError("Assign Sequence SO and DialogueManager in Inspector.");
            return;
        }

        dialogueManager.StartDialogue(dialogueSequence, OnDialogueComplete, false);
    }

    private void OnDialogueComplete()
    {
        Debug.Log("Dialogue finished – loading next scene…");

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
        }
        else
        {
            Debug.LogWarning("No next scene specified! Please set 'nextSceneName' or 'nextSceneBuildIndex'.");
        }
    }
}
