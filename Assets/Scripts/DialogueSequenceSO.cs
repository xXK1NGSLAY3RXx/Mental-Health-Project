// DialogueSequenceSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence")]
public class DialogueSequenceSO : ScriptableObject
{
    [Tooltip("Each speaking character. Name must match a UISlot in the scene.")]
    public DialogueEntry[] entries;
}

[System.Serializable]
public class DialogueEntry
{
    [Tooltip("Unique ID, used to match with a DialogueUISlot in the scene.")]
    public string        name;

    [Tooltip("Shared background sprite for this character's dialogue box.")]
    public Sprite        dialogueBoxSprite;

    [Tooltip("All lines this character will speak.")]
    public DialogueLine[] sentences;
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Global order index. Lines from all entries are merged & sorted by this.")]
    public int     order;

    [Tooltip("What they actually say.")]
    [TextArea]
    public string  text;

    [Tooltip("Portrait to show for *this* line.")]
    public Sprite  characterArt;

    [Tooltip("If true, a tap will skip immediately; if false, taps are ignored.")]
    public bool    skip = true;

    [Tooltip("If >0, auto‑advance after this many seconds.")]
    public float   timer = 0f;
}
