using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private GameObject HealthCollectEffect;
    private bool collected;

    public void Collect()
    {
        if (collected || !MenuManager.GameActive || PlayerCar.Instance == null ||
            PlayerCar.Instance.Health >= PlayerCar.Instance.maxHealth) return;
        collected = true;
        HealthManager.Instance.CollectHealth(this);
        if (HealthCollectEffect != null)
            Instantiate(HealthCollectEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.Rotate(0, 180 * Time.deltaTime, 0);
    }
}
