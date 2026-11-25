using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    [SerializeField] private Coin _coinPrefab;
    [SerializeField] private float _mapSize;
    [SerializeField] private float timerMinCoins;
    [SerializeField] private float timerMaxCoins;
    [SerializeField] private TMP_Text amountCoins;
    [SerializeField] private TMP_Text amountScore;
    [SerializeField] private TMP_Text bestScore;
    [SerializeField] public TMP_Text amountHP;
    public static CoinManager Instance;
    private readonly HashSet<Coin> activeCoins = new HashSet<Coin>();
    private float time;
    private float spawnTimer;
    private int baseEarnings;
    private int creditedEarnings;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() { PyaterkaLocalization.Changed += RefreshCounters; }
    private void OnDisable() { PyaterkaLocalization.Changed -= RefreshCounters; }

    private void Start()
    {
        RefreshCounters();
    }

    public void ShowHP()
    {
        if (amountHP != null && PlayerCar.Instance != null)
            amountHP.text = PlayerCar.Instance.Health.ToString();
    }

    public void ResetRun()
    {
        foreach (Coin coin in activeCoins)
        {
            if (coin == null) continue;
            coin.gameObject.SetActive(false);
            Destroy(coin.gameObject);
        }
        activeCoins.Clear();
        time = 0f;
        baseEarnings = 0;
        creditedEarnings = 0;
        spawnTimer = Random.Range(timerMinCoins, timerMaxCoins);
        Progress.Instance.Data.Score = 0;
        RefreshCounters();
    }

    private void RefreshCounters()
    {
        ProgressData data = Progress.Instance.Data;
        if (amountCoins != null) amountCoins.text = data.Coins.ToString();
        if (amountScore != null) amountScore.text = data.Score.ToString();
        if (bestScore != null) bestScore.text = PyaterkaLocalization.Format("Best: {0}", data.BestScore);
        ShowHP();
    }

    private void Update()
    {
        if (!MenuManager.GameActive) return;
        time += Time.deltaTime;
        int seconds = Mathf.FloorToInt(time);
        if (seconds > 0)
        {
            time -= seconds;
            int before = Progress.Instance.Data.Score;
            Progress.Instance.AddScore(seconds);
            Award(GameBalance.SurvivalIncome(Progress.Instance.Data.Score) - GameBalance.SurvivalIncome(before));
            RefreshCounters();
        }
        if (activeCoins.Count >= 10) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = Mathf.Max(0.1f, Random.Range(timerMinCoins, timerMaxCoins));
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 position = new Vector3(Random.Range(-_mapSize, _mapSize), 1.5f,
                Random.Range(-_mapSize, _mapSize));
            if (Physics.CheckSphere(position, 0.75f)) continue;
            SpawnCoin(position);
            break;
        }
    }

    public Coin SpawnCoin(Vector3 position)
    {
        if (_coinPrefab == null) return null;
        Coin coin = Instantiate(_coinPrefab, position, Quaternion.identity);
        activeCoins.Add(coin);
        return coin;
    }

    private void Award(int value)
    {
        if (value <= 0) return;
        baseEarnings += value;
        bool premium = PlayerCar.Instance != null && PlayerCar.Instance.CurrentCarIndex == GameBalance.PremiumCarIndex;
        int total = premium ? baseEarnings * 3 / 2 : baseEarnings;
        Progress.Instance.AddCoins(total - creditedEarnings);
        creditedEarnings = total;
    }

    public void CollectCoin(Coin pickup)
    {
        activeCoins.Remove(pickup);
        Award(pickup != null ? pickup.Value : 2);
        RefreshCounters();
    }
}
