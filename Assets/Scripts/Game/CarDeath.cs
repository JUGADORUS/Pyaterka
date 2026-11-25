using System.Collections;
using UnityEngine;

public class CarDeath : MonoBehaviour
{
    [SerializeField] private GameObject _dieEffect;
    private int health = 3;
    private bool dying;
    private float nextDamageTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (dying || !MenuManager.GameActive) return;
        PlayerCar player = GetComponent<PlayerCar>();
        bool fatal = collision.gameObject.GetComponentInParent<UnbreakableObject>() != null;
        bool damaging = collision.gameObject.GetComponentInParent<BreakableObject>() != null ||
            collision.gameObject.GetComponentInParent<Police>() != null;
        if (player == null && collision.gameObject.GetComponentInParent<PlayerCar>() != null)
            fatal = true;
        if (!fatal && !damaging) return;

        if (player != null)
        {
            if (Time.time < nextDamageTime) return;
            nextDamageTime = Time.time + 0.75f;
            if ((fatal || player.Health <= 1) && player.UseCrashShield())
            {
                if (Vignette.Instance != null) Vignette.Instance.Flash();
                if (CoinManager.Instance != null) CoinManager.Instance.ShowHP();
                return;
            }
            player.Health = fatal ? 0 : Mathf.Max(0, player.Health - 1);
            if (CoinManager.Instance != null) CoinManager.Instance.ShowHP();
            if (Vignette.Instance != null) Vignette.Instance.Flash();
            if (player.Health == 0) Die(player);
        }
        else if (fatal || --health <= 0)
        {
            Die(null);
        }
    }

    private void Die(PlayerCar player)
    {
        if (dying) return;
        dying = true;
        if (_dieEffect != null) Instantiate(_dieEffect, transform.position, Quaternion.identity);
        if (player == null)
        {
            if (MenuManager.GameActive && GetComponent<Police>() != null && CoinManager.Instance != null)
            {
                Vector3 coinPosition = new Vector3(transform.position.x, 1.5f, transform.position.z);
                Coin drop = CoinManager.Instance.SpawnCoin(coinPosition);
                if (drop != null) drop.Value = 3;
            }
            Destroy(gameObject);
            return;
        }
        MenuManager.Instance.EndRun();
        StartCoroutine(GoBack(player));
    }

    private IEnumerator GoBack(PlayerCar player)
    {
        Rigidbody body = GetComponent<Rigidbody>();
        Vector3 originalScale = transform.localScale;
        body.isKinematic = true;
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(2f);
        transform.SetPositionAndRotation(MenuManager.Instance.transform.position,
            MenuManager.Instance.transform.rotation);
        transform.localScale = originalScale;
        body.isKinematic = false;
        player.ResetRun();
        dying = false;
        MenuManager.Instance.TurnOnMenu();
    }
}
