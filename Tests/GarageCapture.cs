using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public static class GarageCapture
{
    public static void Capture(string state)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) return;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(854,480),new Vector2Int(1024,768)})
            CaptureSize(state,size.x,size.y);
    }
    static void CaptureSize(string state,int width,int height)
    {
        Camera camera=Camera.main;
        var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
        var canvases=UnityEngine.Object.FindObjectsOfType<Canvas>();
        var modes=new RenderMode[canvases.Length];var cameras=new Camera[canvases.Length];var distances=new float[canvases.Length];
        var scalers=new CanvasScaler[canvases.Length];var enabled=new bool[canvases.Length];var scales=new float[canvases.Length];
        camera.targetTexture=target;camera.aspect=(float)width/height;
        for(int i=0;i<canvases.Length;i++)
        {
            var canvas=canvases[i];modes[i]=canvas.renderMode;cameras[i]=canvas.worldCamera;distances[i]=canvas.planeDistance;scales[i]=canvas.scaleFactor;
            scalers[i]=canvas.GetComponent<CanvasScaler>();
            if(scalers[i]!=null){enabled[i]=scalers[i].enabled;scalers[i].enabled=false;}
            if(canvas.renderMode==RenderMode.WorldSpace)continue;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+0.25f;
            if(scalers[i]!=null&&scalers[i].uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize)
                canvas.scaleFactor=width/scalers[i].referenceResolution.x;
        }
        Canvas.ForceUpdateCanvases();
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TMP_Text>())label.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var oldActive=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        Directory.CreateDirectory("Captures");File.WriteAllBytes("Captures/"+state+"-"+width+"x"+height+".png",image.EncodeToPNG());
        var report=new List<string>();
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
        {
            if(label.transform.IsChildOf(BuyButton.Instance.transform)||label.gameObject.name=="Car offer")
            {
                var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
                float pxHeight=Mathf.Abs(camera.WorldToViewportPoint(corners[1]).y-camera.WorldToViewportPoint(corners[0]).y)*height;
                float pxFont=label.fontSize*pxHeight/Mathf.Max(1f,label.rectTransform.rect.height);
                report.Add(label.name+" | font px="+pxFont.ToString("0.0")+" | overflow="+label.isTextOverflowing+" | "+label.text.Replace("\n"," / "));
            }
        }
        File.WriteAllLines("Captures/"+state+"-"+width+"x"+height+".txt",report);
        RenderTexture.active=oldActive;UnityEngine.Object.Destroy(image);
        camera.targetTexture=oldTarget;camera.aspect=oldAspect;
        for(int i=0;i<canvases.Length;i++)
        {
            canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];canvases[i].scaleFactor=scales[i];
            if(scalers[i]!=null)scalers[i].enabled=enabled[i];
        }
        target.Release();UnityEngine.Object.Destroy(target);Canvas.ForceUpdateCanvases();
    }
}
