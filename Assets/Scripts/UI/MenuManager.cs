using UnityEngine;
using YG;

public class MenuManager : MonoBehaviour
{
    [SerializeField] public GameObject MenuUI;
    public static bool GameActive;
    public static MenuManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        GameActive = false;
        Progress.EnsureLoaded();
    }

    private void Start()
    {
        MenuUI.SetActive(true);
    }

    public void RequestStart()
    {
        if (GameActive) return;
        if (AdvManager.Instance != null) AdvManager.Instance.QueueAd();
        else StartGame();
    }

    public void StartGame()
    {
        if (GameActive) return;
        foreach (TimerForPolice timer in FindObjectsOfType<TimerForPolice>()) timer.ResetDifficulty();
        foreach (PoliceSpawner spawner in FindObjectsOfType<PoliceSpawner>()) spawner.ResetSpawner();
        if (CoinManager.Instance != null) CoinManager.Instance.ResetRun();
        if (HealthManager.Instance != null) HealthManager.Instance.ResetRun();
        if (PlayerCar.Instance != null) PlayerCar.Instance.ResetRun();
        if (BreakableObjectManager.Instance != null) BreakableObjectManager.Instance.TurnOnBreakableObjects();
        if (AudioManager.Instance != null) AudioManager.Instance.Unmute();
        GameActive = true;
        if (YG2.isSDKEnabled) YG2.GameplayStart();
        MenuUI.SetActive(false);
        if (TipsHowToControl.Instance != null) TipsHowToControl.Instance.TurnOnTips();
    }

    public void EndRun()
    {
        if (!GameActive) return;
        GameActive = false;
        if (YG2.isSDKEnabled) YG2.GameplayStop();
        if (PlayerCar.Instance != null) PlayerCar.Instance.StopMotion();
        foreach (Police police in FindObjectsOfType<Police>())
        {
            police.gameObject.SetActive(false);
            Destroy(police.gameObject);
        }
        Progress.Instance.SetToLeaderboardCall(Progress.Instance.Data.BestScore);
        Progress.Instance.Flush();
        if (TipsHowToControl.Instance != null) TipsHowToControl.Instance.TurnOffTips();
    }

    public void TurnOnMenu()
    {
        EndRun();
        MenuUI.SetActive(true);
        if (CoinManager.Instance != null) CoinManager.Instance.ResetRun();
        if (HealthManager.Instance != null) HealthManager.Instance.ResetRun();
        if (BreakableObjectManager.Instance != null) BreakableObjectManager.Instance.TurnOnBreakableObjects();
    }

    private void OnDestroy()
    {
        if (Instance == this) GameActive = false;
    }
}
