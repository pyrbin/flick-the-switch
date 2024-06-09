using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public struct RoundState
{
    public int Level;
    public List<GameObject> SpawnedEnemies;
    public List<GameObject> SpawnedPickups;
    public int InitialEnemiesCount;
    public int SpawnedEnemiesCount => SpawnedEnemies.Count;
    public int DeadEnemiesCount => InitialEnemiesCount - SpawnedEnemiesCount;
}

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    public Transform RoundContainer;
    public BoxCollider? CloseSpawn;

    [Header("Gameplay")]
    [ReorderableList]
    public GameObject[] EnemyPrefabs;

    [ReorderableList]
    public GameObject[] PickupsPrefabs;

    private Rect CloseSpawnBounds = new();

    [ShowNativeProperty]
    public RoundState CurrentRoundState { get; private set; } = new();

    public static RoundManager Instance { get; private set; }

    public float CalculateEnemyHealth() => 10 + (Game.Instance.CurrentLevel * 2) + (Game.Instance.CurrentLevel > 5 ? Game.Instance.CurrentLevel.Pow(2) * 0.25f : 0f);

    public int CalculateEnemySpawnRate() => Mathfs.FloorToInt(Freya.Random.Range(1, 3) + (Game.Instance.CurrentLevel >= 4 ? Freya.Random.Range(1, Game.Instance.CurrentLevel - 3)  : 0f));
    public int CalculatePickupSpawnRate() => Mathfs.FloorToInt(Freya.Random.Range(1, Mathfs.CeilToInt(Game.Instance.CurrentLevel/4) + 1));

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        CloseSpawnBounds = Get2DSpawnBounds(CloseSpawn!.bounds);
    }

    const int bossLevel = 5;
    const float reserveSize = 1.5f;
    public void PrepareRound(int level)
    {
        ReservedAreas.Clear();
        RoundState state = new();
        state.Level = level;
        state.SpawnedEnemies = new();
        state.SpawnedPickups = new();

        if (level == bossLevel)
        {
            PrepareBossLevel();
            state.InitialEnemiesCount = state.SpawnedEnemies.Count;
            CurrentRoundState = state;
            return;
        }

        for (int i = 0; i < CalculateEnemySpawnRate(); i++)
        {
            var spawnPosition = RequestSpawnPosition();
            var randomMonster = Freya.Random.Range(0, EnemyPrefabs.Length);
            var enemyPrefab = EnemyPrefabs[randomMonster];
            var enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, RoundContainer);
            var hp = enemy.GetComponent<Health>();
            hp._base = CalculateEnemyHealth();
            state.SpawnedEnemies.Add(enemy);
        }

        for (int i = 0; i < CalculatePickupSpawnRate(); i++)
        {
            var spawnPosition = RequestSpawnPosition();
            var randomPickup = Freya.Random.Range(0, PickupsPrefabs.Length);
            var pickupPrefab = PickupsPrefabs[randomPickup];
            var pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity, RoundContainer);
            var hp = pickup.GetComponent<Health>();
            hp._base = Mathfs.CeilToInt(CalculateEnemyHealth() * 0.33f);
            state.SpawnedPickups.Add(pickup);
        }

        state.InitialEnemiesCount = state.SpawnedEnemies.Count;
        CurrentRoundState = state;
    }

    private int _bossLevelPhase = 1;
    public void PrepareBossLevel()
    {

    }

    List<Rect> ReservedAreas = new();
    public float3 RequestSpawnPosition()
    {
        Vector2 spawnPosition;
        bool positionFound = false;
        const int maxTry = 20;
        var i = 0;
        while (!positionFound)
        {
            if (i > maxTry)
            {
                return float3.zero;
            }
            spawnPosition = GetRandomPointInBounds(CloseSpawnBounds);
            Rect potentialArea = new Rect(spawnPosition.x - reserveSize / 2, spawnPosition.y - reserveSize / 2, reserveSize, reserveSize);

            if (!IsAreaReserved(potentialArea, ref ReservedAreas))
            {
                ReservedAreas.Add(potentialArea);
                return new float3(spawnPosition.x, -1f, spawnPosition.y);
            }

            i++;
        }

        return float3.zero;
    }

    public void TeardownRound()
    {
        foreach (var enemy in CurrentRoundState.SpawnedEnemies)
        {
            Destroy(enemy.gameObject);
        }
        foreach (var pickup in CurrentRoundState.SpawnedPickups)
        {
            Destroy(pickup.gameObject);
        }
        CurrentRoundState.SpawnedEnemies.Clear();
        CurrentRoundState.SpawnedPickups.Clear();
        ReservedAreas.Clear();
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
        CurrentRoundState.SpawnedEnemies?.Remove(enemy);
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
