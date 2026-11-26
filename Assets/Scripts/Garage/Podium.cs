using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Podium : MonoBehaviour
{
    private float _rotationAmount = 60f;
    private float _currentRotation = 0f;
    public int _currentIndex = 0;

    [SerializeField] private Transform _podium;
    [SerializeField] private AnimationCurve _rotationCurve;
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _confirm;
    [SerializeField] private Button _improveHealth;
    [SerializeField] private Button _improveSpeed;

    public static Podium Instance;
    private bool rotating;
    private TMPro.TMP_Text offerText;

    public void Awake()
    {
        Progress.EnsureLoaded();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Start()
    {
        _currentIndex = Progress.Instance.Data.SelectedCar;
        _currentRotation = _podium.eulerAngles.y + _currentIndex * _rotationAmount;
        _podium.rotation = Quaternion.Euler(0, _currentRotation, 0);
        var canvas = FindObjectOfType<Canvas>();
        var template = GetComponentInChildren<TMPro.TMP_Text>(true);
        if (template == null) template = FindObjectOfType<TMPro.TMP_Text>();
        if (canvas != null && template != null)
        {
            var obj = new GameObject("Car offer", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            obj.transform.SetParent(canvas.transform, false);
            offerText = obj.GetComponent<TMPro.TMP_Text>();
            offerText.font = template.font;
            offerText.alignment = TMPro.TextAlignmentOptions.Center;
            offerText.enableAutoSizing = true;
            offerText.fontSizeMin = 38f;
            offerText.fontSizeMax = 42f;
            offerText.raycastTarget = false;
            var rect = offerText.rectTransform;
            rect.anchorMin = new Vector2(0.20f, 0.86f);
            rect.anchorMax = new Vector2(0.80f, 0.99f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        PremiumCar.Changed += UpdateButtons;
        PyaterkaLocalization.Changed += UpdateButtons;
        UpdateButtons();
    }

    private void OnDestroy()
    {
        PremiumCar.Changed -= UpdateButtons;
        PyaterkaLocalization.Changed -= UpdateButtons;
    }

    public int GetIndex()
    {
        return (int)((CarType)_currentIndex);
    }

    public void RotateCircleToLeft()
    {
        if (rotating) return;
        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = 5;
        StartCoroutine(RotationCircle(-_rotationAmount));
        _leftButton.interactable = false;
        _rightButton.interactable = false;
        BuyButton.Instance.UpdateCost((CarType) _currentIndex);
        ImproveHealth.Instance.UpdateHealthCharacteristics((CarType)_currentIndex);
        ImproveSpeed.Instance.UpdateSpeedCharacteristics((CarType)_currentIndex);
    }

    public void RotateCircleToRight()
    {
        if (rotating) return;
        _currentIndex++;
        if (_currentIndex > 5) _currentIndex = 0;
        StartCoroutine(RotationCircle(_rotationAmount));
        _rightButton.interactable = false;
        _leftButton.interactable = false;
        BuyButton.Instance.UpdateCost((CarType)_currentIndex);
        ImproveHealth.Instance.UpdateHealthCharacteristics((CarType)_currentIndex);
        ImproveSpeed.Instance.UpdateSpeedCharacteristics((CarType)_currentIndex);
    }

    private IEnumerator RotationCircle(float rotationAmount)
    {
        rotating = true;
        UpdateButtons();
        for (float i = 0f; i <= 1f; i += Time.deltaTime)
        {
            float t;
            t = _rotationCurve.Evaluate(i);
            float newRotation = Mathf.Lerp(_currentRotation, _currentRotation + rotationAmount, t);
            _podium.transform.rotation = Quaternion.Euler(0, newRotation, 0);
            yield return null;
        }
        _currentRotation += rotationAmount;
        _podium.rotation = Quaternion.Euler(0, _currentRotation, 0);
        rotating = false;
        _leftButton.interactable = true;
        _rightButton.interactable = true;
    }

    public void UpdateButtons()
    {
        BuyButton.Instance.UpdateCost((CarType)_currentIndex);
        bool isUnlocked = false;
        if (Progress.Instance.IsCarUnlocked(_currentIndex))
        {
            isUnlocked = Progress.Instance.IsCarUnlocked(_currentIndex);
        }

        bool upgradeable = isUnlocked && _currentIndex != GameBalance.PremiumCarIndex;
        _improveSpeed.gameObject.SetActive(upgradeable);
        _improveHealth.gameObject.SetActive(upgradeable);
        _confirm.gameObject.SetActive(isUnlocked);
        BuyButton.Instance.gameObject.SetActive(!isUnlocked);
        ImproveSpeed.Instance.gameObject.SetActive(upgradeable);
        ImproveHealth.Instance.gameObject.SetActive(upgradeable);
        ImproveHealth.Instance.UpdateHealthCharacteristics((CarType)_currentIndex);
        ImproveSpeed.Instance.UpdateSpeedCharacteristics((CarType)_currentIndex);
        if (offerText != null)
        {
            // Leave space for upgrade prices to the left of the description.
            offerText.rectTransform.anchorMin = new Vector2(upgradeable ? 0.34f : 0.20f, 0.86f);
            offerText.rectTransform.anchorMax = new Vector2(upgradeable ? 0.82f : 0.80f, 0.99f);
            offerText.text = _currentIndex == GameBalance.PremiumCarIndex
                ? PyaterkaLocalization.Text(PremiumCar.Failed ? "Purchase failed" : "Premium perks")
                : PyaterkaLocalization.Text("Survival income");
        }
    }
}


