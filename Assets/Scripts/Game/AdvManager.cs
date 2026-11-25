using UnityEngine;
using YG;

public class AdvManager : MonoBehaviour
{
    [SerializeField] private float _adTimer;
    private bool _adQueued;
    private bool waitingForAd;
    private bool adOpened;
    private float requestTime;
    public static AdvManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        YG2.onOpenInterAdv += OnAdOpened;
        YG2.onCloseInterAdv += OnAdClosed;
        YG2.onErrorInterAdv += OnAdClosed;
    }

    private void OnDestroy()
    {
        YG2.onOpenInterAdv -= OnAdOpened;
        YG2.onCloseInterAdv -= OnAdClosed;
        YG2.onErrorInterAdv -= OnAdClosed;
    }

    private void Update()
    {
        _adTimer += Time.unscaledDeltaTime;
        // A skipped SDK request may not emit a close callback.
        if (waitingForAd && !adOpened && Time.realtimeSinceStartup - requestTime > 15f)
            OnAdClosed();
        if (!_adQueued || waitingForAd || !YG2.isSDKEnabled || YG2.nowAdsShow || YG2.isPauseGame) return;
        _adQueued = false;
        if (_adTimer >= 120f && YG2.isTimerAdvCompleted)
        {
            waitingForAd = true;
            adOpened = false;
            requestTime = Time.realtimeSinceStartup;
            YG2.InterstitialAdvShow();
            return;
        }
        MenuManager.Instance.StartGame();
    }

    public void QueueAd()
    {
        if (!MenuManager.GameActive && !waitingForAd) _adQueued = true;
    }

    public void OnAdOpened() { if (waitingForAd) adOpened = true; }
    public void OnAdClosed()
    {
        if (!waitingForAd) return;
        waitingForAd = false;
        adOpened = false;
        _adTimer = 0f;
        // The plugin restores pause/audio after its callback has completed.
        _adQueued = true;
    }
}
