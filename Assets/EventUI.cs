using UnityEngine;
using TMPro;

/// <summary>
/// Shows left/right dialog boxes and picks a random message from its options.
/// </summary>
public class EventUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Left dialog box GameObject")]
    public GameObject LeftBox;
    [Tooltip("Text component inside the left box")]
    public TMP_Text  LeftText;

    [Tooltip("Right dialog box GameObject")]
    public GameObject RightBox;
    [Tooltip("Text component inside the right box")]
    public TMP_Text  RightText;

    public SpriteRenderer art;

    [Header("Dialog Options")]
    [Tooltip("List of possible dialog strings. One is chosen at random on Init().")]
    public string[] dialogueOptions;

    /// <summary>
    /// Enables the appropriate box and sets its text from a random option.
    /// </summary>
    /// <param name="spawnLeft">True → show left box; false → show right box.</param>
    public void Init(bool spawnLeft)
    {
        LeftBox.SetActive(spawnLeft);
        RightBox.SetActive(!spawnLeft);

        // pick a random message…
        if (dialogueOptions != null && dialogueOptions.Length > 0)
        {
            string msg = dialogueOptions[Random.Range(0, dialogueOptions.Length)];
            if (spawnLeft && LeftText != null) LeftText.text = msg;
            if (!spawnLeft && RightText != null) RightText.text = msg;
        }

        if (art != null)
        {
            // reset to default (in case this UI object gets reused)
            art.flipX = false;
            // now flip only when spawning on the right
            if (!spawnLeft)
                art.flipX = true;
        }
    }


}