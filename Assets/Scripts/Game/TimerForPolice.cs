using UnityEngine;
public class TimerForPolice : MonoBehaviour
{
    private float timer;
    public static int ActiveLimit { get; private set; } = 3;
    public void ResetDifficulty() { timer = 0f; Apply(); }
    private void FixedUpdate() { if (!MenuManager.GameActive) return; timer += Time.fixedDeltaTime; Apply(); }
    private void Apply()
    {
        Police.speed = GameBalance.PoliceSpeed(timer);
        PoliceSpawner.minTime = GameBalance.SpawnInterval(timer);
        PoliceSpawner.maxTime = PoliceSpawner.minTime + 2f;
        ActiveLimit = GameBalance.PoliceLimit(timer);
    }
}
