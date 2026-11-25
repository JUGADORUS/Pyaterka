using System.Collections.Generic;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!MenuManager.GameActive || !gameObject.activeSelf ||
            (collision.gameObject.GetComponentInParent<PlayerCar>() == null &&
             collision.gameObject.GetComponentInParent<Police>() == null)) return;
        BreakableObjectManager.Instance.breakableObjects.Add(gameObject);
        gameObject.SetActive(false);
    } 
}
