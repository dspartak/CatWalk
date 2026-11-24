using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CanvasGroup))]
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Кнопки")]
    public Button newGameButton;
    public Button exitButton;
    public Button pauseButton;

    [Header("3D Шестеренки (задник)")]
    public GameObject gearsBackground;

    [Header("Анимация")]
    public float fadeDuration = 0.35f;

    [Header("Состояние")]
    public bool isMenuOpen = true;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OpenMenu);

        // По умолчанию меню активно на старте (появится после заставки)
        SetMenuState(true, immediate: true);
    }

    void Update()
    {
        // Поддержка клавиши Escape (ПК) / кнопки "Назад" (Android)
        bool backPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            backPressed = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            backPressed = true;
#endif
        if (backPressed)
        {
            ToggleMenu();
        }
    }

    public void OnNewGameClicked()
    {
        Debug.Log("CatWalk: Запуск игры / Новая игра");

        // Сброс очков
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.score = 0;
            ScoreManager.Instance.UpdateUI();
        }

        CloseMenu();
    }

    public void OnExitClicked()
    {
        Debug.Log("CatWalk: Выход из игры");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        isMenuOpen = true;
        gameObject.SetActive(true);
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f, false));
    }

    private void SetMenuState(bool visible, bool immediate)
    {
        isMenuOpen = visible;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
        if (gearsBackground != null)
        {
            gearsBackground.SetActive(visible);
        }
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!visible);
        }
        if (!visible) gameObject.SetActive(false);
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool enableRaycasts)
    {
        if (canvasGroup == null) yield break;

        if (enableRaycasts)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            if (gearsBackground != null) gearsBackground.SetActive(true);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = enableRaycasts;
        canvasGroup.interactable = enableRaycasts;

        if (!enableRaycasts)
        {
            if (gearsBackground != null) gearsBackground.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
