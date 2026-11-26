using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuyButton : MonoBehaviour
{
    [SerializeField] private TMP_Text costText;
    [SerializeField] private CarData carData;
    [SerializeField] private TMP_Text buyText;
    [SerializeField] private Image leftCurrencyIcon;
    [SerializeField] private Image rightCurrencyIcon;
    private TMP_Text paymentNote;
    private static readonly Color PremiumPurple = new Color(0.48f, 0.12f, 0.8f, 1f);
    private static readonly Color CoinGreen = new Color(0.19f, 0.85f, 0f, 1f);

    private int _cost;
    private int _carIndex;

    public static BuyButton Instance;

    public void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        PremiumCar.Changed += Refresh;
        PyaterkaLocalization.Changed += Refresh;
    }
    private void OnDisable()
    {
        PremiumCar.Changed -= Refresh;
        PyaterkaLocalization.Changed -= Refresh;
    }
    private void Refresh()
    {
        UpdateCost((CarType)_carIndex);
        if (Podium.Instance != null) Podium.Instance.UpdateButtons();
    }

    public void UpdateCost(CarType carType)
    {
        _carIndex = (int)carType;
        _cost = GameBalance.Prices[_carIndex];
        bool premium = _carIndex == GameBalance.PremiumCarIndex;
        ApplyStyle(premium);
        costText.SetText(premium
            ? (PremiumCar.Buying ? PyaterkaLocalization.Text("Buying") : PremiumCar.Product != null ? PremiumCar.Product.price : PyaterkaLocalization.Text("Unavailable"))
            : _cost.ToString());
        Button button = GetComponent<Button>();
        if (button != null) button.interactable = premium ? PremiumCar.CanBuy : Progress.Instance.Data.Coins >= _cost;
    }

    private void ApplyStyle(bool premium)
    {
        var card = (RectTransform)transform;
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.73f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = new Vector2(440f, premium ? 300f : 240f);
        Image background = GetComponent<Image>();
        if (background != null) background.color = premium ? PremiumPurple : CoinGreen;
        costText.color = premium ? Color.white : new Color(0.29f, 1f, 0.33f, 1f);
        costText.alignment = TextAlignmentOptions.Center;
        costText.enableWordWrapping = false;
        costText.enableAutoSizing = true;
        costText.fontSizeMin = 38f;
        costText.fontSizeMax = 76f;
        costText.rectTransform.anchoredPosition = new Vector2(0f, premium ? 10f : -35f);
        costText.rectTransform.sizeDelta = new Vector2(premium ? 360f : 180f, 64f);
        costText.raycastTarget = false;
        if (buyText != null)
        {
            buyText.color = costText.color;
            buyText.alignment = TextAlignmentOptions.Center;
            buyText.enableAutoSizing = true;
            buyText.fontSizeMin = 36f;
            buyText.fontSizeMax = 76f;
            buyText.rectTransform.anchoredPosition = new Vector2(0f, 80f);
            buyText.rectTransform.sizeDelta = new Vector2(360f, 64f);
            buyText.raycastTarget = false;
        }
        StyleCurrencyIcon(leftCurrencyIcon, -135f, !premium);
        StyleCurrencyIcon(rightCurrencyIcon, 135f, !premium);
        if (paymentNote == null)
        {
            var obj = new GameObject("Yandex currency label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(transform, false);
            paymentNote = obj.GetComponent<TMP_Text>();
            paymentNote.font = costText.font;
            paymentNote.color = new Color(0.94f, 0.86f, 1f, 1f);
            paymentNote.alignment = TextAlignmentOptions.Center;
            paymentNote.enableAutoSizing = true;
            paymentNote.fontSizeMin = 38f;
            paymentNote.fontSizeMax = 42f;
            paymentNote.enableWordWrapping = true;
            paymentNote.raycastTarget = false;
            paymentNote.rectTransform.anchorMin = paymentNote.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            paymentNote.rectTransform.anchoredPosition = new Vector2(0f, -77f);
            paymentNote.rectTransform.sizeDelta = new Vector2(360f, 80f);
        }
        paymentNote.gameObject.SetActive(premium);
        paymentNote.text = PyaterkaLocalization.Text("Yandex currency");
    }

    private static void StyleCurrencyIcon(Image icon, float x, bool visible)
    {
        if (icon == null) return;
        icon.gameObject.SetActive(visible);
        icon.raycastTarget = false;
        icon.rectTransform.anchoredPosition = new Vector2(x, -35f);
        icon.rectTransform.sizeDelta = new Vector2(52f, 52f);
    }

    public void BuyCar()
    {
        if (_carIndex == GameBalance.PremiumCarIndex) PremiumCar.Buy();
        else Progress.Instance.TryBuyCar(_carIndex, _cost);
        Podium.Instance.UpdateButtons();
        CoinCounter.Instance.UpdateCounter();
    }
}
