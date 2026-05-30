using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ---------------------------------------------------------------------------
// MainMenuManager — Controla la pantalla de menú principal.
//
// Paneles:
//   · mainPanel        — Contiene los botones Jugar / Instrucciones / Salir
//   · instruccionesPanel — Explica los controles del juego
//
// Animaciones:
//   · Título flota suavemente en Y
//   · Fade-in al entrar, fade-out al cargar la partida
// ---------------------------------------------------------------------------
public class MainMenuManager : MonoBehaviour
{
    // ── Paneles ──────────────────────────────────────────────────────────────
    [Header("Paneles (arrastrar desde la Hierarchy)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject instruccionesPanel;

    // ── Título animado ────────────────────────────────────────────────────────
    [Header("Título")]
    [SerializeField] private TextMeshProUGUI titleText;

    // ── CanvasGroups para fades ───────────────────────────────────────────────
    [Header("CanvasGroups — uno por panel")]
    [SerializeField] private CanvasGroup mainCG;
    [SerializeField] private CanvasGroup instruccionesCG;

    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Configuración")]
    [Tooltip("Nombre EXACTO de la escena de juego (tal como aparece en Build Settings).")]
    [SerializeField] private string gameSceneName = "MainScene";

    [SerializeField] private float fadeInDuration  = 0.6f;
    [SerializeField] private float titleFloatSpeed = 1.2f;
    [SerializeField] private float titleFloatAmount = 6f;

    private Vector3 _titleBasePos;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Nos aseguramos de que el tiempo corra (puede venir de una partida pausada).
        Time.timeScale = 1f;

        if (instruccionesPanel != null)
            instruccionesPanel.SetActive(false);

        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (titleText != null)
            _titleBasePos = titleText.transform.localPosition;

        StartCoroutine(FadeInRoutine(mainCG, fadeInDuration));
        StartCoroutine(FloatTitleRoutine());
    }

    // ── Botones del menú principal ────────────────────────────────────────────

    /// Botón "JUGAR" — carga la escena de juego con fade-out.
    public void OnJugarPressed()
    {
        StartCoroutine(LoadSceneWithFade(gameSceneName));
    }

    /// Botón "INSTRUCCIONES" — muestra el panel de controles.
    public void OnInstruccionesPressed()
    {
        if (mainPanel != null)         mainPanel.SetActive(false);
        if (instruccionesPanel != null) instruccionesPanel.SetActive(true);
        StartCoroutine(FadeInRoutine(instruccionesCG, 0.3f));
    }

    /// Botón "VOLVER" dentro del panel de instrucciones.
    public void OnVolverPressed()
    {
        if (instruccionesPanel != null) instruccionesPanel.SetActive(false);
        if (mainPanel != null)          mainPanel.SetActive(true);
        StartCoroutine(FadeInRoutine(mainCG, 0.3f));
    }

    /// Botón "SALIR" — cierra la aplicación.
    public void OnSalirPressed()
    {
        Debug.Log("[MainMenuManager] Saliendo del juego.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Corrutinas de animación ───────────────────────────────────────────────

    /// El título flota suavemente arriba y abajo de forma continua.
    private IEnumerator FloatTitleRoutine()
    {
        if (titleText == null) yield break;

        while (true)
        {
            float offsetY = Mathf.Sin(Time.time * titleFloatSpeed) * titleFloatAmount;
            titleText.transform.localPosition = _titleBasePos + new Vector3(0f, offsetY, 0f);
            yield return null;
        }
    }

    /// Fade-in del alpha de 0 → 1.
    private IEnumerator FadeInRoutine(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.unscaledDeltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    /// Fade-out del alpha de 1 → 0.
    private IEnumerator FadeOutRoutine(CanvasGroup cg, float duration)
    {
        if (cg == null) yield break;

        cg.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.unscaledDeltaTime;
            cg.alpha  = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
    }

    /// Hace fade-out del panel principal y carga la escena indicada.
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return StartCoroutine(FadeOutRoutine(mainCG, 0.4f));
        SceneManager.LoadScene(sceneName);
    }
}
