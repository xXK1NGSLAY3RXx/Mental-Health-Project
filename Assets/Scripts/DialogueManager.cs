using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Drives a DialogueSequenceSO through named UI slots, handling per-line tap/timer advance,
/// typewriter effect with looping sound, and proper skip behavior, including random selection
/// when multiple lines share the same order.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DialogueManager : MonoBehaviour
{
    [Tooltip("Configure one slot per DialogueEntry in your SO, matching by name.")]
    public DialogueUISlot[] uiSlots;

    [Tooltip("If true, the art sprite hides together with the box; if false, art stays visible when the box hides.")]
    public bool hideCharacterArt = true;

    // [Tooltip("If true, saves the current character art before dialogue and restores it after the sequence completes.")]
    // public bool revertCharacterArtOnComplete = false;

    [Header("Typewriter Settings")]
    [Tooltip("Seconds between each character reveal")]
    public float letterDelay = 0.05f;

    [Tooltip("Looping sound to play during the typewriter effect")]
    public AudioClip typeSound;

    private AudioSource _audioSource;
    private Sprite[] _savedArtSprites;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    /// <summary>
    /// Plays the full sequence, then calls onComplete.
    /// </summary>
    public void StartDialogue(DialogueSequenceSO seq, Action onComplete, bool revertArt)
    {
        // Save original character art sprites
        if (revertArt == true)
        {
            _savedArtSprites = new Sprite[uiSlots.Length];
            for (int i = 0; i < uiSlots.Length; i++)
            {
                if (uiSlots[i].artRenderer != null)
                    _savedArtSprites[i] = uiSlots[i].artRenderer.sprite;
            }
        }

        // Flatten all lines
        var flattened = new List<(string entryName, Sprite boxBg, DialogueLine line)>();
        foreach (var entry in seq.entries)
            foreach (var ln in entry.sentences)
                flattened.Add((entry.name, entry.dialogueBoxSprite, ln));

        // Group by order and pick one at random per order
        var chosen = flattened
            .GroupBy(x => x.line.order)
            .Select(g => g.OrderBy(_ => UnityEngine.Random.value).First())
            .ToList();

        // Sort by order
        chosen.Sort((a, b) => a.line.order.CompareTo(b.line.order));

        StartCoroutine(RunSequence(chosen, onComplete, revertArt));
    }

    private IEnumerator RunSequence(
        List<(string entryName, Sprite boxBg, DialogueLine line)> list,
        Action onComplete, bool revertArt)
    {
        // Hide all slots initially
        foreach (var slot in uiSlots)
            if (hideCharacterArt)
                slot.HideAll();
            else
                slot.HideBoxOnly();
        yield return null;

        foreach (var wrap in list)
        {
            // Find matching UI slot
            var slot = Array.Find(uiSlots, s => s.name == wrap.entryName);
            if (slot == null)
            {
                Debug.LogWarning($"No UISlot named '{wrap.entryName}'");
                continue;
            }

            // Show box + art, clear text
            slot.Show(wrap.boxBg, wrap.line.characterArt, string.Empty);

            // Eat leftover frame
            yield return null;

            // Typewriter + looping sound
            yield return TypeSentence(slot, wrap.line.text, wrap.line.skip);

            // After full sentence shown, wait timer or tap
            if (wrap.line.timer > 0f)
            {
                if (wrap.line.skip)
                    yield return WaitForEither(wrap.line.timer);
                else
                    yield return new WaitForSeconds(wrap.line.timer);
            }
            else
            {
                if (wrap.line.skip)
                    yield return WaitForTap();
                else
                    yield return new WaitUntil(() => false);
            }

            // Hide according to global setting
            if (hideCharacterArt)
                slot.HideAll();
            else
                slot.HideBoxOnly();
        }

        onComplete?.Invoke();

        // Restore original character art if needed
        if (revertArt && _savedArtSprites != null)
        {
            for (int i = 0; i < uiSlots.Length; i++)
            {
                if (uiSlots[i].artRenderer != null && _savedArtSprites[i] != null)
                    uiSlots[i].artRenderer.sprite = _savedArtSprites[i];
            }
        }
    }

    private IEnumerator TypeSentence(DialogueUISlot slot, string full, bool canSkip)
    {
        if (string.IsNullOrEmpty(full) || letterDelay <= 0f)
        {
            slot.textMesh.text = full;
            yield break;
        }

        

        if (typeSound != null)
        {
            _audioSource.clip = typeSound;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        for (int i = 1; i <= full.Length; i++)
        {
            slot.textMesh.text = full.Substring(0, i);
            float t = 0f;
            while (t < letterDelay)
            {
                if (canSkip && (Input.GetMouseButtonDown(0) ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
                {
                    slot.textMesh.text = full;
                    _audioSource.Stop();
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (_audioSource.isPlaying)
            _audioSource.Stop();
    }

    private IEnumerator WaitForEither(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (Input.GetMouseButtonDown(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForTap()
    {
        // Clear any held click/touch
        while (Input.GetMouseButton(0) || Input.touchCount > 0)
            yield return null;

        // Wait for fresh tap
        while (true)
        {
            if (Input.GetMouseButtonDown(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                break;
            yield return null;
        }
    }
}
