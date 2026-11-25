using UnityEngine;

public class PoliceSpawner : MonoBehaviour
{
    [SerializeField] private Police PoliceCar;
    private float time;
    private float nextSpawn;
    public static float minTime = 10f;
    public static float maxTime = 12f;

    public void ResetSpawner()
    {
        time = 0f;
        nextSpawn = Random.Range(minTime, maxTime);
    }

    private void FixedUpdate()
    {
        if (!MenuManager.GameActive) return;
        if (nextSpawn <= 0f) ResetSpawner();
        time += Time.fixedDeltaTime;
        if (time >= Mathf.Min(nextSpawn, maxTime))
        {
            if (PoliceCar != null && Police.ActiveCount < TimerForPolice.ActiveLimit) Instantiate(PoliceCar, transform.position, Quaternion.identity);
            ResetSpawner();
        }
    }
}
