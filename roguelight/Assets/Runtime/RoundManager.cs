using JetBrains.Annotations;
using JSAM;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct RoundState
{
    public int Level;
    [SerializeField]
    public List<GameObject> SpawnedEnemies;
    [SerializeField]
    public List<GameObject> SpawnedPickups;
    public int InitialEnemiesCount;
    [ShowNativeProperty]
    public int SpawnedEnemiesCount => SpawnedEnemies.Count;
    [ShowNativeProperty]
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

    [Header("Boss")]
    public Transform _BossRightHandSpawn;
    public Transform _BossLeftHandSpawn;
    public Transform _BossSpawn;
    public ParticleSystem _BossRightHandSpawnParticleSystem;
    public ParticleSystem _BossLeftHandSpawnParticleSystem;
    public ParticleSystem _BossSpawnParticleSystem;
    public MoleBoss _BossPrefab;
    public MoleHand _BossRightHandPrefab;
    public MoleHand _BossLeftHandPrefab;
    public SoundFileObject _EarthquakeSound;
    public BoxCollider BossTopSpawn;
    public GameObject BarrelPrefab;

    private MoleBoss _Boss;
    private MoleHand _BossRightHand;
    private MoleHand _BossLeftHand;


    private Rect CloseSpawnBounds = new();
    private Rect TopSpawnBounds = new();

    [ShowNativeProperty]
    public RoundState CurrentRoundState { get; private set; } = new();

    public static RoundManager Instance { get; private set; }

    // scalings xd
    public float CalculateEnemyHealth() => 9 + (Game.Instance.CurrentLevel * 4) + (Game.Instance.CurrentLevel >= Game.Instance.BossLevel ? Game.Instance.CurrentLevel.Pow(2) * 0.25f : 0f);
    public int CalculateEnemySpawnRate() => Mathfs.FloorToInt(Freya.Random.Range(2, (Game.Instance.CurrentLevel / 2f).CeilToInt()) + (Game.Instance.CurrentLevel >= Game.Instance.BossLevel - 2 ? Freya.Random.Range(1, Game.Instance.CurrentLevel -  Game.Instance.BossLevel / 2)  : 0f));
    public int CalculatePickupSpawnRate() => Mathfs.FloorToInt(Freya.Random.Range(1, Mathfs.CeilToInt(Game.Instance.CurrentLevel/4) + 1));

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _BossSpawnParticleSystem.Stop();
        _BossLeftHandSpawnParticleSystem.Stop();
        _BossRightHandSpawnParticleSystem.Stop();
        CloseSpawnBounds = Get2DSpawnBounds(CloseSpawn!.bounds);
        TopSpawnBounds = Get2DSpawnBounds(BossTopSpawn!.bounds);
    }

    const float reserveSize = 1.5f;
    public void PrepareRound(int level)
    {
        ReservedAreas.Clear();
        RoundState state = new();
        state.Level = level;
        state.SpawnedEnemies = new();
        state.SpawnedPickups = new();

        if (level == Game.Instance.BossLevel)
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

    private bool _leftHandDead = false;
    private bool _rightHandDead = false;
    bool _spawnedBoos = false;
    bool _bossActive = false;

    const float defaultFov = 40;
    const float animFov = 50;
    const float handAnimation = 4f;
    const float bossAnimation = 4f;

    public void PrepareBossLevel()
    {
        _spawnedBoos = false;

        Camera.main.DOFieldOfView(animFov, 1f).SetEase(Ease.Linear).OnComplete(() => {
            AudioManager.PlaySound(_EarthquakeSound);

            _BossRightHand = Instantiate(_BossRightHandPrefab, _BossRightHandSpawn.transform.position, Quaternion.identity, RoundContainer);
            _BossLeftHand = Instantiate(_BossLeftHandPrefab, _BossLeftHandSpawn.transform.position, Quaternion.identity, RoundContainer);

            _BossRightHand.RunSpawnAnimation(handAnimation);
            _BossLeftHand.RunSpawnAnimation(handAnimation);
            _BossLeftHandSpawnParticleSystem.gameObject.SetActive(true);
            _BossLeftHandSpawnParticleSystem.Play();
            _BossRightHandSpawnParticleSystem.gameObject.SetActive(true);
            _BossRightHandSpawnParticleSystem.Play();
            TweenTools.Shake2(Camera.main.transform, handAnimation, 5.5f);

            _BossRightHand.GetComponent<Enemy>().OnDeath += () => {
                _rightHandDead = true;
                if (_leftHandDead && _rightHandDead)
                {
                    PrepareBossLevelPhase2();
                }
            };
            _BossLeftHand.GetComponent<Enemy>().OnDeath += () => {
                _leftHandDead = true;
                if (_leftHandDead && _rightHandDead)
                {
                    PrepareBossLevelPhase2();
                }
            };

            UniTask.Void(async () => {
                await UniTask.Delay(TimeSpan.FromSeconds(handAnimation), ignoreTimeScale: false);
                _BossLeftHandSpawnParticleSystem.Stop();
                _BossLeftHandSpawnParticleSystem.gameObject.SetActive(false);
                _BossRightHandSpawnParticleSystem.Stop();
                _BossRightHandSpawnParticleSystem.gameObject.SetActive(false);
                Camera.main.DOFieldOfView(defaultFov, 0.333f).SetEase(Ease.Linear);
                BossLevelPhase1Started();
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale: false);
                AudioManager.StopSound(_EarthquakeSound);
            });
        });
    }

    public void BossLevelPhase1Started()
    {
        Game.Instance.PauseOilDepletion = false;
        _bossActive = true;
    }

    public void PrepareBossLevelPhase2()
    {
        _bossActive = false;
        Game.Instance.PauseOilDepletion = true;

        AudioManager.PlaySound(_EarthquakeSound);

        Camera.main.DOFieldOfView(animFov, 1f).SetEase(Ease.Linear).OnComplete(() => {
            _Boss = Instantiate(_BossPrefab, _BossSpawn.transform.position, Quaternion.identity, RoundContainer);
            _Boss.RunSpawnAnimation(bossAnimation);
            _spawnedBoos = true;

            _BossSpawnParticleSystem.gameObject.SetActive(true);
            _BossSpawnParticleSystem.Play();
            TweenTools.Shake2(Camera.main.transform, handAnimation, 5.5f);

            _Boss.GetComponent<Enemy>().OnDeath += () => {
                UniTask.Void(async () => {
                    _BossSpawnParticleSystem.Stop();
                    _BossSpawnParticleSystem.gameObject.SetActive(false);
                    _bossActive = false;
                    Game.Instance.PauseOilDepletion = true;
                    AudioManager.PlaySound(_EarthquakeSound);
                    TeardownBoss(false);
                    await UniTask.Delay(TimeSpan.FromSeconds(3f), ignoreTimeScale: false);
                    AudioManager.StopSound(_EarthquakeSound);
                    _Boss = null;
                    _BossLeftHand = null;
                    _BossRightHand = null;
                });
            };

            UniTask.Void(async () => {
                await UniTask.Delay(TimeSpan.FromSeconds(bossAnimation), ignoreTimeScale: false);
                Camera.main.DOFieldOfView(defaultFov, 0.333f).SetEase(Ease.Linear);
                BossLevelPhase2Started();
                await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: false);
                AudioManager.StopSound(_EarthquakeSound);
            });
        });
    }

    void TeardownBoss(bool setNull = true)
    {
        _BossSpawnParticleSystem.Stop();
        _BossSpawnParticleSystem.gameObject.SetActive(false);
        _bossActive = false;
        _BossLeftHand?.RunDespawnAnimation(3f);
        _BossRightHand?.RunDespawnAnimation(3f);
        _Boss?.RunDespawnAnimation(3f);
        if (setNull)
        {
            _Boss = null;
            _BossLeftHand = null;
            _BossRightHand = null;
        }
    }

    List<GameObject> _barrels = new();
    void SpawnBarrel()
    {
        var spawnPoint = GetRandomPointInBounds(TopSpawnBounds);
        var barrel = Instantiate(BarrelPrefab,new float3(spawnPoint.x, 13f, spawnPoint.y), Quaternion.identity, RoundContainer);
        _barrels.Add(barrel);
    }

    const float spawnRate = 1.5f;
    private float elapsedTime = 0;
    void Update()
    {
        if (!_bossActive) return;
        if (_bossActive && elapsedTime > spawnRate)
        {
            elapsedTime = 0;
            SpawnBarrel();
            return;
        }
        elapsedTime += Time.deltaTime;
    }

    public void BossLevelPhase2Started()
    {
        Game.Instance.PauseOilDepletion = false;
        _bossActive = true;
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
        TeardownBoss(true);
        _leftHandDead = false;
        _rightHandDead = false;
        Game.Instance.PauseOilDepletion = false;
        _spawnedBoos = false;
        _bossActive = false;
        elapsedTime = 0;

        foreach (var barrel in _barrels)
        {
            Destroy(barrel);
        }
        _barrels.Clear();

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
        if (Game.Instance.IsDoingBoss) return _spawnedBoos && _Boss is null;

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
