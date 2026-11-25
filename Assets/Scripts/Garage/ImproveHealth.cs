using UnityEngine;
using TMPro;

public class ImproveHealth : MonoBehaviour
{
    [SerializeField] private CarData carData;
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] public TMP_Text _maxHealth;
    [SerializeField] private TMP_Text _priceText;
    private int _carIndex;
    public static ImproveHealth Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ImproveHealthCharacteristics()
    {
        Progress.Instance.TryImproveHealth(_carIndex);
        Podium.Instance.UpdateButtons();
        CoinCounter.Instance.UpdateCounter();
    }

    public void UpdateHealthCharacteristics(CarType carType)
    {
        _carIndex = (int)carType;
        int health = Progress.Instance.GetHealth(_carIndex);
        _healthText.SetText(health.ToString());
        int cost = GameBalance.HealthCost(_carIndex, health);
        bool showUpgrade = Progress.Instance.IsCarUnlocked(_carIndex) && _carIndex != GameBalance.PremiumCarIndex;
        _maxHealth.gameObject.SetActive(showUpgrade && cost == 0);
        if (_priceText != null)
        {
            _priceText.gameObject.SetActive(showUpgrade && cost > 0);
            _priceText.text = showUpgrade && cost > 0 ? cost + "$" : "";
        }
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null) button.interactable = showUpgrade && cost > 0 && Progress.Instance.Data.Coins >= cost;
    }
}
