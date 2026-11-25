using System;
using UnityEngine;
using YG;

public class PyaterkaPlatform : MonoBehaviour
{
    public static bool CloudAvailable;
    private static bool cloudBound;
    private static string pendingJson;
    private static bool dirty;
    private float nextSave;
    private const string AccountKey = "Pyaterka_ProgressAccount";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        cloudBound = false;
        dirty = false;
        pendingJson = null;
        new GameObject("PyaterkaPlatform").AddComponent<PyaterkaPlatform>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        YG2.onGetSDKData += LoadPlatformProgress;
        if (YG2.isSDKEnabled) LoadPlatformProgress();
    }

    private void OnDestroy() { YG2.onGetSDKData -= LoadPlatformProgress; }

    private void LoadPlatformProgress()
    {
        Progress.EnsureLoaded();
#if UNITY_EDITOR
        // Editor simulation has its own plugin save file; keep the existing local test profile.
        CloudAvailable = false;
        return;
#else
        if (!CloudAvailable || !YG2.player.auth || string.IsNullOrEmpty(YG2.player.id)) return;
        if (cloudBound) return;
        cloudBound = true;
        string account = YG2.player.id;
        string cachedAccount = PlayerPrefs.GetString(AccountKey, "");
        ProgressData data = null;
        if (!string.IsNullOrEmpty(YG2.saves.pyaterkaProgress))
        {
            try { data = JsonUtility.FromJson<ProgressData>(YG2.saves.pyaterkaProgress); }
            catch (ArgumentException) { Debug.LogWarning("Invalid cloud progress; keeping local recovery data."); }
        }
        else if (cachedAccount.Length > 0 && cachedAccount != account)
        {
            // Do not transfer another account's cached purchases/currency to this account.
            data = ProgressData.GetEmptyProgressData();
        }
        bool reload = data != null && JsonUtility.ToJson(data) != JsonUtility.ToJson(Progress.Instance.Data);
        if (data != null) Progress.Instance.ApplyLoadedData(data);
        PlayerPrefs.SetString(AccountKey, account);
        Progress.Instance.Flush();
        if (reload) StartCoroutine(ReloadMenu());
#endif
    }

    private System.Collections.IEnumerator ReloadMenu()
    {
        yield return null;
        while (MenuManager.GameActive) yield return null;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0) UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    private void Update()
    {
        if (dirty && Time.realtimeSinceStartup >= nextSave)
        {
            FlushCloud();
            nextSave = Time.realtimeSinceStartup + 5f;
        }
    }

    private void OnApplicationPause(bool paused) { if (paused) FlushCloud(); }
    private void OnApplicationQuit() { FlushCloud(); }

    private static void FlushCloud()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!dirty || !cloudBound || !CloudAvailable || !YG2.isSDKEnabled || !YG2.player.auth) return;
        YG2.saves.pyaterkaProgress = pendingJson;
        dirty = false;
        YG2.SaveProgress();
#endif
    }

    public static void Save(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!cloudBound || !CloudAvailable || !YG2.isSDKEnabled || !YG2.player.auth) return;
        pendingJson = json;
        dirty = true;
#endif
    }

    public static void SubmitScore(int score)
    {
        if (YG2.isSDKEnabled && score > 0) YG2.SetLeaderboard("BestLivingTime", score);
    }
}
