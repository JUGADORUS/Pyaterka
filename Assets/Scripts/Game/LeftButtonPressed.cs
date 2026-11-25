using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeftButtonPressed : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool buttonPressed;

    public static LeftButtonPressed Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        buttonPressed = true;
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        buttonPressed = false;
    }

    private void OnDisable() { buttonPressed = false; }
    private void OnApplicationFocus(bool focused) { if (!focused) buttonPressed = false; }
}
