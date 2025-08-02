// SpeakerUI.cs
using UnityEngine;
using TMPro;

/// <summary>
/// Attach this to the root of your Speaker prefab. Drag-and-drop:
///   • LeftBox (GameObject)
///   • LeftText (TMP_Text)
///   • RightBox (GameObject)
///   • RightText (TMP_Text)
/// </summary>
public class SpeakerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The left dialog box GameObject.")]
    public GameObject LeftBox;

    [Tooltip("The TextMeshPro component inside the left box.")]
    public TMP_Text LeftText;

    [Tooltip("The right dialog box GameObject.")]
    public GameObject RightBox;

    [Tooltip("The TextMeshPro component inside the right box.")]
    public TMP_Text RightText;

    /// <summary>
    /// Initializes the speaker UI by toggling the correct box, setting its text, and destroying after lifetime.
    /// </summary>
    /// <param name="spawnLeft">True to show the left box; false to show the right box.</param>
    /// <param name="message">Dialog string to display.</param>
    /// <param name="lifetime">Seconds before automatic destruction.</param>
    public void Init(bool spawnLeft, string message, float lifetime)
    {
        LeftBox .SetActive(spawnLeft);
        RightBox.SetActive(!spawnLeft);

        if (spawnLeft && LeftText  != null) LeftText .text = message;
        if (!spawnLeft && RightText != null) RightText.text = message;

        Destroy(gameObject, lifetime);
    }
}
