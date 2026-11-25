using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoinCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text amountCoins;
    [SerializeField] private TMP_Text amountHP;

    public static CoinCounter Instance;

    public void Awake()
    {
        Progress.EnsureLoaded();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateCounter();
    }

    public void UpdateCounter()
    {
        amountCoins.text = Progress.Instance.Data.Coins.ToString();
        if (amountHP != null && PlayerCar.Instance != null)
            amountHP.text = PlayerCar.Instance.Health.ToString();
    }
}
