using UnityEngine;
using TMPro;

public class ImproveSpeed : MonoBehaviour
{
    [SerializeField] private CarData carData;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] public TMP_Text _maxSpeed;
    [SerializeField] private TMP_Text _priceText;
    private int _carIndex;
    public static ImproveSpeed Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ImproveSpeedCharacteristics()
    {
        Progress.Instance.TryImproveSpeed(_carIndex);
        Podium.Instance.UpdateButtons();
        CoinCounter.Instance.UpdateCounter();
    }

    public void UpdateSpeedCharacteristics(CarType carType)
    {
        _carIndex = (int)carType;
        float speed = Progress.Instance.GetSpeed(_carIndex);
        _speedText.SetText(speed.ToString("0.0"));
        int cost = GameBalance.SpeedCost(_carIndex, speed);
        bool showUpgrade = Progress.Instance.IsCarUnlocked(_carIndex) && _carIndex != GameBalance.PremiumCarIndex;
        _maxSpeed.gameObject.SetActive(showUpgrade && cost == 0);
        if (_priceText != null)
        {
            _priceText.gameObject.SetActive(showUpgrade && cost > 0);
            _priceText.text = showUpgrade && cost > 0 ? cost + "$" : "";
        }
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null) button.interactable = showUpgrade && cost > 0 && Progress.Instance.Data.Coins >= cost;
    }
}
