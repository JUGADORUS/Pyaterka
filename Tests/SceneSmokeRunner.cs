using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using TMPro;
using YG;
public class SceneSmokeRunner:MonoBehaviour
{
    int errors,checks;
    void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;}
    void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Debug.Log("SCENE PASS: "+name);}
    IEnumerator Start(){var r=Run();while(true){object next;try{if(!r.MoveNext())break;next=r.Current;}catch(Exception e){Debug.LogException(e);Finish(false);yield break;}yield return next;}Finish(errors==0);}
    void Finish(bool ok){File.WriteAllText("scene-results.txt","passed="+ok+" checks="+checks+" errors="+errors);EditorApplication.Exit(ok?0:1);}
    T Field<T>(object target,string name) { return (T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target); }
    void CheckUpgradeVisibility(bool upgrades)
    {
        var h=ImproveHealth.Instance; var v=ImproveSpeed.Instance;
        var hp=Field<TMP_Text>(h,"_priceText");var sp=Field<TMP_Text>(v,"_priceText");
        Check(hp.gameObject.activeSelf==upgrades&&sp.gameObject.activeSelf==upgrades,"upgrade prices match ownership state");
        if(!upgrades)Check(!h._maxHealth.gameObject.activeSelf&&!v._maxSpeed.gameObject.activeSelf,"MAX badges hidden for locked or premium car");
    }
    IEnumerator Run()
    {
        yield return SceneManager.LoadSceneAsync(0);yield return null;
        Check(SceneManager.GetActiveScene().name=="Game","renamed Game scene loads");
        Check(PlayerCar.Instance!=null&&MenuManager.Instance!=null,"real player and menu initialized");
        Check(PlayerCar.Instance.CurrentCarIndex==0&&PlayerCar.Instance.maxHealth==3,"actual starter model has balanced stats");
        YG2.SwitchLanguage("en");
        var coins=CoinManager.Instance;
        var record=(TMP_Text)typeof(CoinManager).GetField("bestScore",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(coins);
        Check(record.text.StartsWith("Best:"),"actual game HUD localized");
        MenuManager.Instance.StartGame();yield return new WaitForSeconds(0.3f);
        Check(MenuManager.GameActive,"actual game can start");MenuManager.Instance.EndRun();
        yield return SceneManager.LoadSceneAsync(1);yield return null;
        Check(SceneManager.GetActiveScene().name=="Garage","renamed Garage scene loads");
        Check(Podium.Instance!=null&&BuyButton.Instance!=null,"real garage initializes");
        Podium.Instance.RotateCircleToRight();yield return new WaitForSeconds(1.1f);
        var cost=(TMP_Text)typeof(BuyButton).GetField("costText",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(BuyButton.Instance);
        Check(cost.text=="100","real regular car price updated");
        CheckUpgradeVisibility(false);
        GarageCapture.Capture("locked-en");
        var leftIcon=Field<UnityEngine.UI.Image>(BuyButton.Instance,"leftCurrencyIcon");
        Check(leftIcon.gameObject.activeSelf,"ordinary car uses game currency icons");
        Check(Mathf.Abs(leftIcon.rectTransform.anchoredPosition.x)-leftIcon.rectTransform.sizeDelta.x/2f>cost.rectTransform.sizeDelta.x/2f,"ordinary price does not overlap currency icon");
        Progress.Instance.Data.Coins=1000;
        BuyButton.Instance.BuyCar();
        CheckUpgradeVisibility(true);
        Podium.Instance.RotateCircleToRight();yield return new WaitForSeconds(1.1f);
        Check(Podium.Instance.GetIndex()==2&&cost.text.Contains("99"),"actual Hypercar2 has 99 YAN offer");
        Check(!Progress.Instance.IsCarUnlocked(2),"actual paid car initially locked");
        CheckUpgradeVisibility(false);
        Check(!leftIcon.gameObject.activeSelf&&!Field<UnityEngine.UI.Image>(BuyButton.Instance,"rightCurrencyIcon").gameObject.activeSelf,"premium price has no coin icons");
        var bg=BuyButton.Instance.GetComponent<UnityEngine.UI.Image>().color;
        Check(bg.b>bg.g*2f&&bg.r>bg.g*2f,"premium button is purple");
        var note=Field<TMP_Text>(BuyButton.Instance,"paymentNote");
        Check(note.gameObject.activeSelf&&note.text=="Real money","premium clearly identifies real-money purchase");
        Check(Mathf.Abs(note.rectTransform.anchoredPosition.y-cost.rectTransform.anchoredPosition.y)>(note.rectTransform.sizeDelta.y+cost.rectTransform.sizeDelta.y)/2f,"price and payment note do not overlap");
        foreach(string lang in new[]{"ru","en","tr","es","pt"})
        {
            YG2.SwitchLanguage(lang);
            Check(note.text==PyaterkaLocalization.Text("Real money"),"payment note localized "+lang);
        }
        YG2.SwitchLanguage("en");
        yield return null;
        GarageCapture.Capture("premium-en");
        YG2.SwitchLanguage("ru");GarageCapture.Capture("premium-ru");YG2.SwitchLanguage("en");
        yield return new WaitForSeconds(0.3f);
        YG2.onPurchaseSuccess?.Invoke(PremiumCar.ProductId);yield return null;
        Check(Progress.Instance.IsCarUnlocked(2)&&!BuyButton.Instance.gameObject.activeSelf,"purchase callback unlocks real garage car");
        Check(PlayerPrefs.GetInt("Pyaterka_PremiumCar_Simulation",0)==1,"editor purchase persisted only to simulation");
        PlayerPrefs.DeleteKey("Pyaterka_PremiumCar_Simulation");
    }
}
