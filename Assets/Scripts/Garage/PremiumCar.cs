using System;
using UnityEngine;
using YG;
using YG.Utils.Pay;

public static class PremiumCar
{
    public const string ProductId = "pyaterka_premium_car";
    private const string SimulationKey = "Pyaterka_PremiumCar_Simulation";
    public static event Action Changed;
    public static bool Buying { get; private set; }
    public static bool Failed { get; private set; }
    public static Purchase Product { get { return YG2.PurchaseByID(ProductId); } }
    public static bool Owned
    {
        get
        {
#if UNITY_EDITOR
            return PlayerPrefs.GetInt(SimulationKey, 0) == 1;
#else
            return Array.IndexOf(YG2.ownedProductIds, ProductId) >= 0;
#endif
        }
    }
    public static bool CanBuy { get { return !Buying && !Owned && YG2.isSDKEnabled && !YG2.nowAdsShow && Product != null && !string.IsNullOrEmpty(Product.price); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Buying = false; Failed = false; Changed = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Subscribe()
    {
        YG2.onGetPayments += Refresh;
        YG2.onGetSDKData += Refresh;
        YG2.onPurchaseSuccess += Succeeded;
        YG2.onPurchaseFailed += Failure;
    }
    public static void Buy()
    {
        if (!CanBuy || (Progress.Instance != null && Progress.Instance.Data.LegacyPremium)) return;
        Buying = true;
        Failed = false;
        Changed?.Invoke();
        YG2.BuyPayments(ProductId);
    }
    private static void Refresh() { if (Owned) Buying = false; Changed?.Invoke(); }
    private static void Succeeded(string id)
    {
        if (id != ProductId) return;
#if UNITY_EDITOR
        PlayerPrefs.SetInt(SimulationKey, 1);
        PlayerPrefs.Save();
#endif
        Buying = false;
        Failed = false;
        if (Progress.Instance != null) Progress.Instance.Flush();
        Changed?.Invoke();
    }
    private static void Failure(string id)
    {
        if (id != ProductId) return;
        Buying = false;
        Failed = true;
        Changed?.Invoke();
    }
}
