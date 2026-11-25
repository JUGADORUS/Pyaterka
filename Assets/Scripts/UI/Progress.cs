using System;
using UnityEngine;

[Serializable]
public class ProgressData
{
    public bool[] CarsUnlockedDefault = { true, false, false, false, false, false };
    [NonSerialized] public int Score;
    public int Coins;
    public int BalanceVersion;
    public bool LegacyPremium;
    public int BestScore;
    public int[] Health = (int[])GameBalance.BaseHealth.Clone();
    public float[] Speed = (float[])GameBalance.BaseSpeed.Clone();
    public bool[] CarsUnlocked;
    public int SelectedCar;
    public bool PromoRedeemed;

    public static ProgressData GetEmptyProgressData()
    {
        ProgressData data = new ProgressData();
        data.BalanceVersion = GameBalance.Version;
        data.SetDefaultCars();
        return data;
    }

    public void FillEmptyValues()
    {
        ProgressData defaults = GetEmptyProgressData();
        bool[] unlocked = defaults.CarsUnlocked;
        if (CarsUnlocked != null)
            Array.Copy(CarsUnlocked, unlocked, Math.Min(CarsUnlocked.Length, unlocked.Length));
        if (BalanceVersion < GameBalance.Version)
        {
            LegacyPremium |= unlocked[GameBalance.PremiumCarIndex];
            // Refund paid upgrades above the new caps; the old 100-HP base is not an upgrade.
            int[] oldBaseHealth = { 3, 5, 100, 6, 4, 5 };
            for (int i = 0; i < 6; i++)
            {
                if (Health != null && i < Health.Length)
                    Coins += Mathf.Max(0, Mathf.Min(10, Health[i]) - Mathf.Max(GameBalance.MaxHealth[i], oldBaseHealth[i])) * 10;
                if (Speed != null && i < Speed.Length && !float.IsNaN(Speed[i]) && !float.IsInfinity(Speed[i]))
                    Coins += Mathf.Max(0, Mathf.RoundToInt((Mathf.Min(18f, Speed[i]) - GameBalance.MaxSpeed[i]) * 10f)) * 5;
            }
            BalanceVersion = GameBalance.Version;
        }
        unlocked[0] = true;
        CarsUnlocked = unlocked;
        int[] health = defaults.Health;
        float[] speed = defaults.Speed;
        for (int i = 0; i < health.Length; i++)
        {
            if (Health != null && i < Health.Length)
                health[i] = Mathf.Clamp(Health[i], health[i], GameBalance.MaxHealth[i]);
            if (Speed != null && i < Speed.Length && !float.IsNaN(Speed[i]) && !float.IsInfinity(Speed[i]))
                speed[i] = Mathf.Clamp(Speed[i], speed[i], GameBalance.MaxSpeed[i]);
        }
        Health = health;
        Speed = speed;
        Coins = Math.Max(0, Coins);
        BestScore = Math.Max(0, BestScore);
        Score = 0;
        if (SelectedCar < 0 || SelectedCar >= 6 || (SelectedCar != GameBalance.PremiumCarIndex && !IsCarUnlocked(SelectedCar))) SelectedCar = 0;
    }

    public void SetDefaultCars()
    {
        CarsUnlocked = new[] { true, false, false, false, false, false };
    }

    public bool IsCarUnlocked(int carIndex)
    {
        return CarsUnlocked != null && carIndex >= 0 && carIndex < CarsUnlocked.Length && CarsUnlocked[carIndex];
    }
}

public class Progress : MonoBehaviour
{
    public ProgressData Data;
    public static Progress Instance;


    public static void EnsureLoaded()
    {
        if (Instance != null) return;
        Progress existing = FindObjectOfType<Progress>();
        if (existing != null) existing.Initialize();
        else new GameObject("Progress").AddComponent<Progress>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Initialize();
    }

    private void Initialize()
    {
        if (Instance == this) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        if ((Data.SelectedCar != GameBalance.PremiumCarIndex || YG.YG2.isSDKEnabled) && !IsCarUnlocked(Data.SelectedCar)) Data.SelectedCar = 0;
        StaticData.valueToKeep = Data.SelectedCar;
    }

