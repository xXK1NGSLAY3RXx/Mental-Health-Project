using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBarHUD : MonoBehaviour
{
    [Header("Placement & Fill")]
    [Tooltip("Rect whose width is used for star positioning (should also be the parent of the star Images). If empty, uses fillImage's RectTransform.")]
    public RectTransform placementRect;
    [Tooltip("Bar Image to animate. Must be Image(Type=Filled, Horizontal, Origin=Left).")]
    public Image fillImage;

    [Header("Stars (3 Images under placementRect)")]
    public Image[] starAnchors = new Image[3];
    public Sprite  starEmptySprite;
    public Sprite  starFilledSprite;

    [Header("Animation")]
    [Range(0.05f, 3f)] public float fillDuration = 0.6f;

    [Header("Audio")]
    [Tooltip("AudioSource used to play star sounds (2D). If empty, one will be added here.")]
    public AudioSource audioSource;
    [Tooltip("Optional per-star clips (index 0..2). Overrides the single clip for that star if set).")]
    public AudioClip[] perStarClips = new AudioClip[3];
    

    private Coroutine _fillRoutine;
    private float[] _starNorms;       // thresholds normalized 0..1
    private bool[]  _starEarned;      // tracks earned state to detect first crossing

    void Awake()
    {
        if (fillImage)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI sound
        }
    }

    void OnEnable()  { StartCoroutine(InitWhenReady()); }
    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged -= HandleScoreChanged;

        if (_fillRoutine != null) StopCoroutine(_fillRoutine);
    }

    IEnumerator InitWhenReady()
    {
        while (GameManager.Instance == null) yield return null;
        yield return new WaitForEndOfFrame(); // let layout settle

        var gm = GameManager.Instance;
        if (placementRect == null && fillImage != null)
            placementRect = fillImage.rectTransform;

        CacheNormalizedThresholds(gm.maxScore, gm.GetStarThresholds());
        PositionStars(gm.maxScore, gm.GetStarThresholds());

        // initial state
        float initialFill = gm.maxScore > 0 ? Mathf.Clamp01(gm.CurrentScore / (float)gm.maxScore) : 0f;
        if (fillImage) fillImage.fillAmount = initialFill;
        UpdateStarsByFill(initialFill, firstTime:true);

        gm.OnScoreChanged += HandleScoreChanged;
    }

    void CacheNormalizedThresholds(int max, int[] thresholds)
    {
        if (thresholds == null) { _starNorms = new float[0]; _starEarned = new bool[0]; return; }
        _starNorms = new float[thresholds.Length];
        _starEarned = new bool[thresholds.Length];
        for (int i = 0; i < thresholds.Length; i++)
        {
            _starNorms[i] = max > 0 ? Mathf.Clamp01(thresholds[i] / (float)max) : 0f;
            _starEarned[i] = false;
        }
    }

    void HandleScoreChanged(int current, int max)
    {
        float target = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
        if (_fillRoutine != null) StopCoroutine(_fillRoutine);
        _fillRoutine = StartCoroutine(AnimateFillTo(target, fillDuration));
    }

    IEnumerator AnimateFillTo(float target, float duration)
    {
        if (fillImage == null) yield break;

        float start = fillImage.fillAmount;
        if (Mathf.Approximately(start, target))
        {
            UpdateStarsByFill(target, firstTime:false);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            float f = Mathf.Lerp(start, target, u);

            fillImage.fillAmount = f;
            UpdateStarsByFill(f, firstTime:false); // play sound/pops exactly when we cross a threshold
            yield return null;
        }

        fillImage.fillAmount = target;
        UpdateStarsByFill(target, firstTime:false);
    }

    void UpdateStarsByFill(float normalizedFill, bool firstTime)
    {
        if (_starNorms == null) return;

        for (int i = 0; i < starAnchors.Length && i < _starNorms.Length; i++)
        {
            var img = starAnchors[i];
            if (!img) continue;

            bool earnedNow = normalizedFill >= _starNorms[i];

            // On first-time init, just set the correct sprite without SFX.
            if (firstTime)
            {
                img.sprite = earnedNow && starFilledSprite ? starFilledSprite
                                                           : (starEmptySprite ? starEmptySprite : img.sprite);
                _starEarned[i] = earnedNow;
                continue;
            }

            // Detect crossing (from not earned -> earned)
            if (!_starEarned[i] && earnedNow)
            {
                img.sprite = starFilledSprite ? starFilledSprite : img.sprite;
                StartCoroutine(Pop(img.rectTransform));
                PlayStarSfx(i);
                _starEarned[i] = true;
            }
            else if (!earnedNow)
            {
                // If score can ever go down and you want stars to unfill, handle here.
                img.sprite = starEmptySprite ? starEmptySprite : img.sprite;
                _starEarned[i] = false;
            }
        }
    }

    void PlayStarSfx(int starIndex)
    {
        if (audioSource == null) return;

        AudioClip clip = null;
        if (perStarClips != null && starIndex >= 0 && starIndex < perStarClips.Length)
            clip = perStarClips[starIndex];

        if (clip != null) audioSource.PlayOneShot(clip);
    }

    IEnumerator Pop(RectTransform rt)
    {
        if (!rt) yield break;
        const float d = 0.18f;
        float t = 0f; Vector3 a = Vector3.one, b = Vector3.one * 1.2f;
        while (t < d) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.Lerp(a, b, t/d); yield return null; }
        t = 0f;       while (t < d) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.Lerp(b, a, t/d); yield return null; }
        rt.localScale = Vector3.one;
    }

    public void PositionStars(int maxScore, int[] thresholds)
    {
        if (placementRect == null && fillImage != null)
            placementRect = fillImage.rectTransform;
        if (!placementRect || thresholds == null) return;

        float width = placementRect.rect.width;
        for (int i = 0; i < starAnchors.Length && i < thresholds.Length; i++)
        {
            var img = starAnchors[i];
            if (!img) continue;

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
            rt.pivot     = new Vector2(0.5f, rt.pivot.y);

            float p = maxScore > 0 ? Mathf.Clamp01(thresholds[i] / (float)maxScore) : 0f;
            float half = rt.rect.width * 0.5f;                     // keep fully inside
            float x = Mathf.Lerp(half, width - half, p);
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);

            if (starEmptySprite) img.sprite = starEmptySprite;
        }
    }

    void OnRectTransformDimensionsChange()
    {
        var gm = GameManager.Instance;
        if (gm != null) PositionStars(gm.maxScore, gm.GetStarThresholds());
    }
}
