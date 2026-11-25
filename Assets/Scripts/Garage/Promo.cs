using UnityEngine;
using TMPro;

public class Promo : MonoBehaviour
{
    [SerializeField] private TMP_InputField _InputField;
    [SerializeField] private string _promo;

    public void Ok()
    {
        if (BuildFlag.BuildValue != 69 || _InputField.text.Trim() != _promo ||
            !Progress.Instance.TryRedeemPromo()) return;
        _InputField.text = string.Empty;
        Podium.Instance.UpdateButtons();
        CoinCounter.Instance.UpdateCounter();
    }
}
