#if YandexGamesPlatform_yg
using System.Runtime.InteropServices;
using UnityEngine;
using YG.Insides;
#if NJSON_STORAGE_YG2
using Newtonsoft.Json;
#endif

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        [DllImport("__Internal")]
        private static extern string InitCloud_js();

        [DllImport("__Internal")]
        private static extern string LoadCloud_js();

        public void LoadCloud()
        {
            PyaterkaPlatform.CloudAvailable = false;
            if (!YG2.isSDKEnabled)
            {
                YG2.sendMessage.SetLoadSaves(InitCloud_js());
            }
            else
            {
                LoadCloud_js();
            }
        }

        [DllImport("__Internal")]
        private static extern void SaveCloud_js(string jsonData, bool flush);
        public void SaveCloud()
        {
#if NJSON_STORAGE_YG2
            SaveCloud_js(JsonConvert.SerializeObject(YG2.saves), YG2.infoYG.Storage.flush);
#else
            SaveCloud_js(JsonUtility.ToJson(YG2.saves), YG2.infoYG.Storage.flush);
#endif
        }
    }
}

namespace YG.Insides
{
    public partial class YGSendMessage
    {
        public void SetLoadSaves(string data)
        {
            // A failed request must not be treated as a new cloud save.
            PyaterkaPlatform.CloudAvailable = !string.IsNullOrEmpty(data) && data != "PYATERKA_CLOUD_UNAVAILABLE";
            if (PyaterkaPlatform.CloudAvailable)
                YGInsides.SetLoadSaves(data);
            else
                YG2.GetDataInvoke();
        }
    }
}
#endif