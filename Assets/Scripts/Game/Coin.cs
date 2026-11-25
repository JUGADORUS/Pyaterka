using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private GameObject CoinCollectEffect;
    public int Value = 2;
    private bool collected;

    public bool Collect()
    {
        if (collected || !MenuManager.GameActive) return false;
        collected = true;
        CoinManager.Instance.CollectCoin(this);
        if (CoinCollectEffect != null)
            Instantiate(CoinCollectEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
        return true;
    }

    private void Update()
    {
        transform.Rotate(0, 180 * Time.deltaTime, 0);
    }
}
