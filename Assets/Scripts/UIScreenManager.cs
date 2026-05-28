using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ---------------------------------------------------------------------------
// UIScreenManager — Muestra y anima los paneles de Pausa, Game Over y Victoria.
//
// CORRECCIÓN CLAVE:
//   La suscripción a GameManager.OnStateChanged se hace en Start(), NO en
//   OnEnable(). Esto es porque OnEnable() corre antes de que GameManager
//   ejecute su Awake() y asigne la instancia estática, por lo que la
//   suscripción fallaba silenciosamente y los paneles nunca aparecían.
//
// Todas las animaciones usan Time.unscaledDeltaTime para funcionar correctamente
// cuando Time.timeScale es 0 (pausa o Game Over).
// ---------------------------------------------------------------------------
public class UIScreenManager : MonoBehaviour
{
    // ── Paneles ──────────────────────────────────────────────────────────────
    [Header("Paneles (arrastrar desde la Hierarchy)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject pausePanel;

    // ── Textos de título dentro de cada panel ────────────────────────────────
    [Header("Títulos animados")]
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI victoryTitleText;

    // ── CanvasGroup para fade (uno por panel) ────────────────────────────────
    [Header("CanvasGroups — uno por panel")]
    [SerializeField] private CanvasGroup gameOverCG;
    [SerializeField] private CanvasGroup victoryCG;
    [SerializeField] private CanvasGroup pauseCG;

    // ── Duración de animaciones ───────────────────────────────────────────────
    [Header("Animaciones")]
    [SerializeField] private float fadeDuration  = 0.35f;
    [SerializeField] private float pulseDuration = 0.14f;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Nos aseguramos de que todos los paneles estén ocultos al arrancar.
        HideAll();
    }

    private void Start()
    {
        // IMPORTANTE: la suscripción va aquí, en Start(), porque en este punto
        // todos los Awake() ya han corrido y GameManager.Instance ya existe.
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIScreenManager] No se encontró GameManager en la escena. " +
                           "Asegúrate de que existe un objeto con el script GameManager.");
            return;
        }

        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        // Limpiamos la suscripción para evitar errores si la escena se recarga.
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    // ── Reacción al cambio de estado ──────────────────────────────────────────

    private void HandleStateChanged(GameState newState)
    {
        // Detenemos cualquier animación anterior antes de iniciar la nueva.
        StopAllCoroutines();
        HideAll();

        switch (newState)
        {
            case GameState.GameOver:
                ShowPanel(gameOverPanel, gameOverCG);
                StartCoroutine(PulseTitleRoutine(gameOverTitleText));
                break;

            case GameState.Victory:
                ShowPanel(victoryPanel, victoryCG);
                StartCoroutine(BounceInTitleRoutine(victoryTitleText));
                break;

            case GameState.Paused:
                ShowPanel(pausePanel, pauseCG);
                break;

            case GameState.Playing:
                // HideAll() ya ocultó todo arriba.
                break;
        }
    }

    // ── Métodos de los botones (asígnalos en el Inspector de cada botón) ──────

    /// Botón "Reintentar" (Game Over y Victoria).
    public void OnRestartPressed()   => GameManager.Instance.RestartGame();

    /// Botón "Salir" (todos los paneles).
    public void OnQuitPressed()      => GameManager.Instance.QuitGame();

    /// Botón "Reanudar" (Pausa).
    public void OnResumePressed()    => GameManager.Instance.TogglePause();

    // ── Helpers de visibilidad ────────────────────────────────────────────────

    private void HideAll()
    {
        SetActive(gameOverPanel, false);
        SetActive(victoryPanel,  false);
        SetActive(pausePanel,    false);
    }

    /// Activa el panel y lanza su fade-in. Si no tiene CanvasGroup asignado
    /// el panel aparece directamente sin animación (mejor que no verse).
    private void ShowPanel(GameObject panel, CanvasGroup cg)
    {
        SetActive(panel, true);

        if (cg != null)
            StartCoroutine(FadeInRoutine(cg));
        else
            Debug.LogWarning($"[UIScreenManager] El panel '{panel?.name}' no tiene " +
                              "CanvasGroup asignado. El panel aparece sin animación.");
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    // ── Corrutinas de animación ───────────────────────────────────────────────

    /// Fade-in del alpha del CanvasGroup de 0 → 1.
    private IEnumerator FadeInRoutine(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed   += Time.unscaledDeltaTime;
            cg.alpha   = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    /// Pulsa el título de Game Over tres veces (escala normal → grande → normal).
    private IEnumerator PulseTitleRoutine(TextMeshProUGUI tmp)
    {
        if (tmp == null) yield break;
        yield return new WaitForSecondsRealtime(fadeDuration);

        Vector3 normal = tmp.transform.localScale;
        Vector3 big    = normal * 1.3f;

        for (int i = 0; i < 3; i++)
        {
            yield return LerpScale(tmp.transform, normal, big,    pulseDuration);
            yield return LerpScale(tmp.transform, big,    normal, pulseDuration);
        }
    }

    /// El título de Victoria entra desde escala 0 con un pequeño rebote.
    private IEnumerator BounceInTitleRoutine(TextMeshProUGUI tmp)
    {
        if (tmp == null) yield break;
        yield return new WaitForSecondsRealtime(fadeDuration * 0.4f);

        Vector3 target = tmp.transform.localScale;
        tmp.transform.localScale = Vector3.zero;

        yield return LerpScale(tmp.transform, Vector3.zero, target * 1.2f, 0.18f);
        yield return LerpScale(tmp.transform, target * 1.2f, target,       0.10f);
    }

    /// Interpola la escala de un Transform usando tiempo no escalado.
    private IEnumerator LerpScale(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.unscaledDeltaTime;
            t.localScale  = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        t.localScale = to;
    }
}
