using UnityEngine;
using TMPro;

/// <summary>
/// Hook this up in the Inspector to each on‑screen dialogue slot:
///  • name must match DialogueEntry.name
///  • boxRoot is the parent GameObject for the box (enable/disable)
///  • boxRenderer draws the dialogue box
///  • artRenderer draws the portrait
///  • textMesh shows the line of text
/// </summary>
[System.Serializable]
public class DialogueUISlot
{
    [Tooltip("Must match DialogueEntry.name")]
    public string         name;

    [Tooltip("Root GameObject for this speaker's UI")]
    public GameObject     boxRoot;

    [Tooltip("SpriteRenderer for the box background")]
    public SpriteRenderer boxRenderer;

    [Tooltip("SpriteRenderer for the character portrait")]
    public SpriteRenderer artRenderer;

    [Tooltip("TextMeshPro for the dialogue text")]
    public TextMeshPro    textMesh;

    /// <summary>
    /// Show the full box + art + text.
    /// </summary>
    public void Show(Sprite boxBg, Sprite art, string msg)
    {
        if (boxRoot)       boxRoot.SetActive(true);
        if (boxRenderer)   boxRenderer.sprite = boxBg;
        if (artRenderer)   artRenderer.sprite   = art;
        if (textMesh)      textMesh.text        = msg;

        if (boxRenderer)   boxRenderer.enabled = true;
        if (artRenderer)   artRenderer.enabled = true;
        if (textMesh)      textMesh.enabled    = true;
    }

    /// <summary>
    /// Hide only the dialogue box and text, leave the art up.
    /// </summary>
    public void HideBoxOnly()
    {
        if (boxRenderer)   boxRenderer.enabled = false;
        if (textMesh)      textMesh.enabled    = false;
    }

    /// <summary>
    /// Hide both box+text and art.
    /// </summary>
    public void HideAll()
    {
        HideBoxOnly();
        if (artRenderer) artRenderer.enabled = false;
    }
}
