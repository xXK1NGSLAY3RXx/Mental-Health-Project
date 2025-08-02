using UnityEngine;
using System.Collections;

[System.Serializable]
public class TimedEvent
{
    [Tooltip("Prefab with EventBehavior + EventUI")]
    public GameObject eventPrefab;
    [Tooltip("Seconds after level start to spawn this event")]
    public float spawnTime;
    [Tooltip("Lifetime (seconds) before event auto-despawns if untouched")]
    public float lifetime;
}

public class EventSpawner : MonoBehaviour
{
    [Tooltip("Configure each timed event here")]
    public TimedEvent[] events;

    [Header("Prefabs")]
    public Flock      flockPrefab;
    public GameObject bombPrefab;
    public GameObject multiplierPrefab;

    [Header("PowerUp Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Screen‐edge Offsets (0–0.5)")]
    [Tooltip("How far in from left/right (as viewport fraction)")]
    [Range(0f, 0.5f)] public float horizontalOffset = 0.1f;
    [Tooltip("How far in from top/bottom (as viewport fraction)")]
    [Range(0f, 0.5f)] public float verticalOffset   = 0.1f;

    void Start()
    {
        foreach (var e in events)
            StartCoroutine(Schedule(e));
    }

    IEnumerator Schedule(TimedEvent e)
    {
        yield return new WaitForSeconds(e.spawnTime);
        bool spawnLeft = Random.value < 0.5f;
        Vector3 pos = GetRandomLeftOrRightEdge(spawnLeft);
        var go = Instantiate(e.eventPrefab, pos, Quaternion.identity);

        var beh = go.GetComponent<EventBehavior>();
        if (beh != null)
        {
            beh.spawnLeft = spawnLeft;
            go.GetComponent<EventUI>().Init(spawnLeft);
            Destroy(go, e.lifetime);
            beh.OnActivated += HandleActivation;
        }
    }

    void HandleActivation(EventBehavior e)
    {
        Vector3 pos = e.transform.position;

        // Positive Boids
        if (e.spawnPositiveBoids)
        {
            var flock = Instantiate(flockPrefab, pos, Quaternion.identity);
            flock.startingCount   = e.positiveBoidCount;
            flock.defaultPolarity = Polarity.Positive;
        }
        // Negative Boids
        if (e.spawnNegativeBoids)
        {
            var flock = Instantiate(flockPrefab, pos, Quaternion.identity);
            flock.startingCount   = e.negativeBoidCount;
            flock.defaultPolarity = Polarity.Negative;
        }

        // Bomb
        if (e.spawnBomb && bombPrefab != null && spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, spawnPoints.Length);
            var b = Instantiate(bombPrefab, spawnPoints[idx].position, Quaternion.identity);
            b.GetComponent<Bomb>().timer = e.bombTimer;
        }

        // Multiplier
        if (e.spawnMultiplier && multiplierPrefab != null && spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, spawnPoints.Length);
            var m = Instantiate(multiplierPrefab, spawnPoints[idx].position, Quaternion.identity);
            m.GetComponent<TimedPolarityMultiplier>().delay = e.multiplierTimer;
        }

        // Portal
        if (e.spawnPortal)
        {
            foreach (var p in GameObject.FindGameObjectsWithTag(e.portalTag))
                //p.GetComponent<Portal>().enabledPortal = true;
                 p.SetActive(true);
        }
    }

    Vector3 GetRandomLeftOrRightEdge(bool spawnLeft)
    {
        Camera cam = Camera.main;
        float z = -cam.transform.position.z;

        // pick X at left or right, but inset by horizontalOffset
        float x = spawnLeft
            ? horizontalOffset
            : 1f - horizontalOffset;

        // pick Y anywhere between verticalOffset and 1 - verticalOffset
        float y = Random.Range(verticalOffset, 1f - verticalOffset);

        Vector3 viewportPos = new Vector3(x, y, z);
        Vector3 worldPos    = cam.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0f;
        return worldPos;
    }
}
