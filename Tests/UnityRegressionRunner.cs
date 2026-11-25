using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using YG;
using TMPro;

public class UnityRegressionRunner : MonoBehaviour
{
    int checks, errors;
    void Awake() { Application.logMessageReceived += Log; }
    void Log(string text, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
    void Check(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); checks++; Debug.Log("PASS: " + name); }
    static void Set(object obj, string key, object value)
    {
        Type type = obj.GetType(); FieldInfo field = null;
        while (field == null && type != null) { field = type.GetField(key, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); type = type.BaseType; }
        field.SetValue(obj,value);
    }
    static object Get(object obj, string key) { return obj.GetType().GetField(key, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(obj); }
    static void Call(object obj, string method, params object[] args) { obj.GetType().GetMethod(method, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(obj,args); }
    IEnumerator Start()
    {
        var tests = Run();
        while (true)
        {
            object next;
            try { if (!tests.MoveNext()) break; next = tests.Current; }
            catch(Exception e) { Debug.LogException(e); Finish(false); yield break; }
            yield return next;
        }
        Finish(errors == 0);
    }
    void Finish(bool ok) { File.WriteAllText("results.txt", "passed="+ok+" checks="+checks+" errors="+errors); EditorApplication.Exit(ok?0:1); }
    IEnumerator Run()
    {
        yield return null;
        Check(YG2.isSDKEnabled, "PluginYG initialized");
        Check(PremiumCar.Product != null && PremiumCar.Product.priceValue == "99", "99 YAN premium product configured");
        var data = new ProgressData { Coins = -5, Health = new[]{0}, Speed = new[]{0f}, CarsUnlocked = new[]{false}, SelectedCar = 99 };
        data.FillEmptyValues();
        Check(data.Coins==0 && data.IsCarUnlocked(0) && !data.IsCarUnlocked(99), "legacy save migration");
        Check(data.Health[2]==12 && data.SelectedCar==0, "base stats preserved");
        var label = new GameObject("LocalizedLabel").AddComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.text = "Купить";
        typeof(PyaterkaLocalization).GetMethod("BindScene", BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,
            new object[]{UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single});
        foreach(var pair in new[]{new[]{"en","Buy"},new[]{"tr","Satın al"},new[]{"es","Comprar"},new[]{"pt","Comprar"},new[]{"ru","Купить"}})
        {
            YG2.SwitchLanguage(pair[0]);
            Check(label.text==pair[1], "SDK language event updates scene " + pair[0]);
            Check(PyaterkaLocalization.Format("Best: {0}",25).Contains("25"), "dynamic localized counter " + pair[0]);
            Check(!PyaterkaLocalization.Text("Instructions").Contains("Instructions"), "tutorial translated " + pair[0]);
        }
        YG2.SwitchLanguage("pt-BR"); Check(label.text=="Comprar", "regional Portuguese");
        YG2.SwitchLanguage("de"); Check(label.text=="Buy", "unsupported language uses English");
        YG2.SwitchLanguage("ru");
        string nested = JsonUtility.ToJson(new SavesYG { pyaterkaProgress=JsonUtility.ToJson(data) });
        string encoded = "[" + "\"" + nested.Replace("\\","\\\\").Replace("\"","\\\"") + "\"" + "]";
        YG.Insides.YGInsides.SetLoadSaves(encoded);
        Check(JsonUtility.FromJson<ProgressData>(YG2.saves.pyaterkaProgress).Health[2]==12, "nested cloud JSON round trip");
        Progress.EnsureLoaded(); var progress=Progress.Instance; progress.Data=ProgressData.GetEmptyProgressData(); progress.Data.Coins=500;
        Check(!progress.TryImproveSpeed(1), "locked upgrade rejected");
        Check(progress.TryBuyCar(1,100)&&progress.Data.Coins==400, "purchase");
        Check(!progress.TryBuyCar(1,100)&&progress.Data.Coins==400, "duplicate purchase rejected");
        Check(progress.TryImproveSpeed(1)&&Mathf.Approximately(progress.GetSpeed(1),16.4f), "speed upgrade");
        progress.Data.Speed[1]=18f; Check(!progress.TryImproveSpeed(1), "speed cap");
        progress.Data.Health[1]=7; Check(progress.TryImproveHealth(1)&&!progress.TryImproveHealth(1), "health cap");
        progress.SelectCar(1); Check(progress.Data.SelectedCar==1, "selected car persisted");
        Check(progress.TryRedeemPromo()&&!progress.TryRedeemPromo(), "promo once");
        var roundTrip=JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString("Progress"));
        Check(roundTrip.SelectedCar==1&&roundTrip.PromoRedeemed, "local progress round trip");
        progress.Data.Coins=10000;
        Check(!progress.TryBuyCar(GameBalance.PremiumCarIndex,0), "premium cannot be bought with game currency");
        Check(!progress.TryBuyCar(4,0), "forged car price rejected");
        var legacy = new ProgressData { BalanceVersion=0, CarsUnlocked=new[]{true,false,true,false,false,false}, Health=new[]{10,5,100,6,4,5}, Speed=new[]{18f,16f,18f,17.5f,15.4f,16.6f} };
        legacy.FillEmptyValues();
        Check(legacy.LegacyPremium&&legacy.Health[2]==12&&legacy.Health[0]==6, "legacy ownership preserved and stats migrated");
        int refunded=legacy.Coins; legacy.FillEmptyValues();
        Check(legacy.Coins==refunded&&refunded>0,"obsolete upgrades refunded exactly once");
        progress.Data=ProgressData.GetEmptyProgressData();progress.Data.Coins=10000;
        foreach(int index in new[]{0,4,1,5,3})
        {
            if(index!=0) Check(progress.TryBuyCar(index,GameBalance.Prices[index]),"free car purchasable " + index);
            int guard=0;while(progress.TryImproveHealth(index)){if(++guard>20)throw new Exception("health cap missing");}
            guard=0;while(progress.TryImproveSpeed(index)){if(++guard>30)throw new Exception("speed cap missing");}
            Check(progress.GetHealth(index)==GameBalance.MaxHealth[index]&&Mathf.Approximately(progress.GetSpeed(index),GameBalance.MaxSpeed[index]),"free car reaches max stats " + index);
        }
        int completionCost=10000-progress.Data.Coins;
        Check(completionCost==1680,"all free cars and upgrades cost 1680");
        Check(GameBalance.SurvivalIncome(120)>=GameBalance.Prices[4],"first car earned from two minutes survival without pickups");
        Check(GameBalance.SurvivalIncome(300)>GameBalance.SurvivalIncome(120)*2,"longer survival improves earnings");
        float previousSpeed=0;float previousInterval=100;
        for(int second=0;second<=1800;second+=5)
        {
            float speed=GameBalance.PoliceSpeed(second),interval=GameBalance.SpawnInterval(second);
            if(speed<previousSpeed||interval>previousInterval||GameBalance.PoliceLimit(second)>16)throw new Exception("invalid difficulty curve");
            previousSpeed=speed;previousInterval=interval;
        }
        Check(previousSpeed>GameBalance.MaxSpeed[3],"late police pressure exceeds fully upgraded cars");
        progress.Data=ProgressData.GetEmptyProgressData(); progress.SelectCar(0);
        var menu=new GameObject("MenuManager").AddComponent<MenuManager>(); menu.MenuUI=new GameObject("MenuUI");
        var coins=new GameObject("Coins").AddComponent<CoinManager>(); var health=new GameObject("Health").AddComponent<HealthManager>();
        var recordLabel=new GameObject("RecordLabel").AddComponent<TextMeshProUGUI>();
        recordLabel.font=label.font;
        Set(coins,"bestScore",recordLabel);
        progress.Data.BestScore=23;
        foreach(var pair in new[]{new[]{"ru","Рекорд: 23"},new[]{"en","Best: 23"},new[]{"tr","Rekor: 23"},new[]{"es","Récord: 23"},new[]{"pt","Recorde: 23"}})
        {
            YG2.SwitchLanguage(pair[0]);
            Check(recordLabel.text==pair[1], "actual HUD record updates on language switch " + pair[0]);
            Call(coins,"RefreshCounters");
            Check(recordLabel.text==pair[1], "HUD refresh preserves language " + pair[0]);
        }
        YG2.SwitchLanguage("en");
        progress.Data.BestScore=24;
        Call(coins,"RefreshCounters");
        Check(recordLabel.text=="Best: 24", "new record keeps translated caption");
        YG2.SwitchLanguage("ru");
        var cp=new GameObject("CoinPrototype").AddComponent<Coin>(); var hp=new GameObject("HealthPrototype").AddComponent<Health>();
        Set(coins,"_coinPrefab",cp); Set(health,"_healthPrefab",hp); Set(coins,"_mapSize",100f); Set(health,"_mapSize",100f);
        Set(coins,"timerMinCoins",10f); Set(coins,"timerMaxCoins",10f); Set(health,"timerMinHealth",10f); Set(health,"timerMaxHealth",10f);
        var car=new GameObject("Car"); car.SetActive(false); var body=car.AddComponent<Rigidbody>(); body.useGravity=false;
        var player=car.AddComponent<PlayerCar>(); var death=car.AddComponent<CarDeath>(); var selector=car.AddComponent<GiveCurrentCar>();
        var visual=new GameObject("Visual").transform; visual.SetParent(car.transform);
        var other=new GameObject("Other"); other.transform.SetParent(car.transform); var limo=new GameObject("Limo"); limo.transform.SetParent(car.transform);
        var left=new GameObject("LeftSmoke").AddComponent<ParticleSystem>(); var right=new GameObject("RightSmoke").AddComponent<ParticleSystem>();
        var carData=ScriptableObject.CreateInstance<CarData>(); carData.entries=new[]{new CarDataEntry{CarType=CarType.Hypercar1,CarPrefab=new GameObject("Model")}};
        Set(player,"indexOfCurrentCar",selector); Set(player,"_carTransform",car.transform); Set(player,"_visualTransform",visual); Set(player,"_visualParent",visual);
        Set(player,"_carRigidbody",body); Set(player,"_carPrefabs",carData); Set(player,"_otherCarData",other);
        Set(player,"_leftSmoke",left); Set(player,"_rightSmoke",right);
        car.SetActive(true); var difficulty=new GameObject("Difficulty").AddComponent<TimerForPolice>(); var spawner=new GameObject("Spawner").AddComponent<PoliceSpawner>();
        yield return null;
        menu.StartGame(); Check(MenuManager.GameActive&&player.Health==3,"run starts"); Call(player,"Update");
        Check(cp.Collect()&&!cp.Collect()&&progress.Data.Coins==2,"coin collected once");
        hp.Collect(); Check(player.Health==3,"full health capped"); player.Health=2; hp.Collect(); hp.Collect(); Check(player.Health==3,"healing once");
        for(int i=0;i<12;i++){Set(coins,"spawnTimer",0f);Call(coins,"Update");Set(health,"spawnTimer",0f);Call(health,"Update");}
        var coinSet=(HashSet<Coin>)Get(coins,"activeCoins"); var existing=new HashSet<Coin>(coinSet);
        Check(coinSet.Count==10&&((HashSet<Health>)Get(health,"activeHealth")).Count==10,"pickup limits");
        var police=new GameObject("Police"); police.transform.position=new Vector3(20f,0.5f,30f); police.AddComponent<Rigidbody>();police.AddComponent<Police>();
        var policeDeath=police.AddComponent<CarDeath>();Call(policeDeath,"Die",(object)null);Call(policeDeath,"Die",(object)null);
        Check(coinSet.Count==11,"police drops one coin beyond ambient limit"); Coin drop=null;foreach(var coin in coinSet)if(!existing.Contains(coin))drop=coin;
        Check(drop.transform.position==new Vector3(20f,1.5f,30f),"loot position");Check(drop.Collect()&&!drop.Collect(),"loot collected once");
        Set(difficulty,"timer",110.5f);Call(difficulty,"FixedUpdate");Check(Police.speed>14.3f&&Police.speed<19f,"difficulty increases smoothly");progress.Data.Score=0;progress.Data.BestScore=0;progress.AddScore(12);
        Call(death,"Die",player);Call(death,"Die",player);Check(!MenuManager.GameActive&&body.isKinematic,"death stops run once");
        yield return new WaitForSeconds(2.2f);
        Check(menu.MenuUI.activeSelf&&player.Health==3&&!body.isKinematic,"respawn");Check(progress.Data.Score==0&&progress.Data.BestScore==12,"record preserved");
        menu.StartGame();Check(Mathf.Approximately(Police.speed,14.3f)&&PoliceSpawner.minTime==12f,"difficulty resets");Check((float)Get(spawner,"time")==0f,"spawner resets");
        Check(coinSet.Count==0,"loot reset");yield return new WaitForFixedUpdate();Check(body.velocity.magnitude>0f,"physics movement");menu.EndRun();Check(body.velocity==Vector3.zero,"motion stops");
        var button=new GameObject("Left").AddComponent<LeftButtonPressed>();button.buttonPressed=true;button.gameObject.SetActive(false);Check(!button.buttonPressed,"touch resets");
        var ads=new GameObject("Ads").AddComponent<AdvManager>();Set(ads,"waitingForAd",true);ads.OnAdClosed();Check(!(bool)Get(ads,"waitingForAd"),"ad callback releases start");
        Set(ads,"_adQueued",false);YG2.nowInterAdv=true;YG.Insides.YGInsides.ErrorInterAdv();Check(!YG2.nowInterAdv,"ad error resets state");
        PlayerPrefs.DeleteKey("Pyaterka_PremiumCar_Simulation");
        Check(!PremiumCar.Owned,"premium initially locked");
        YG2.onPurchaseSuccess?.Invoke("unrelated_product");Check(!PremiumCar.Owned,"unrelated purchase grants nothing");
        YG2.onPurchaseSuccess?.Invoke(PremiumCar.ProductId);Check(PremiumCar.Owned&&progress.IsCarUnlocked(2),"successful premium purchase unlocks actual car slot");
        Set(player,"<CurrentCarIndex>k__BackingField",2);player.maxHealth=12;player.ResetRun();
        Check(player.HasCrashShield&&player.UseCrashShield()&&!player.UseCrashShield(),"premium shield usable once per run");
        player.ResetRun();Check(player.HasCrashShield,"premium shield restored for next run");
        menu.StartGame();int balanceBefore=progress.Data.Coins;
        var premiumCoin=new GameObject("PremiumCoin").AddComponent<Coin>();premiumCoin.Value=2;premiumCoin.Collect();
        Check(progress.Data.Coins-balanceBefore==3,"premium earns exactly fifty percent extra");
        menu.EndRun();PlayerPrefs.DeleteKey("Pyaterka_PremiumCar_Simulation");

    }
}
