using System.Collections;
using TMPro;
using UnityEngine;

/// Controla todos los elementos de UI del HUD durante el juego.
///
/// Lee el estado de WaveManager.Instance cada frame y actualiza:
///   - waveText       → "OLEADA 2 / 3 — Asalto"
///   - statusText     → "Preparando..." / "¡En curso!" / "Despejando..." / "¡Victoria!"
///   - countdownText  → "Próxima oleada en: 4s" (visible solo en cuenta regresiva)
///   - enemyCountText → "Enemigos: 7"
///
/// Adjunta este script al objeto Canvas (o a un hijo Empty llamado HUDController).
public class HUDController : MonoBehaviour
{
    // ── Referencias UI ───────────────────────────────────────────────────────
    [Header("Textos de Oleada")]
    [Tooltip("Muestra el número y nombre de la oleada actual.")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Tooltip("Muestra el estado actual: Preparando / En curso / etc.")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Tooltip("Cuenta regresiva antes de cada oleada. Se oculta mientras hay combate.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Muestra cuántos enemigos quedan vivos en escena.")]
    [SerializeField] private TextMeshProUGUI enemyCountText;

    // ── Colores de estado ─────────────────────────────────────────────────────
    [Header("Colores de Estado")]
    [SerializeField] private Color colorWaiting      = new Color(1f,  0.85f, 0.2f);  // amarillo
    [SerializeField] private Color colorSpawning     = new Color(1f,  0.4f,  0.1f);  // naranja
    [SerializeField] private Color colorWaitingClear = new Color(0.4f,0.8f,  1f);    // azul claro
    [SerializeField] private Color colorVictory      = new Color(0.3f,1f,    0.4f);  // verde

    // ── Estado interno ────────────────────────────────────────────────────────
    private WaveState _lastState = (WaveState)(-1); // valor inválido para forzar update inicial

    // ── Ciclo de vida ─────────────────────────────────────────────────────────
    private void Start()
    {
        // Oculta la cuenta regresiva hasta que haya datos
        SetCountdownVisible(false);
    }

    private void Update()
    {
        if (WaveManager.Instance == null) return;

        RefreshWaveText();
        RefreshStatusText();
        RefreshCountdown();
        RefreshEnemyCount();
    }

    // ── Actualización de cada elemento ───────────────────────────────────────

    /// "OLEADA 2 / 3 — Asalto"
    private void RefreshWaveText()
    {
        if (waveText == null) return;
        waveText.text = $"OLEADA  {WaveManager.Instance.CurrentWaveNumber} / " +
                        $"{WaveManager.Instance.TotalWaves}" +
                        $"  —  {WaveManager.Instance.CurrentWaveName}";
    }

    /// Mensaje y color según el estado de la oleada
    private void RefreshStatusText()
    {
        if (statusText == null) return;

        WaveState state = WaveManager.Instance.State;

        // Solo actualizamos si el estado cambió (evitamos asignar cada frame).
        if (state == _lastState) return;
        _lastState = state;

        switch (state)
        {
            case WaveState.Waiting:
                statusText.text  = "Preparando siguiente oleada...";
                statusText.color = colorWaiting;
                break;

            case WaveState.Spawning:
                statusText.text  = "¡OLEADA EN CURSO!";
                statusText.color = colorSpawning;
                // Lanzar corrutina de animación de pulso en el texto
                StartCoroutine(PulseTextRoutine(statusText));
                break;

            case WaveState.WaitingForClear:
                statusText.text  = "Eliminando enemigos...";
                statusText.color = colorWaitingClear;
                break;

            case WaveState.AllComplete:
                statusText.text  = "¡VICTORIA!";
                statusText.color = colorVictory;
                StartCoroutine(PulseTextRoutine(statusText));
                break;
        }
    }

    /// "Próxima oleada en: 4s" — solo visible durante la cuenta regresiva
    private void RefreshCountdown()
    {
        if (countdownText == null) return;

        bool isWaiting = WaveManager.Instance.State == WaveState.Waiting;
        SetCountdownVisible(isWaiting);

        if (isWaiting)
        {
            int seconds = Mathf.CeilToInt(WaveManager.Instance.Countdown);
            countdownText.text = $"Próxima oleada en:  {seconds}s";
        }
    }

    /// "Enemigos: 7" — se oculta a cero cuando estamos en espera
    private void RefreshEnemyCount()
    {
        if (enemyCountText == null) return;

        int alive = WaveManager.Instance.EnemiesAlive;
        enemyCountText.text = $"Enemigos:  {alive}";

        // Texto en rojo si quedan pocos enemigos para tensión visual
        enemyCountText.color = alive <= 3 && alive > 0
            ? new Color(1f, 0.3f, 0.3f)
            : Color.white;
    }

    // ── Corrutinas de feedback visual ─────────────────────────────────────────

    /// Pulsa la escala del texto tres veces para llamar la atención.
    private IEnumerator PulseTextRoutine(TextMeshProUGUI tmp)
    {
        if (tmp == null) yield break;

        Vector3 originalScale = tmp.transform.localScale;
        Vector3 bigScale      = originalScale * 1.25f;
        float   halfDuration  = 0.12f;

        for (int i = 0; i < 3; i++)
        {
            // Escala hacia arriba
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                tmp.transform.localScale = Vector3.Lerp(originalScale, bigScale, t);
                yield return null;
            }

            // Escala hacia abajo
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                tmp.transform.localScale = Vector3.Lerp(bigScale, originalScale, t);
                yield return null;
            }
        }

        tmp.transform.localScale = originalScale;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetCountdownVisible(bool visible)
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(visible);
    }
}
