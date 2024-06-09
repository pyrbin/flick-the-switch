using UnityEngine;
using Utilities.Extensions;

[System.Serializable]
public enum GameState
{
    Menu,
    Playing,
    Shop,
    GameOver
}

[System.Serializable]
public struct ShopEntry {
    [SerializeField]
    public int LowerLevelThreshold;
    [SerializeField]
    public int UpperLevelThreshold;
    [SerializeField]
    public Upgrade Item;
}

[System.Serializable]
public struct GameStats
{
    public int TotalGold;
    public int TotalKills;
    public int Level;
}

public class Game : MonoBehaviour
{
    [Header("References")]
    public Player? Player;

    public MainMenu? MainMenu;
    public HUD? HUD;
    public Inventory? Inventory;
    public Shop? Shop;
    public GameOverMenu? GameOverMenu;
    public Transform? UICanvas;
    public Light? RoundExitLight;

    public Switch? Switch;
    public Light? Light;
    public Transform? RoundContainer;
    public Transform? BackWall;

    [Header("Gameplay")]
    public float OilDepletionRate = 0.1f;
    public int RerollBaseCost = 2;
    public int InterestRate = 4;
    public int InterestCeiling = 20;

    [ReorderableList]
    public List<ShopEntry> ShopEntries = new();

    [ShowNativeProperty]
    public int CurrentLevel { get; private set; } = 0;

    [ShowNativeProperty]
    public int CurrentRerollCost => RerollBaseCost + RerollBaseCost * _rerollCount;

    private int _rerollCount = 0;

    [ReadOnly]
    public GameState State = GameState.Menu;

    [ReadOnly]
    public GameStats Stats = new();

    public event Action<GameState>? OnStateChange;


    public static Game Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Assert.IsFalse(Light is null);
        Assert.IsFalse(Switch is null);
        Assert.IsFalse(RoundContainer is null);
        Assert.IsFalse(BackWall is null);
        Assert.IsFalse(MainMenu is null);
        Assert.IsFalse(Inventory is null);
        Assert.IsFalse(Shop is null);
        Assert.IsFalse(GameOverMenu is null);

        Player = FindFirstObjectByType<Player>();

