using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public static class PyaterkaLocalization
{
    private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>
    {
        { "Real money", new[] { "Реальные деньги", "Real money", "Gerçek para", "Dinero real", "Dinheiro real" } },
        { "Buying", new[] { "Покупка…", "Purchasing…", "Satın alınıyor…", "Comprando…", "Comprando…" } },
        { "Unavailable", new[] { "Недоступно", "Unavailable", "Kullanılamıyor", "No disponible", "Indisponível" } },
        { "Purchase failed", new[] { "Покупка не завершена. Можно повторить.", "Purchase not completed. You can retry.", "Satın alma tamamlanmadı. Tekrar dene.", "Compra no completada. Puedes reintentar.", "Compra não concluída. Tente novamente." } },
        { "Premium perks", new[] { "VIP: +50% заработка\n1 спасение от гибели за заезд", "VIP: +50% earnings\n1 fatal-hit shield per run", "VIP: +%50 kazanç\nTur başına 1 ölümcül darbe kalkanı", "VIP: +50% de ingresos\n1 escudo contra golpe mortal por partida", "VIP: +50% de ganhos\n1 escudo contra golpe fatal por partida" } },
        { "Survival income", new[] { "Деньги за каждые 10 секунд выживания.\nПокупай и улучшай машины.", "Earn cash every 10 seconds of survival.\nBuy and upgrade cars.", "Hayatta kaldığın her 10 saniyede para kazan.\nAraba al ve geliştir.", "Gana dinero cada 10 segundos de supervivencia.\nCompra y mejora coches.", "Ganhe dinheiro a cada 10 segundos de sobrevivência.\nCompre e melhore carros." } },
        { "Play", new[] { "поехали!", "Play", "Oyna", "Jugar", "Jogar" } },
        { "Logo GA", new[] { "Г   А", "G   A", "G   A", "G   A", "G   A" } },
        { "Logo T", new[] { "Т", "T", "T", "T", "T" } },
        { "Go to work!", new[] { "Пойти на работу!", "Go to work!", "İşe git!", "¡A trabajar!", "Vá trabalhar!" } },
        { "More cars", new[] { "Другие машины", "More cars", "Diğer arabalar", "Más coches", "Mais carros" } },
        { "How to play", new[] { "Как играть", "How to play", "Nasıl oynanır", "Cómo jugar", "Como jogar" } },
        { "Improve", new[] { "Улучшить", "Upgrade", "Geliştir", "Mejorar", "Melhorar" } },
        { "Health", new[] { "Здоровье", "Health", "Can", "Salud", "Vida" } },
        { "Speed", new[] { "Скорость", "Speed", "Hız", "Velocidad", "Velocidade" } },
        { "Buy", new[] { "Купить", "Buy", "Satın al", "Comprar", "Comprar" } },
        { "Select", new[] { "Принять", "Select", "Seç", "Elegir", "Selecionar" } },
        { "Back", new[] { "Домой", "Back", "Geri", "Volver", "Voltar" } },
        { "MAX", new[] { "Макс.", "MAX", "MAKS.", "MÁX.", "MÁX." } },
        { "OK", new[] { "ОК", "OK", "Tamam", "Aceptar", "OK" } },
        { "Promo", new[] { "Введи промокод:", "Enter promo code:", "Promosyon kodu:", "Código promocional:", "Código promocional:" } },
        { "Best: {0}", new[] { "Рекорд: {0}", "Best: {0}", "Rekor: {0}", "Récord: {0}", "Recorde: {0}" } },
        { "Escape", new[] { "Уезжай от полиции!\nПродержись как можно дольше!", "Escape the police!\nSurvive as long as you can!", "Polisten kaç!\nOlabildiğince uzun süre dayan!", "¡Escapa de la policía!\n¡Sobrevive todo lo que puedas!", "Fuja da polícia!\nSobreviva o máximo que puder!" } },
        { "Instructions", new[] { "1. Уворачивайся от полиции, используя дрифт (зажми кнопку поворота подольше).\n\n2. Не врезайся в объекты.\n\n3. Собирай деньги, чтобы улучшать машину и покупать новые.", "1. Dodge the police by drifting (hold a turn button).\n\n2. Avoid obstacles.\n\n3. Collect money to upgrade your car and buy new ones.", "1. Drift yaparak polisten kaç (dönüş düğmesini basılı tut).\n\n2. Engellere çarpma.\n\n3. Arabanı geliştirmek ve yeni arabalar almak için para topla.", "1. Esquiva a la policía derrapando (mantén pulsado el botón de giro).\n\n2. Evita los obstáculos.\n\n3. Recoge dinero para mejorar tu coche y comprar otros.", "1. Desvie da polícia com drift (segure o botão de virar).\n\n2. Evite os obstáculos.\n\n3. Colete dinheiro para melhorar seu carro e comprar outros." } },
        { "FightClub", new[] { "\n\n4. Не упоминать о Бойцовском клубе.\n\n5. Не упоминать нигде о Бойцовском клубе.", "\n\n4. Do not talk about Fight Club.\n\n5. Never talk about Fight Club.", "\n\n4. Dövüş Kulübü hakkında konuşma.\n\n5. Dövüş Kulübü hakkında asla konuşma.", "\n\n4. No hables del Club de la Lucha.\n\n5. Nunca hables del Club de la Lucha.", "\n\n4. Não fale sobre o Clube da Luta.\n\n5. Nunca fale sobre o Clube da Luta." } },
        { "Pyaterka Friday", new[] { "Пятница Пятерки", "Pyaterka Friday", "Pyaterka Cuması", "Viernes de Pyaterka", "Sexta do Pyaterka" } },
        { "Vice City", new[] { "Вай-Сити", "Vice City", "Vice City", "Vice City", "Vice City" } },
        { "Rostov", new[] { "Ростов", "Rostov", "Rostov", "Rostov", "Rostov" } },
        { "Strip club", new[] { "Стрипуха", "Strip club", "Striptiz kulübü", "Club de striptease", "Clube de striptease" } },
        { "Monument", new[] { "Памятник\nЛегенде хайпа", "Monument to\nthe hype legend", "Hype efsanesinin\nanıtı", "Monumento a\nla leyenda del hype", "Monumento à\nlenda do hype" } },
        { "HomeSign", new[] { "Дом Пятерки?", "Pyaterka’s house?", "Pyaterka’nın evi mi?", "¿La casa de Pyaterka?", "A casa do Pyaterka?" } },
    };
    private static readonly List<Binding> Labels = new List<Binding>();
    private static int language;
    private static TMP_FontAsset fallbackFont;
    public static event Action Changed;
    private class Binding { public TMP_Text Label; public string Key; public bool Legacy; }
    public static string Text(string key) { return Translations.TryGetValue(key, out var row) ? row[language] : key; }
    public static string Format(string key, params object[] args) { return string.Format(Text(key), args); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        language = 0;
        Labels.Clear();
        Changed = null;
        fallbackFont = null;
        SceneManager.sceneLoaded -= BindScene;
        SceneManager.sceneLoaded += BindScene;
    }

    private static string Normalize(string text)
    {
        return Regex.Replace((text ?? "").Replace("\u200b", ""), @"\s+", " ").Trim();
    }

    private static void BindScene(Scene scene, LoadSceneMode mode)
    {
        YG2.onSwitchLang -= ApplyLanguage;
        YG2.onSwitchLang += ApplyLanguage;
        Labels.RemoveAll(binding => binding.Label == null);
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            PrepareFont(label);
            TMP_InputField input = label.GetComponentInParent<TMP_InputField>(true);
            if (input != null && input.textComponent == label) continue;
            string source = Normalize(label.text);
            string key = null;
            bool legacy = false;
            if (source.StartsWith("1.", StringComparison.Ordinal))
            {
                key = "Instructions";
                legacy = source.Contains("Бойцовском");
            }
            else
            {
                foreach (var entry in Translations)
                {
                    if (Normalize(entry.Value[0]) == source) { key = entry.Key; break; }
                }
                if (source.StartsWith("Уезжай от полиции!", StringComparison.Ordinal)) key = "Escape";
            }
            if (key == null) continue;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * 0.55f;
            label.enableAutoSizing = true;
            Labels.Add(new Binding { Label = label, Key = key, Legacy = legacy });
        }
        ApplyLanguage(YG2.isSDKEnabled ? YG2.lang : "ru");
    }

    private static void PrepareFont(TMP_Text label)
    {
        if (label.font == null) return;
        if (fallbackFont == null)
        {
            var standard = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (standard != null && standard.sourceFontFile != null)
            {
                fallbackFont = TMP_FontAsset.CreateFontAsset(standard.sourceFontFile);
                fallbackFont.name = "Pyaterka Latin and Cyrillic fallback";
                fallbackFont.isMultiAtlasTexturesEnabled = true;
            }
        }
        if (fallbackFont == null) return;
        if (label.font.fallbackFontAssetTable == null)
            label.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (!label.font.fallbackFontAssetTable.Contains(fallbackFont))
            label.font.fallbackFontAssetTable.Add(fallbackFont);
    }

    public static void ApplyLanguage(string code)
    {
        string lang = (code ?? "en").Split('-', '_')[0].ToLowerInvariant();
        language = lang == "ru" || lang == "be" || lang == "kk" || lang == "uk" || lang == "uz" ? 0 :
            lang == "tr" ? 2 : lang == "es" ? 3 : lang == "pt" ? 4 : 1;
        foreach (Binding binding in Labels)
            if (binding.Label != null)
                binding.Label.text = Text(binding.Key) + (binding.Legacy ? Text("FightClub") : "");
        Changed?.Invoke();
    }
}
