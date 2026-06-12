using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ---------------------------------------------------------------------------
// MainMenuManager — Controla la pantalla de menú principal (MenuScene).
//
// Estrategia "hotspot": los textos JUGAR / INSTRUCCIONES / SALIR / VOLVER ya
// vienen dibujados en las imágenes de fondo (1920x1080). Encima de cada texto
// se coloca un Button de Unity con su Image en alpha 0: invisible pero
// cliqueable (la Image sigue siendo Raycast Target aunque no se vea).
//
// Paneles (cada uno con su CanvasGroup):
//   · mainPanel          — Fondo del menú con JUGAR / INSTRUCCIONES / SALIR
//   · instruccionesPanel — Fondo de instrucciones con VOLVER
//
// El cambio de panel se hace con alpha + interactable + blocksRaycasts del
// CanvasGroup. blocksRaycasts = false es CLAVE: garantiza que el panel oculto
// nunca intercepte los clics destinados a los botones del panel visible.
// ---------------------------------------------------------------------------
public class MainMenuManager : MonoBehaviour
{
    // ── Paneles ──────────────────────────────────────────────────────────────
    [Header("Paneles (arrastrar los CanvasGroup desde la Hierarchy)")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup instruccionesPanel;

    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Configuración")]
    [Tooltip("Índice de la escena de juego en Build Settings (Menú = 0, Juego = 1).")]
    [SerializeField] private int gameSceneIndex = 1;

    [SerializeField] private float fadeDuration = 0.35f;

    private bool _isLoading;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Nos aseguramos de que el tiempo corra (puede venir de una partida pausada).
        Time.timeScale = 1f;

        HideInstantly(instruccionesPanel);
        StartCoroutine(FadeRoutine(mainPanel, 0f, 1f, fadeDuration));
    }

    // ── Métodos de los botones (asígnalos en el OnClick de cada botón) ────────

    /// Botón "JUGAR" — fade-out del menú y carga de la escena de juego.
    public void OnJugarPressed()
    {
        if (_isLoading) return;   // Evita dobles clics mientras carga.
        _isLoading = true;
        StartCoroutine(LoadGameRoutine());
    }

    /// Botón "INSTRUCCIONES" — transición del panel principal al de instrucciones.
    public void OnInstruccionesPressed()
    {
        StartCoroutine(SwapPanelsRoutine(mainPanel, instruccionesPanel));
    }

    /// Botón "VOLVER" — transición del panel de instrucciones al principal.
    public void OnVolverPressed()
    {
        StartCoroutine(SwapPanelsRoutine(instruccionesPanel, mainPanel));
    }

    /// Botón "SALIR" — cierra la aplicación (detiene el Play Mode en el editor).
    public void OnSalirPressed()
    {
        Debug.Log("[MainMenuManager] Saliendo del juego.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers de visibilidad ────────────────────────────────────────────────

    /// Oculta un panel de golpe, sin animación.
    private static void HideInstantly(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha          = 0f;
        cg.interactable   = false;
        cg.blocksRaycasts = false;
    }

    // ── Corrutinas ────────────────────────────────────────────────────────────

    /// Cross-fade entre paneles: oculta 'from' y luego muestra 'to'.
    private IEnumerator SwapPanelsRoutine(CanvasGroup from, CanvasGroup to)
    {
        yield return FadeRoutine(from, 1f, 0f, fadeDuration);
        yield return FadeRoutine(to,   0f, 1f, fadeDuration);
    }

    /// Anima el alpha del CanvasGroup y sincroniza interactable/blocksRaycasts.
    private IEnumerator FadeRoutine(CanvasGroup cg, float fromAlpha, float toAlpha, float duration)
    {
        if (cg == null) yield break;

        bool visibleAlFinal = toAlpha > 0.5f;

        // Durante la animación nadie puede hacer clic (evita clics a mitad de fade).
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        cg.alpha = fromAlpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        cg.alpha          = toAlpha;
        cg.interactable   = visibleAlFinal;
        cg.blocksRaycasts = visibleAlFinal;
    }

    /// Fade-out del menú y carga de la escena de juego por índice de Build Settings.
    private IEnumerator LoadGameRoutine()
    {
        yield return FadeRoutine(mainPanel, 1f, 0f, fadeDuration);
        SceneManager.LoadScene(gameSceneIndex);
    }
}
