using System;
using System.Linq;
using UnityEngine;

public class Sentence : MonoBehaviour
{
    [Tooltip("Definition SO for thresholds and multipliers.")]
    public SentenceDefinition definition;

    public int  PolarityScore { get; private set; } = 0;
    [HideInInspector] public float timer { get; private set; }
    [HideInInspector] public bool  reachedEnd = false;

    // Events
    public event Action<int> OnPolarityScoreChanged;
    public event Action<int> OnAbsorbed;
    public event Action      OnBoidHit;
    public event Action<int> OnThresholdReached;   // fired when we advance to a new tier
    public event Action      OnAbsorbRequested;    // UI should play FX, then call ConfirmAbsorb()

    // Tier locking (no backsliding to previous tiers)
    private int[] _thresholdsAsc;      // ascending distinct thresholds, e.g., [0, 10, 25]
    private int   _tierIndex = 0;      // index into _thresholdsAsc
    public  int   CurrentTierFloor { get; private set; } = 0; // locked floor
    public  int   CurrentTierCeil  { get; private set; } = 0; // next threshold or top

    // Public readouts for UI
    public int   TierIndex    => _thresholdsAsc != null ? _tierIndex : 0;                     // 0 == Default tier
    public int   TotalTiers   => _thresholdsAsc != null ? _thresholdsAsc.Length : 0;          // includes Default
    public bool  AtTopTier    => _thresholdsAsc != null && _tierIndex >= _thresholdsAsc.Length - 1;
    public int   TopThreshold => _thresholdsAsc != null ? _thresholdsAsc[_thresholdsAsc.Length - 1] : 0;

    private bool _initialized   = false;
    private bool _absorbPending = false; // absorb FX running / requested
    private bool _absorbed      = false;

    private Canvas     _canvas;
    private Collider2D _col;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas != null)
        {
            _canvas.renderMode  = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;
        }

        _col = GetComponent<Collider2D>(); // cache once
    }

    /// Must be called after 'definition' is assigned (spawner/creator does this).
    public void InitFromDefinition()
    {
        if (_initialized) return;

        if (definition == null || definition.levels == null || definition.levels.Count == 0)
        {
            _thresholdsAsc = new[] { 0 };
            _tierIndex = 0;
            UpdateTierBounds();
            _initialized = true;
            return;
        }

        _thresholdsAsc = definition.levels
            .Select(l => Mathf.Max(0, l.threshold))
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        if (_thresholdsAsc.Length == 0) _thresholdsAsc = new[] { 0 };

        _tierIndex = Array.IndexOf(_thresholdsAsc, 0);
        if (_tierIndex < 0) _tierIndex = 0;

        UpdateTierBounds();
        PolarityScore = Mathf.Max(PolarityScore, CurrentTierFloor);

        _initialized = true;
    }

    void Update()
    {
        if (_absorbed || _absorbPending) return;

        if (reachedEnd)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                RequestAbsorb(); // ask UI to play FX first
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var agent = other.GetComponent<FlockAgent>();
        if (agent == null) return;

        if (!_initialized) InitFromDefinition();
        if (!_initialized) return;

        // While absorb FX is pending/running or already absorbed, ignore boids and consume them.
        if (_absorbPending || _absorbed)
        {
            if (agent.ParentFlock != null) agent.ParentFlock.RemoveAgent(agent);
            else Destroy(agent.gameObject);
            return;
        }

        OnBoidHit?.Invoke();

        int delta = agent.polarityScore;

        if (agent.polarity == Polarity.Positive)
        {
            PolarityScore += delta;

            // Advance tier(s) if we crossed thresholds
            while (_tierIndex < _thresholdsAsc.Length - 1 &&
                   PolarityScore >= _thresholdsAsc[_tierIndex + 1])
            {
                _tierIndex++;
                UpdateTierBounds(); // locks floor upward
                OnThresholdReached?.Invoke(_tierIndex);
            }
        }
        else
        {
            // Negative reduces score but never below the locked floor for the current tier
            PolarityScore = Mathf.Max(CurrentTierFloor, PolarityScore - delta);
        }

        OnPolarityScoreChanged?.Invoke(PolarityScore);

        // Remove agent safely
        if (agent.ParentFlock != null) agent.ParentFlock.RemoveAgent(agent);
        else Destroy(agent.gameObject);

        // When top reached, request absorb (FX first)
        if (PolarityScore >= TopThreshold)
            RequestAbsorb();
    }

    private void UpdateTierBounds()
    {
        if (_thresholdsAsc == null || _thresholdsAsc.Length == 0)
        {
            CurrentTierFloor = 0;
            CurrentTierCeil  = 0;
            return;
        }

        CurrentTierFloor = _thresholdsAsc[Mathf.Clamp(_tierIndex, 0, _thresholdsAsc.Length - 1)];
        CurrentTierCeil  = _thresholdsAsc[Mathf.Clamp(_tierIndex + 1, 0, _thresholdsAsc.Length - 1)];
    }

    private void RequestAbsorb()
    {
        if (_absorbPending || _absorbed) return;

        _absorbPending = true;

        // HARD-BLOCK any further collisions affecting visuals/score:
        if (_col != null) _col.enabled = false;

        OnAbsorbRequested?.Invoke(); // UI plays FX, then calls ConfirmAbsorb()
    }

    /// Call this from UI after absorb FX completes.
    public void ConfirmAbsorb()
    {
        if (_absorbed) return;
        _absorbed = true;
        DoAbsorption();
    }

    private void DoAbsorption()
    {
        int points = CalculateAbsorptionPoints();
        OnAbsorbed?.Invoke(points);
        Destroy(gameObject);
    }

    public int CalculateAbsorptionPoints()
    {
        float m = definition != null ? definition.GetMultiplierForScore(PolarityScore) : 1f;
        return Mathf.RoundToInt((definition != null ? definition.baseAbsorptionPoints : 0) * m);
    }

    public void SetLifetime(float amount) => timer = amount;
}
