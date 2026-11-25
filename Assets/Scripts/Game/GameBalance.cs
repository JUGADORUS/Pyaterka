using UnityEngine;

// Slots match CarData and the six positions on the garage podium.
public static class GameBalance
{
    public const int Version = 2;
    public const int PremiumCarIndex = 2;
    public const float SpeedStep = 0.2f;
    public static readonly int[] Prices = { 0, 100, 0, 300, 45, 180 };
    public static readonly int[] BaseHealth = { 3, 4, 12, 6, 5, 5 };
    public static readonly int[] MaxHealth = { 6, 8, 12, 10, 10, 9 };
    public static readonly float[] BaseSpeed = { 15f, 16.2f, 18f, 17f, 15.6f, 16.6f };
    public static readonly float[] MaxSpeed = { 16.6f, 18f, 18f, 18f, 17.2f, 17.8f };
    public static readonly float[] TurnSpeed = { 135f, 145f, 135f, 138f, 128f, 140f };

    public static int HealthCost(int car, int health)
    {
        return health >= MaxHealth[car] ? 0 : 12 + Mathf.Max(0, health - BaseHealth[car]) * 8;
    }
    public static int SpeedCost(int car, float speed)
    {
        int level = Mathf.Max(0, Mathf.RoundToInt((speed - BaseSpeed[car]) / SpeedStep));
        return speed >= MaxSpeed[car] - 0.001f ? 0 : 6 + level * 3;
    }
    // Guaranteed income for surviving, independent of random pickup placement.
    public static int SurvivalIncome(int seconds)
    {
        int result = 0;
        for (int tick = 1; tick <= Mathf.Max(0, seconds) / 10; tick++)
            result += 4 + Mathf.Min(3, tick / 6);
        return result;
    }
    public static float PoliceSpeed(float seconds)
    {
        return 14.3f + 4.7f * (1f - Mathf.Exp(-Mathf.Max(0f, seconds) / 150f));
    }
    public static float SpawnInterval(float seconds)
    {
        return Mathf.Lerp(12f, 4.5f, 1f - Mathf.Exp(-Mathf.Max(0f, seconds) / 150f));
    }
    public static int PoliceLimit(float seconds) { return Mathf.Min(16, 3 + Mathf.FloorToInt(Mathf.Max(0f, seconds) / 25f)); }
}
