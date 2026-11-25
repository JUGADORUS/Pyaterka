using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private Health _healthPrefab;
    [SerializeField] private float _mapSize;
    [SerializeField] private float timerMinHealth;
    [SerializeField] private float timerMaxHealth;
    public static HealthManager Instance;
    private readonly HashSet<Health> activeHealth = new HashSet<Health>();
    private float spawnTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetRun()
    {
        foreach (Health health in activeHealth)
        {
            if (health == null) continue;
            health.gameObject.SetActive(false);
            Destroy(health.gameObject);
        }
        activeHealth.Clear();
        spawnTimer = Random.Range(timerMinHealth, timerMaxHealth);
    }

    private void Update()
    {
        if (!MenuManager.GameActive || activeHealth.Count >= 10) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = Mathf.Max(0.1f, Random.Range(timerMinHealth, timerMaxHealth));
        // A crowded map must not block the main thread with an infinite search.
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 position = new Vector3(Random.Range(-_mapSize, _mapSize), 1.5f,
                Random.Range(-_mapSize, _mapSize));
            if (Physics.CheckSphere(position, 0.75f)) continue;
            if (_healthPrefab != null)
                activeHealth.Add(Instantiate(_healthPrefab, position, Quaternion.identity));
            break;
        }
    }

    public void CollectHealth(Health pickup)
    {
        activeHealth.Remove(pickup);
        PlayerCar player = PlayerCar.Instance;
        if (player == null) return;
        player.Health = Mathf.Min(player.Health + 1, player.maxHealth);
        if (CoinManager.Instance != null) CoinManager.Instance.ShowHP();
    }
}
