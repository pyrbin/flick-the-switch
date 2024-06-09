using JetBrains.Annotations;
using Unity.VisualScripting;
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

    [Header("Gameplay")]
    [ReorderableList]
    public GameObject[] EnemyPrefabs;

    private Rect CloseSpawnBounds = new();

    [ShowNativeProperty]
    public RoundState CurrentRoundState { get; private set; } = new();

    public static RoundManager Instance { get; private set; }

    public float CalculateEnemyHealth() => 13 + (Game.Instance.CurrentLevel * 2) + (Game.Instance.CurrentLevel > 5 ? Game.Instance.CurrentLevel.Pow(2) * 0.25f : 0f);

    public int CalculateEnemySpawnRate() => Mathfs.FloorToInt(Freya.Random.Range(2, 3) + (Game.Instance.CurrentLevel >= 4 ? Freya.Random.Range(1, Game.Instance.CurrentLevel - 3)  : 0f));

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
        RoundState state = new();
        state.Level = level;
        state.SpawnedEnemies = new();

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