        Switch!.OnToggle += OnSwitchToggled;
    }

    public void Start()
    {
        Player!.GetComponent<Oil>().OnDepleted += () =>
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.GameOver);
            }
        };

        Player.OnAddToInventory += (item) =>
        {
            Inventory!.AddToInventory(item);
        };

        Player.GoldChanged += (amount, type) =>
        {
            if (type == GoldChangedMode.Added)
            {
                Stats.TotalGold += amount;
            }
        };


        Player.OnKill += () =>
        {
            Stats.TotalKills++;
        };

        UICanvas!.SetActive(true);

        MainMenu!.SetActive(false);
        HUD!.SetActive(false);
        Shop!.SetActive(false);
        Inventory!.SetActive(false);
        GameOverMenu!.SetActive(false);
        RoundExitLight!.SetActive(false);

        UpdateRoomAndLightsOnRound(false);
        SetState(GameState.Menu);
    }

    public void SetState(GameState state)
    {
        ExitState(State);
        State = state;
        OnStateChange?.Invoke(State);
        EnterState(State);
    }

    public void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                RoundExitLight!.SetActive(false);
                GameOverMenu!.SetActive(false);
                Inventory!.SetActive(false);
                Shop!.SetActive(false);
                Inventory!.SetActive(false);
                HUD!.Hide();
                Switch!.SetActive(true);
                MainMenu!.Show();
                break;
            case GameState.Playing:
                RoundExitLight!.SetActive(false);
                GameOverMenu!.SetActive(false);
                Inventory!.SetActive(false);
                Shop.SetActive(false);
                CurrentLevel++;
                Stats.Level = CurrentLevel;
                Switch!.SetActive(false);
                MainMenu!.Hide();
                HUD!.Show();
                Player!.GetComponent<Oil>().SetFull();
                UpdateRoomAndLightsOnRound(true);
                RoundManager.Instance.PrepareRound(CurrentLevel);
                break;
            case GameState.Shop:
                RoundExitLight!.SetActive(false);
                GameOverMenu!.SetActive(false);
                _rerollCount = 0;
                Inventory!.SetActive(true);
                HUD!.SetForShop();
                MainMenu!.SetForShop();
                Switch!.SetActive(true);
                Shop!.Reset();
                Shop.SetActive(true);
                foreach (var item in GenerateShopUpgradesForLevel(CurrentLevel, ref ShopEntries))
                {
                    Shop.SpawnUpgradeItem(item);
                }

                break;
            case GameState.GameOver:
                RoundExitLight!.SetActive(false);
                GameOverMenu!.SetActive(true);
                GameOverMenu.SyncData();
                Shop!.SetActive(false);
                Shop!.Reset();
                Inventory!.SetActive(true);
                MainMenu!.SetForShop();
                HUD!.SetForShop();
                Switch!.SetActive(false);
                break;
        }
    }

    public void ExitState(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                break;
            case GameState.Playing:
                UpdateRoomAndLightsOnRound(false);
                RoundManager.Instance.TeardownRound();
                break;
            case GameState.Shop:
                Player.Instance.AddGold(CalculateInterest());
                break;
            case GameState.GameOver:
                break;
        }
    }

    public void SetShopActionsDisabled(bool value) => IsShopActionsDisabled = value;

    [ShowNativeProperty]
    public bool IsShopActionsDisabled { get; private set; }

    public bool HasMoneyForReroll()
    {
        return Player!.Gold >= CurrentRerollCost;
    }

    public void RestartCycle()
    {
        CurrentLevel = 0;
        Player.Reset();
        RoundManager.Instance.Reset();
        Stats = new();
        SetState(GameState.Menu);
    }

    public void RerollShop()
    {
        if (Player!.Gold < CurrentRerollCost)
            return;

        SetShopActionsDisabled(true);

        Shop!.Reset();
        foreach (var item in GenerateShopUpgradesForLevel(CurrentLevel, ref ShopEntries))
        {
            Shop.SpawnUpgradeItem(item);
        }

        Player.Instance.RemoveGold(CurrentRerollCost);

        _rerollCount++;

        SetShopActionsDisabled(false);
    }

    public void UpdateRoomAndLightsOnRound(bool start)
    {
        Light!.enabled = !start;
        Cursor.Instance.EnableLights = start;
        RoundContainer!.SetActive(start);
        BackWall!.SetActive(!start);
        Switch!.SetActive(!start);
        Switch!.ForceToggleSilent(!start);
    }

    public void OnSwitchToggled(bool isOn)
    {
        if (State == GameState.Playing)
        {
            SetState(GameState.Shop);
            return;
        }
        UniTask.Void(async () => {
            await UniTask.Delay(TimeSpan.FromSeconds(0.333), ignoreTimeScale: false);
            SetState(GameState.Playing);
        });
    }

    public void Update()
    {
        switch(State)
        {
            case GameState.Menu:
                UpdateMainMenu();
                break;
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.Shop:
                UpdateShop();
                break;
            case GameState.GameOver:
                break;
        }
    }

    public void UpdateMainMenu()
    {

    }

    public void UpdatePlaying()
    {
        if (RoundManager.Instance is not null && RoundManager.Instance.CheckRoundState())
        {
            Switch!.SetActive(true);
            RoundExitLight!.SetActive(true);
        }

        Player!.GetComponent<Oil>().Reduce(OilDepletionRate);
    }

    public void UpdateShop()
    {

    }

    private int CalculateInterest()
    {
       return Mathfs.Min(Player.Instance.Gold / InterestRate, InterestCeiling);
    }

    const int shopEntryCount = 3;
    public List<Upgrade> GenerateShopUpgradesForLevel(int level, ref List<ShopEntry> shopEntries)
    {
        const float upperChanceModifier = 0.5f;
        List<Upgrade> selectedUpgrades = new();
        System.Random random = new System.Random();

        var eligibleEntries = shopEntries.Where(entry => level >= entry.LowerLevelThreshold).ToList();
        while (selectedUpgrades.Count < shopEntryCount && eligibleEntries.Count > 0)
        {
            eligibleEntries = eligibleEntries.OrderBy(e => random.Next()).ToList();
            foreach (var entry in eligibleEntries)
            {
                bool addEntry = true;
                if (level >= entry.UpperLevelThreshold)
                {
                    addEntry = random.NextDouble() <= upperChanceModifier;
                }
                if (addEntry)
                {
                    selectedUpgrades.Add(entry.Item);
                    if (selectedUpgrades.Count >= shopEntryCount)
                    {
                        break;
                    }
                }
            }
        }
        return selectedUpgrades;
    }
}
