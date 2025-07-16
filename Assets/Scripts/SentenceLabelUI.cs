using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class SentenceLabelUI : MonoBehaviour
{
    [Tooltip("Drag the child TMP_Text here")]
    public TMP_Text uiText;

    [Tooltip("Drag the child Image (Filled) here")]
    public Image progressBar;

    private float _initialLockTime;

    /// <summary>
    /// Call once when the sentence label is created.
    /// </summary>
    public void Initialize(string text, float lockTime)
    {
        uiText.text = text;
        _initialLockTime = lockTime;
        if (progressBar != null)
        {
            progressBar.type        = Image.Type.Filled;
            progressBar.fillMethod  = Image.FillMethod.Horizontal;
            progressBar.fillOrigin  = (int)Image.OriginHorizontal.Left;
            progressBar.fillAmount  = 0f; // start empty
        }
    }

    /// <summary>
    /// Call each frame to update the fill amount.
    /// </summary>
    public void UpdateProgress(float remaining)
    {
        if (progressBar == null || _initialLockTime <= 0f)
            return;
        // Fill from 0 (start) to 1 (complete) as remaining time decreases
        float filled = 1f - Mathf.Clamp01(remaining / _initialLockTime);
        progressBar.fillAmount = filled;
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
