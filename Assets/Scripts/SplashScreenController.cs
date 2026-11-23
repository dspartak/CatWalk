using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SplashScreenController : MonoBehaviour
{
    [Header("Настройки заставки")]
    [Tooltip("Общее время показа заставки (в секундах)")]
    public float displayDuration = 3f;

    [Tooltip("Длительность плавного затухания (в секундах)")]
    public float fadeOutDuration = 0.6f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    void Start()
    {
        StartCoroutine(SplashRoutine());
    }

    private IEnumerator SplashRoutine()
    {
        // Удерживаем заставку на экране
        float holdTime = Mathf.Max(0.1f, displayDuration - fadeOutDuration);
        yield return new WaitForSeconds(holdTime);

        // Плавное затухание
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}