    public void SetToLeaderboardCall(int score)
    {
        PyaterkaPlatform.SubmitScore(score);
    }

    public void AddScore(int score)
    {
        Data.Score += Math.Max(0, score);
        if (Data.Score > Data.BestScore) SaveBestScore(Data.Score);
    }

    public void SaveBestScore(int bestScore)
    {
        Data.BestScore = Math.Max(Data.BestScore, bestScore);
        Save();
    }

    public void AddCoins(int value)
    {
        Data.Coins = Math.Max(0, Data.Coins + value);
        Save(true);
    }

    public bool TryBuyCar(int carIndex, int cost)
    {
        if (carIndex < 0 || carIndex >= Data.CarsUnlocked.Length || carIndex == GameBalance.PremiumCarIndex ||
            IsCarUnlocked(carIndex) || cost != GameBalance.Prices[carIndex] || Data.Coins < cost) return false;
        Data.Coins -= cost;
        Data.CarsUnlocked[carIndex] = true;
        Save(true);
        return true;
    }

    public bool IsCarUnlocked(int carIndex)
    {
        if (carIndex == GameBalance.PremiumCarIndex) return Data.LegacyPremium || PremiumCar.Owned;
        return Data.IsCarUnlocked(carIndex);
    }

    public bool TryImproveHealth(int carIndex)
    {
        if (!IsCarUnlocked(carIndex)) return false;
        int cost = GameBalance.HealthCost(carIndex, Data.Health[carIndex]);
        if (cost == 0 || Data.Coins < cost) return false;
        Data.Coins -= cost;
        Data.Health[carIndex]++;
        Save(true);
        return true;
    }

    public bool TryImproveSpeed(int carIndex)
    {
        if (!IsCarUnlocked(carIndex)) return false;
        int cost = GameBalance.SpeedCost(carIndex, Data.Speed[carIndex]);
        if (cost == 0 || Data.Coins < cost) return false;
        Data.Coins -= cost;
        Data.Speed[carIndex] = Mathf.Min(GameBalance.MaxSpeed[carIndex], Mathf.Round((Data.Speed[carIndex] + GameBalance.SpeedStep) * 10f) / 10f);
        Save(true);
        return true;
    }

    public bool TryRedeemPromo()
    {
        if (Data.PromoRedeemed) return false;
        Data.PromoRedeemed = true;
        Data.Coins += 50;
        Save(true);
        return true;
    }

    public void SelectCar(int carIndex)
    {
        if (!IsCarUnlocked(carIndex)) return;
        Data.SelectedCar = carIndex;
        StaticData.valueToKeep = carIndex;
        Save(true);
    }

    public int GetHealth(int carIndex) { return Data.Health[carIndex]; }
    public float GetSpeed(int carIndex) { return Data.Speed[carIndex]; }

    public void Flush()
    {
        Save(true);
    }

    public void ApplyLoadedData(ProgressData data)
    {
        data.FillEmptyValues();
        Data = data;
        if ((Data.SelectedCar != GameBalance.PremiumCarIndex || YG.YG2.isSDKEnabled) && !IsCarUnlocked(Data.SelectedCar)) Data.SelectedCar = 0;
        StaticData.valueToKeep = Data.SelectedCar;
    }

    private void Save(bool flush = false)
    {
        PlayerPrefs.SetString("Progress", JsonUtility.ToJson(Data));
        if (flush)
        {
            PlayerPrefs.Save();
            PyaterkaPlatform.Save(JsonUtility.ToJson(Data));
        }
    }

    private void Load()
    {
        try
        {
            Data = PlayerPrefs.HasKey("Progress")
                ? JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString("Progress")) : null;
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("Invalid progress data; loading defaults.");
        }
        if (Data == null) Data = ProgressData.GetEmptyProgressData();
        Data.FillEmptyValues();
        Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && Instance == this) Flush();
    }

    private void OnApplicationQuit()
    {
        if (Instance == this) Flush();
    }
}
