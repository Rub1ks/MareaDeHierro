using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ---------------------------------------------------------------------------
// Estados posibles del juego en cualquier momento.
// UIScreenManager y HUDController escuchan este enum para reaccionar.
// ---------------------------------------------------------------------------
public enum GameState
{
    Playing,    // Juego activo, tiempo corriendo
    Paused,     // Pausado por el jugador (Escape)
    GameOver,   // Base destruida
    Victory     // Todas las oleadas completadas
}

// ---------------------------------------------------------------------------
// GameManager — Máquina de estados central del juego.
//
// Responsabilidades:
//   · Mantener y cambiar el GameState actual.
//   · Controlar Time.timeScale (pausa, cámara lenta en Game Over).
//   · Notificar a otros sistemas mediante el evento OnStateChanged.
//   · Exponer métodos públicos: TriggerGameOver, TriggerVictory,
//     TogglePause, RestartGame, QuitGame.
//
// Otros scripts lo llaman así:
//   GameManager.Instance.TriggerGameOver();
//   GameManager.Instance.TriggerVictory();
// ---------------------------------------------------------------------------
public class GameManager : MonoBehaviour
{
    // Instancia estática accesible desde cualquier script.
    public static GameManager Instance { get; private set; }

    // ── Configuración Inspector ──────────────────────────────────────────────
    [Header("Cámara lenta al perder")]
    [Tooltip("Duración en segundos del efecto de cámara lenta antes del Game Over.")]
    [SerializeField] private float slowMoDuration = 1.2f;

    [Tooltip("Escala de tiempo mínima durante la cámara lenta (0 = parada total).")]
    [SerializeField][Range(0f, 0.5f)] private float slowMoTargetScale = 0f;

    // ── Estado ───────────────────────────────────────────────────────────────
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ── Evento ───────────────────────────────────────────────────────────────
    // UIScreenManager y HUDController se suscriben aquí para recibir cambios.
    public event System.Action<GameState> OnStateChanged;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Aseguramos que el tiempo corra al iniciar (importante en reinicios).
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Tecla Escape: alterna pausa solo si el juego está activo o pausado.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// Llamado por BaseHealth cuando la vida llega a 0.
    /// Inicia el efecto de cámara lenta y luego activa el estado GameOver.
    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver || CurrentState == GameState.Victory) return;
        StartCoroutine(GameOverSequenceRoutine());
    }

    /// Llamado por WaveManager cuando se completan todas las oleadas.
    public void TriggerVictory()
    {
        if (CurrentState == GameState.GameOver || CurrentState == GameState.Victory) return;
        Time.timeScale = 0f;
        SetState(GameState.Victory);
    }

    /// Alterna entre Playing y Paused.
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }
        else if (CurrentState == GameState.Paused)
        {
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }
    }

    /// Reinicia la escena activa desde cero.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// Cierra la aplicación.
    /// En build ejecutable llama Application.Quit().
    /// En el Editor de Unity detiene el Play Mode.
    public void QuitGame()
    {
        Debug.Log("[GameManager] Saliendo del juego.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Corrutinas ───────────────────────────────────────────────────────────

    /// Efecto dramático: ralentiza gradualmente el tiempo antes de mostrar
    /// la pantalla de Game Over. Usa unscaledDeltaTime para no verse afectado
    /// por el propio timeScale que está modificando.
    private IEnumerator GameOverSequenceRoutine()
    {
        float elapsed    = 0f;
        float startScale = Time.timeScale;

        while (elapsed < slowMoDuration)
        {
            elapsed        += Time.unscaledDeltaTime;
            float t         = Mathf.Clamp01(elapsed / slowMoDuration);
            Time.timeScale  = Mathf.Lerp(startScale, slowMoTargetScale, t);
            yield return null;
        }

        Time.timeScale = slowMoTargetScale;
        SetState(GameState.GameOver);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    /// Actualiza el estado y lanza el evento para que los listeners reaccionen.
    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] Estado → {newState}");
    }
}
