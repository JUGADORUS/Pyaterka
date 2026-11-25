using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableObjectManager : MonoBehaviour
{
    public static BreakableObjectManager Instance;
    public List<GameObject> breakableObjects = new List<GameObject> { };
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

    public void TurnOnBreakableObjects()
    {
        for (int i = 0; i < Instance.breakableObjects.Count; i++)
        {
            if (breakableObjects[i] != null) breakableObjects[i].SetActive(true);
        }
        breakableObjects.Clear();
    }
}
