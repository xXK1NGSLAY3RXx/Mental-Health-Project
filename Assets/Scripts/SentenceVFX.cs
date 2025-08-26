using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual effects for a sentence:
///  • Hit bump (capped, decays back)
///  • Threshold reached (stub for now)
///  • Absorb: scale up → hold → fly to world target → shrink to 0 → callback
/// Place a world Transform where you want it to fly and tag it "AbsorbWorldTarget".
/// </summary>
public class SentenceVFX : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Root RectTransform to animate. If null, uses this component’s RectTransform.")]
    public RectTransform fxRoot;

    [Header("Hit (boid impact) bump")]
    public float hitScaleStep = 0.06f;     // extra scale per hit (+6%)
    public float maxHitScale  = 1.20f;     // clamp to 120%
    public float hitHoldTime  = 0.10f;     // keep bump before decay
    public float hitDecayPerSecond = 3.0f; // decay speed
    public float hitUpOffset = 8f;         // vertical pop at max bump
    public float hitSmoothTime = 0.06f;    // smoothing for scale/pos

    [Header("Absorb FX")]
    public float absorbScaleUpTo        = 1.25f;
    public float absorbScaleUpDuration  = 0.25f;
    public float absorbHoldDuration     = 0.35f;
    public float absorbFlyDuration      = 0.28f;
    public AnimationCurve absorbUpCurve = null; // set to EaseInOut in inspector if null
    public AnimationCurve absorbFlyCurve= null; // set to EaseInOut in inspector if null

    [Header("Fly target (world)")]
    [Tooltip("found by tag 'AbsorbWorldTarget'.")]
    private Transform worldFlyTarget;
    public string worldFlyTargetTag = "AbsorbedSentencePoint";

    // internals
    RectTransform _rt;
    Vector3 _baseScale;
    Vector3 _baseWorldPos;

    float   _bumpExtra;  // extra scale above 1.0
    float   _bumpVel;    // SmoothDamp scale vel
    Vector3 _posVel;     // SmoothDamp pos vel
    float   _lastHitTime;
    bool    _hitRunning;
    bool    _absorbing;

    Coroutine _hitCo;
    Coroutine _absorbCo;

    void Awake()
    {
        _rt = fxRoot ? fxRoot : GetComponent<RectTransform>();
        _baseScale    = _rt.localScale;
        _baseWorldPos = _rt.position;

      
            var go = GameObject.FindWithTag(worldFlyTargetTag);
            if (go) worldFlyTarget = go.transform;
        

        if (absorbUpCurve == null)  absorbUpCurve  = AnimationCurve.EaseInOut(0,0,1,1);
        if (absorbFlyCurve == null) absorbFlyCurve = AnimationCurve.EaseInOut(0,0,1,1);
    }

    void OnEnable()
    {
        _bumpExtra  = 0f;
        _bumpVel    = 0f;
        _posVel     = Vector3.zero;
        _hitRunning = false;
        _absorbing  = false;
        // Do NOT force position/scale here; we animate from wherever we currently are.
    }

    /// <summary>Set current pos/scale as the new base for bumps/offsets.</summary>
    private void RebaseToCurrent()
    {
        _baseWorldPos = _rt.position;
        _baseScale    = _rt.localScale;
    }

    // ----------------- Hit bump -----------------
    public void OnBoidHit()
    {
        if (_absorbing) return; // ignore hits during absorb

        float current = 1f + _bumpExtra;
        float room    = Mathf.Max(0f, maxHitScale - current);
        _bumpExtra   += Mathf.Min(room, hitScaleStep);
        _lastHitTime  = Time.time;

        if (!_hitRunning)
        {
            RebaseToCurrent();                // anchor bump to where we are now
            _hitCo = StartCoroutine(HitLoop());
        }
    }

    IEnumerator HitLoop()
    {
        _hitRunning = true;

        while (true)
        {
            // if absorb kicked in meanwhile, stop immediately
            if (_absorbing) break;

            float sinceLast = Time.time - _lastHitTime;

            // start decaying after a short hold
            if (sinceLast > hitHoldTime)
                _bumpExtra = Mathf.Max(0f, _bumpExtra - hitDecayPerSecond * Time.deltaTime);

            float targetScale  = 1f + _bumpExtra;
            float currentScale = Mathf.SmoothDamp(_rt.localScale.x, targetScale, ref _bumpVel, hitSmoothTime);
            currentScale       = Mathf.Clamp(currentScale, 1f, maxHitScale);

            // scale uniformly around the re-based scale
            _rt.localScale = _baseScale * currentScale;

            // positional pop based on bump (world-space)
            float t = Mathf.InverseLerp(1f, maxHitScale, currentScale);
            Vector3 targetPos = _baseWorldPos + Vector3.up * (hitUpOffset * t);
            _rt.position = Vector3.SmoothDamp(_rt.position, targetPos, ref _posVel, hitSmoothTime);

            // stop when fully settled back
            if (_bumpExtra <= 0.0001f && sinceLast > hitHoldTime && Mathf.Abs(currentScale - 1f) < 0.001f)
            {
                _rt.localScale = _baseScale;
                _rt.position   = _baseWorldPos;
                break;
            }
            yield return null;
        }

        _hitRunning = false;
        _hitCo = null;
    }

    // ----------------- Threshold reached (stub) -----------------
    public void OnThresholdReached(int newTierIndex)
    {
        // Stub: add threshold VFX here later.
    }

    // ----------------- Absorb FX -----------------
    /// <summary>
    /// Plays absorb VFX (scale up → hold → fly to world target → shrink to 0),
    /// temporarily hiding the provided GameObjects during the VFX,
    /// then invokes onFinished.
    /// </summary>
    public void PlayAbsorb(Action onFinished, params GameObject[] temporarilyHide)
    {
        if (_absorbCo != null) StopCoroutine(_absorbCo);
        _absorbCo = StartCoroutine(AbsorbRoutine(onFinished, temporarilyHide));
    }

    IEnumerator AbsorbRoutine(Action onFinished, GameObject[] temporarilyHide)
    {
        _absorbing = true;

        // Stop any ongoing hit animation completely so it won’t decay us back
        if (_hitCo != null) { StopCoroutine(_hitCo); _hitCo = null; }
        _hitRunning = false;
        _bumpExtra  = 0f;

        // Rebase to *current* place/scale so we start exactly from here
        RebaseToCurrent();

        // Hide requested bits (remember previous states)
        var prevStates = new List<(GameObject go, bool prev)>();
        if (temporarilyHide != null)
        {
            foreach (var go in temporarilyHide)
            {
                if (!go) continue;
                prevStates.Add((go, go.activeSelf));
                go.SetActive(false);
            }
        }

        // 1) Scale up (no decay interfering)
        float t = 0f;
        while (t < absorbScaleUpDuration)
        {
            float u = absorbUpCurve.Evaluate(t / absorbScaleUpDuration);
            _rt.localScale = Vector3.Lerp(_baseScale, _baseScale * absorbScaleUpTo, u);
            t += Time.deltaTime; yield return null;
        }
        _rt.localScale = _baseScale * absorbScaleUpTo;

        // 2) Hold for readability
        if (absorbHoldDuration > 0f)
            yield return new WaitForSeconds(absorbHoldDuration);

        // 3) Fly (world space) & shrink to 0
        Vector3 startPos = _rt.position;
        Vector3 endPos   = worldFlyTarget ? worldFlyTarget.position : startPos;

        t = 0f;
        while (t < absorbFlyDuration)
        {
            float u = absorbFlyCurve.Evaluate(t / absorbFlyDuration);
            _rt.position   = Vector3.Lerp(startPos, endPos, u);
            _rt.localScale = Vector3.Lerp(_baseScale * absorbScaleUpTo, Vector3.zero, u);
            t += Time.deltaTime; yield return null;
        }
        _rt.position   = endPos;
        _rt.localScale = Vector3.zero;

        // restore hidden objects
        foreach (var (go, prev) in prevStates)
            if (go) go.SetActive(prev);

        _absorbing = false;
        onFinished?.Invoke();
        _absorbCo = null;
    }
}
