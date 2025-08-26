using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AbsorbedSentenceDisplay : MonoBehaviour
{
  [Tooltip("Drag in your slot GameObjects here (each should contain an Image + a TMP_Text child)")]
    public List<GameObject> slots;

    /// <summary>
    /// Clears and hides all slots.
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetActive(false);
        }
    }

    /// <summary>
    /// Finds the next empty slot, activates it, and sets its text.
    /// </summary>
    public void AddSentence(string sentence)
    {
        foreach (var slot in slots)
        {
            if (!slot.activeSelf)
            {
                // Activate the whole panel
                slot.SetActive(true);

                // Find its TMP_Text child 
                var tmp = slot.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                    tmp.text = sentence;
                else
                    Debug.LogWarning($"Slot \"{slot.name}\" has no TMP_Text child!");

                return;
            }
        }

        Debug.LogWarning($"All {slots.Count} HUD slots are already in use. Sentence dropped:\n{sentence}");
    }
}

