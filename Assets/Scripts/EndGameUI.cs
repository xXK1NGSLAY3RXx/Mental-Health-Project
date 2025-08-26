using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Post-level report presenter.
/// Shows:
///  • Stars earned (0–3)
///  • Absorbed sentence rows from a single anchor
///  • Tick in front of completed sentences; Cross in front of others
///  • Plays star-based dialogue, then shows Continue/Retry
/// Robust to subscribe even if GameManager appears later or EndLevel fired early.
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

    [Header("Sentence Rows (manual layout)")]
    [Tooltip("Anchor for the FIRST sentence row. New rows will be placed below this.")]
    public RectTransform firstRowAnchor;
    [Tooltip("Vertical spacing between rows in local units (pixels).")]
    public float rowSpacing = 40f;
    [Tooltip("TMP_Text prefab used for each sentence line (can include a background as a child).")]
    public TMP_Text sentenceTextPrefab;

    [Tooltip("Tick Image prefab for completed sentences.")]
    public Image checkPrefab;
    [Tooltip("Cross Image prefab for incomplete sentences.")]
    public Image crossPrefab;

    [Tooltip("Icon offset relative to each row's anchored position.")]
    public Vector2 iconOffset = new Vector2(-24f, 0f);

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueSequenceSO dialogue0Stars;
    public DialogueSequenceSO dialogue1Star;
    public DialogueSequenceSO dialogue2Stars;
    public DialogueSequenceSO dialogue3Stars;

    [Header("Buttons")]
    public GameObject buttonsRoot;        
    public Button continueButton;
    public Button retryButton;

    [Header("Scene Routing")]
    [Tooltip("If set, Continue loads this scene by name; else falls back to build index.")]
    public string nextSceneName;
    [Tooltip("Used if nextSceneName is empty.")]
    public int nextSceneBuildIndex = -1;

    // Keep track of spawned UI so we can clear/rebuild
    private readonly List<GameObject> _spawned = new();

    // Robust subscription / guard flags
    bool _subscribed = false;
    Coroutine _subscribeRoutine;
    bool _shown = false;

    void Awake()
    {
        // Only disable the panel if it is NOT this GameObject
        if (panel && panel != gameObject)
            panel.SetActive(false);

        if (buttonsRoot) buttonsRoot.SetActive(false);
    }

    void OnEnable()
    {
        // Subscribe even if GameManager spawns a few frames later
        _subscribeRoutine = StartCoroutine(SubscribeWhenGMReady());

        if (continueButton) continueButton.onClick.AddListener(OnContinue);
        if (retryButton)    retryButton.onClick.AddListener(OnRetry);
    }

    IEnumerator SubscribeWhenGMReady()
    {
        while (GameManager.Instance == null) yield return null;

        var gm = GameManager.Instance;

        if (!_subscribed)
        {
            gm.OnLevelEnded += HandleLevelEnded;
            _subscribed = true;
        }

        // If the level already ended before we subscribed, show immediately
        if (gm.LevelHasEnded)
            HandleLevelEnded();
    }

    void OnDisable()
    {
        if (_subscribeRoutine != null) StopCoroutine(_subscribeRoutine);

        if (_subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelEnded -= HandleLevelEnded;
            _subscribed = false;
        }

        if (continueButton) continueButton.onClick.RemoveListener(OnContinue);
        if (retryButton)    retryButton.onClick.RemoveListener(OnRetry);
    }

    private void HandleLevelEnded()
    {
        if (_shown) return; // build once
        _shown = true;

        if (panel) panel.SetActive(true);
        if (buttonsRoot) buttonsRoot.SetActive(false);

        // Stars
        int earned = GameManager.Instance.GetStarsEarned();
        PaintStars(earned);

        // Sentences (manual rows from a single anchor)
        PopulateSentencesManual(GameManager.Instance.GetAbsorbedSentences());

        // Dialogue
        var seq = PickDialogueFor(earned);
        if (dialogueManager != null && seq != null)
        {
            bool done = false;

            // NOTE: If your DialogueManager.StartDialogue has only (seq, onComplete),
            // change the next line to: dialogueManager.StartDialogue(seq, () => done = true);
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

    /// <summary>
    /// Spawns one TMP text per absorbed sentence, row-by-row from firstRowAnchor.
    /// Puts a tick icon for top rows, and a cross icon for non-top rows.
    /// </summary>
    private void PopulateSentencesManual(IReadOnlyList<GameManager.AbsorbedSentence> items)
    {
        if (!firstRowAnchor || sentenceTextPrefab == null)
        {
            Debug.LogWarning("EndgameUI: Missing firstRowAnchor or sentenceTextPrefab.");
            return;
        }

        // Clear prior spawns
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) Destroy(_spawned[i]);
        _spawned.Clear();

        if (items == null) return;

        var parent = firstRowAnchor.parent as RectTransform;
        Vector2 basePos = firstRowAnchor.anchoredPosition;

        for (int i = 0; i < items.Count; i++)
        {
            var data = items[i];

            // Create text (this instantiates the WHOLE prefab the TMP_Text is on;
            // put your background Image as a child of that prefab root so it moves together)
            TMP_Text txt = Instantiate(sentenceTextPrefab, parent);
            var textRT = txt.rectTransform;

            // Mirror the anchor/pivot of the reference
            textRT.anchorMin = firstRowAnchor.anchorMin;
            textRT.anchorMax = firstRowAnchor.anchorMax;
            textRT.pivot     = firstRowAnchor.pivot;
            textRT.anchoredPosition = basePos + new Vector2(0f, -i * rowSpacing);

            // Set text (escaped; no strikethrough)
            txt.text = EscapeForTMP(data.text);
            _spawned.Add(txt.gameObject);

            // Icon in front (tick or cross)
            Image iconPrefab = data.isTop ? checkPrefab : crossPrefab;
            if (iconPrefab != null)
            {
                Image icon = Instantiate(iconPrefab, parent);
                var iconRT = icon.rectTransform;
                iconRT.anchorMin = textRT.anchorMin;
                iconRT.anchorMax = textRT.anchorMax;
                iconRT.pivot     = textRT.pivot;
                iconRT.anchoredPosition = textRT.anchoredPosition + iconOffset;
                _spawned.Add(icon.gameObject);
            }
        }
    }

    // Escape TMP angle brackets in user text to avoid tag parsing
    private static string EscapeForTMP(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("<", "< ").Replace(">", " >");
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
