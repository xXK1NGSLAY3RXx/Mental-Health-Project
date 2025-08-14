using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Post-level report presenter.
/// Listens to GameManager.EndLevel and shows:
///  • Stars earned (0–3)
///  • Absorbed sentence texts (collected by GameManager.RecordAbsorbedSentenceVersion)
///  • Plays a dialogue variant based on star count (0/1/2/3)
///  • Continue + Retry buttons
/// </summary>
public class EndgameUI : MonoBehaviour
{
    [Header("Root Panel")]
    [Tooltip("Enable when the level ends.")]
    public GameObject panel;

    [Header("Stars")]
    [Tooltip("Exactly 3 star Image slots in UI order left→right.")]
    public Image[] starImages = new Image[3];
    public Sprite starEmptySprite;
    public Sprite starFilledSprite;

    [Header("Absorbed Sentences List")]
    [Tooltip("Parent with VerticalLayoutGroup to hold rows.")]
    public RectTransform sentencesContainer;
    [Tooltip("Prefab TMP_Text for one row of absorbed text.")]
    public TMP_Text sentenceRowPrefab;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueSequenceSO dialogue0Stars;
    public DialogueSequenceSO dialogue1Star;
    public DialogueSequenceSO dialogue2Stars;
    public DialogueSequenceSO dialogue3Stars;

    [Header("Buttons")]
    public GameObject buttonsRoot;        // parent of Continue/Retry, hidden until dialogue finishes
    public Button continueButton;
    public Button retryButton;

    [Header("Scene Routing")] 
    [Tooltip("If set, Continue loads this scene by name; else falls back to build index.")]
    public string nextSceneName;
    [Tooltip("Used if nextSceneName is empty.")]
    public int nextSceneBuildIndex = -1;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (buttonsRoot) buttonsRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelEnded += HandleLevelEnded;

        if (continueButton) continueButton.onClick.AddListener(OnContinue);
        if (retryButton)    retryButton.onClick.AddListener(OnRetry);
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelEnded -= HandleLevelEnded;

        if (continueButton) continueButton.onClick.RemoveListener(OnContinue);
        if (retryButton)    retryButton.onClick.RemoveListener(OnRetry);
    }

    private void HandleLevelEnded()
    {
        if (panel) panel.SetActive(true);
        if (buttonsRoot) buttonsRoot.SetActive(false);

        // Stars
        int earned = GameManager.Instance.GetStarsEarned();
        PaintStars(earned);

        // Sentences
        PopulateSentences(GameManager.Instance.GetAbsorbedSentenceVersions());

        // Dialogue
        var seq = PickDialogueFor(earned);
        if (dialogueManager != null && seq != null)
        {
            bool done = false;
            dialogueManager.StartDialogue(seq, () => done = true, false);
            StartCoroutine(WaitThenShowButtons(doneFlag: () => done));
        }
        else
        {
            if (buttonsRoot) buttonsRoot.SetActive(true);
        }
    }

    private IEnumerator WaitThenShowButtons(System.Func<bool> doneFlag)
    {
        // wait until dialogue manager reports completion
        yield return new WaitUntil(() => doneFlag());
        if (buttonsRoot) buttonsRoot.SetActive(true);
    }

    private DialogueSequenceSO PickDialogueFor(int stars)
    {
        return stars switch
        {
            0 => dialogue0Stars,
            1 => dialogue1Star,
            2 => dialogue2Stars,
            _ => dialogue3Stars,
        };
    }

    private void PaintStars(int earned)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            var img = starImages[i];
            if (!img) continue;
            bool on = i < earned;
            if (on && starFilledSprite) img.sprite = starFilledSprite;
            else if (!on && starEmptySprite) img.sprite = starEmptySprite;
            img.enabled = true;
        }
    }

    private void PopulateSentences(IReadOnlyList<string> lines)
    {
        if (!sentencesContainer || sentenceRowPrefab == null) return;

        // clear previous
        for (int i = sentencesContainer.childCount - 1; i >= 0; i--)
            Destroy(sentencesContainer.GetChild(i).gameObject);

        if (lines == null) return;
        foreach (var text in lines)
        {
            var row = Instantiate(sentenceRowPrefab, sentencesContainer);
            row.text = text;
        }
    }

    // --- Buttons ---
    public void OnContinue()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else if (nextSceneBuildIndex >= 0)
            SceneManager.LoadScene(nextSceneBuildIndex);
        else
            Debug.LogWarning("EndgameUI.OnContinue: No next scene configured.");
    }

    public void OnRetry()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
