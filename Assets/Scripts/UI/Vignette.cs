using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Vignette : MonoBehaviour
{
    [SerializeField] private Image image;

    public static Vignette Instance;
    private Coroutine flash;

    public void Awake()
    {
        gameObject.SetActive(false);
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Flash()
    {
        gameObject.SetActive(true);
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            image.color = new Color(1, 1, 1, 1 - t * 4);
            yield return null;
        }
        flash = null;
        gameObject.SetActive(false);
    }
}
