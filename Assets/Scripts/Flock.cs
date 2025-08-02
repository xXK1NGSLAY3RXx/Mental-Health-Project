using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [Header("Agent Setup")]
    public FlockAgent agentPrefab;
    public LayerMask AgentLayer;
    List<FlockAgent> agents = new List<FlockAgent>();
    public FlockBehavior behavior;

    [Header("Spawn Settings")]
    [Range(0, 500)]
    public int startingCount = 250;
    [Tooltip("Radius around this Flock’s transform to spread agents")]
    public float spawnRadius = 5f;

    [Header("Movement Settings")]
    [Range(1f, 100f)]
    public float driveFactor = 10f;
    [Range(1f, 100f)]
    public float maxSpeed = 5f;
    [Range(1f, 10f)]
    public float neighborRadius = 1.5f;
    [Range(0f, 1f)]
    public float avoidanceRadiusMultiplier = 0.5f;

    [Header("Polarity Settings")]
    public Polarity defaultPolarity = Polarity.Neutral;
    public Color positiveColor = Color.green;
    public Sprite positiveSprite;
    public Color neutralColor = Color.gray;
    public Sprite neutralSprite;
    public Color negativeColor = Color.red;
    public Sprite negativeSprite;

    public int polarityScore = 0;

    [Header("Phase Spawning")]
    
    [Tooltip("If true, spawns each agent on a random screen edge instead of in a circle")]
    public bool spawnOnScreenEdge = false;

    [Tooltip("Minimum distance between spawned agents when on screen edge")]
    public float minSpawnSpacing = 1f;

    float squareMaxSpeed;
    float squareNeighborRadius;
    float squareAvoidanceRadius;
    public float SquareAvoidanceRadius => squareAvoidanceRadius;

    void Start()
    {
        squareMaxSpeed = maxSpeed * maxSpeed;
        squareNeighborRadius = neighborRadius * neighborRadius;
        squareAvoidanceRadius = squareNeighborRadius * avoidanceRadiusMultiplier * avoidanceRadiusMultiplier;

        // Spawn around THIS flock’s position, inside spawnRadius
        for (int i = 0; i < startingCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + (Vector3)offset;

            // optional: raycast or OverlapCircle to avoid spawning *inside* other agents
            FlockAgent newAgent = Instantiate(
                agentPrefab,
                pos,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.name = $"{name} Agent {i}";
            newAgent.polarity = defaultPolarity;
            newAgent.ParentFlock = this;
            newAgent.polarityScore = polarityScore;

            // tint them:
            var sr = newAgent.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                switch (defaultPolarity)
                {
                    case Polarity.Positive: sr.sprite = positiveSprite; break;
                    case Polarity.Neutral: sr.sprite = neutralSprite; break;
                    case Polarity.Negative: sr.sprite = negativeSprite; break;
                    // case Polarity.Positive: sr.color = positiveColor; break;
                    // case Polarity.Neutral: sr.color = neutralColor; break;
                    // case Polarity.Negative: sr.color = negativeColor; break;

                }
            }
            agents.Add(newAgent);
        }
    }

    void Update()
    {
        foreach (var agent in agents.ToArray())
        {
            var context = GetNearbyObjects(agent);
            Vector2 move = behavior.CalculateMove(agent, context, this) * driveFactor;
            if (move.sqrMagnitude > squareMaxSpeed)
                move = move.normalized * maxSpeed;
            agent.Move(move);
        }
    }

    List<Transform> GetNearbyObjects(FlockAgent agent)
    {
        var context = new List<Transform>();
        var cols = Physics2D.OverlapCircleAll(agent.transform.position, neighborRadius, AgentLayer);
        foreach (var c in cols)
            if (c != agent.AgentCollider)
                context.Add(c.transform);
        return context;
    }

    public void RemoveAgent(FlockAgent agent)
    {
        if (agents.Remove(agent))
            Destroy(agent.gameObject);
    }
    
    /// <summary>
    /// Spawns exactly one agent per polarity entry, either in a circle or on the screen edges.
    /// </summary>
    public void SpawnAgentsWithAssignedWords(List<Polarity> polarities, List<WordDefinition> wordDefs)
    {
        // 1) Clear out old agents
        ClearAgents();

        // 2) We'll collect all chosen positions here
        List<Vector3> positions = new List<Vector3>();
        

        for (int i = 0; i < polarities.Count; i++)
        {
            Vector3 spawnPos;

            if (spawnOnScreenEdge)
            {
                // Rejection‐sample until we find a spot far enough from all prior picks
                int attempts = 0;
                do
                {
                    spawnPos = GetRandomScreenBorderPosition();
                    attempts++;
                    // after too many attempts, just accept and break
                    if (attempts > 10) break;
                }
                while (positions.Exists(p => Vector3.Distance(p, spawnPos) < minSpawnSpacing));

                positions.Add(spawnPos);
            }
            else
            {
                // inside a circle around the flock’s transform
                Vector2 offset = Random.insideUnitCircle * spawnRadius;
                spawnPos = transform.position + (Vector3)offset;
            }

            // 3) Instantiate the agent
            var newAgent = Instantiate(
                agentPrefab,
                spawnPos,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.name = $"{name} Agent {i}";
            newAgent.ParentFlock = this;
            newAgent.polarity = polarities[i];
            newAgent.AssignedWord = wordDefs[i];

            // tint by polarity
            var sr = newAgent.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                switch (newAgent.polarity)
                {
                    case Polarity.Positive: sr.color = Color.green; break;
                    case Polarity.Neutral: sr.color = Color.gray; break;
                    case Polarity.Negative: sr.color = Color.red; break;
                }
            }

            agents.Add(newAgent);
        }
    }
    
    /// <summary>
    /// Spawns <paramref name="count"/> new agents of the given polarity
    /// around the specified center point, using this flock’s spawnRadius.
    /// </summary>
    public void SpawnAgentsWithPolarity(Polarity polarity, Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // pick a random point in the spawn circle
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = center + (Vector3)offset;

            // instantiate and initialize
            var newAgent = Instantiate(
                agentPrefab,
                spawnPos,
                Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)),
                transform
            );
            newAgent.name = $"{name} Agent {agents.Count}";
            newAgent.ParentFlock = this;
            newAgent.polarity = polarity;

            // tint by polarity
            var sr = newAgent.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                switch (polarity)
                {
                    case Polarity.Positive: sr.sprite = positiveSprite; break;
                    case Polarity.Neutral: sr.sprite = neutralSprite; break;
                    case Polarity.Negative: sr.sprite = negativeSprite; break;
                }
            }

            agents.Add(newAgent);
        }
    }

    

    /// <summary>
    /// Picks a random point along the edge of the screen (viewport).
    /// </summary>
    Vector3 GetRandomScreenBorderPosition()
    {
        Camera cam = Camera.main;
        // choose which side
        int side = Random.Range(0, 4);
        float x = 0f, y = 0f;
        switch (side)
        {
            case 0: x = 0f;      y = Random.value; break; // left
            case 1: x = 1f;      y = Random.value; break; // right
            case 2: x = Random.value; y = 0f;      break; // bottom
            default: x = Random.value; y = 1f;      break; // top
        }
        float z = -cam.transform.position.z; 
        Vector3 vp = new Vector3(x, y, z);
        Vector3 world = cam.ViewportToWorldPoint(vp);
        world.z = 0f;
        return world;
    }

    // You’ll need this if not already present:
    public void ClearAgents()
    {
        foreach (var a in agents.ToArray())
            RemoveAgent(a);
    }
}
