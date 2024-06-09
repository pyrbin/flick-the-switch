using UnityEngine;

[Serializable]
public struct RoundState
{
    public int Level;
    public List<GameObject> SpawnedEnemies;
    public int InitialEnemiesCount;
    public int SpawnedEnemiesCount => SpawnedEnemies.Count;
    public int DeadEnemiesCount => InitialEnemiesCount - SpawnedEnemiesCount;
}

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    public Transform RoundContainer;
    public BoxCollider? CloseSpawn;
    public BoxCollider? FarSpawn;

    [Header("Gameplay")]
    [ReorderableList]
    public GameObject[] EnemyPrefabs;
    public GameObject EnemyPrefab;

    private Rect CloseSpawnBounds = new();
    private Rect FarSpawnBounds = new();

    [ShowNativeProperty]
    public RoundState CurrentRoundState { get; private set; }

    public static RoundManager Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        CloseSpawnBounds = Get2DSpawnBounds(CloseSpawn!.bounds);
        FarSpawnBounds = Get2DSpawnBounds(FarSpawn!.bounds);
    }

    public void PrepareRound(int level)
    {
        const float reserveSize = 1.5f;

        RoundState state = new();
        state.Level = level;
        state.SpawnedEnemies = new();

        List<Rect> reservedCloseAreas = new();
        List<Rect> reservedFarAreas = new();

        const int testMobs = 2;
        for (int i = 0; i < testMobs ; i++)
        {
            Vector2 spawnPosition;
            bool positionFound = false;

            while (!positionFound)
            {
                spawnPosition = GetRandomPointInBounds(CloseSpawnBounds);
                Rect potentialArea = new Rect(spawnPosition.x - reserveSize / 2, spawnPosition.y - reserveSize / 2, reserveSize, reserveSize);

                if (!IsAreaReserved(potentialArea, ref reservedCloseAreas))
                {
                    reservedCloseAreas.Add(potentialArea);
                    var spawnPosition3 = new float3(spawnPosition.x, -1f, spawnPosition.y);
                    var enemy = Instantiate(EnemyPrefab, spawnPosition3, Quaternion.identity, RoundContainer);

                    state.SpawnedEnemies.Add(enemy);
                    positionFound = true;
                }
            }
        }
        state.InitialEnemiesCount = state.SpawnedEnemies.Count;

        CurrentRoundState = state;
    }

    public void SpawnRoundEntity()
    {
        // Examples of interactable:
            // Coin Purse
            // Oil Lantern
            // Falling Stone
            // Mud (slows cursor)
        // RollForEnemyOrInteractable() (interactable: max 3, min 1)

        // If Enemy:
        // RollForEnemySkin
        // RollForEnemyBehavior (Circle, Jumping, Moving, Flying)
        // RollForEnemyAffix (Teleport, Spiky, Screaming, Baby)
        // RollForEnemyHealth (level + skinMod + affixMod)
        // RollForEnemyGold (level + healthMod + skinMod + affixMod)
        // RollForPosition
    }

    public void TeardownRound()
    {
        foreach (var enemy in CurrentRoundState.SpawnedEnemies)
        {
            Destroy(enemy.gameObject);
        }
        CurrentRoundState.SpawnedEnemies.Clear();
    }

    public void Reset()
    {
        TeardownRound();
    }

    public bool CheckRoundState()
    {
        if (CurrentRoundState.SpawnedEnemiesCount == 0)
        {
            return true;
        }

        return false;
    }

    public void RemoveFromState(GameObject enemy)
    {
        CurrentRoundState.SpawnedEnemies.Remove(enemy);
    }

    bool IsAreaReserved(Rect area, ref List<Rect> reservedAreas)
    {
        foreach (var reservedArea in reservedAreas)
        {
            if (reservedArea.Overlaps(area)) return true;
        }
        return false;
    }

    public float2 GetRandomPointInBounds(Rect bounds)
    {
        var x = Freya.Random.Range(bounds.xMin, bounds.xMax);
        var y = Freya.Random.Range(bounds.yMin, bounds.yMax);
        return new float2(x, y);
    }

    public Rect Get2DSpawnBounds(Bounds collider)
    {
        var center = collider.center;
        var size = collider.size;
        var halfSize = size * 0.5f;
        var min = center - halfSize;
        var max = center + halfSize;
        return new Rect(min.x, min.z, max.x, max.z);
    }

}
