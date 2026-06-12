using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Gestiona la vida del fuerte (la base del jugador).
///
/// Flujo de daño:
///   Un enemigo entra en el trigger del Player
///   → OnTriggerEnter2D detecta tag "Enemy"
///   → Llama TakeDamage() en este script  (daño a la base)
///   → Llama TakeDamage(999) en EnemyHealth (destruye al enemigo normalmente,
///     lo que a su vez notifica al WaveManager)
///   → Actualiza el Slider de vida
///   → Lanza HitFlashRoutine() para feedback visual
///
/// Requiere: Rigidbody2D Kinematic + Collider2D con Is Trigger = true en el Player.
[RequireComponent(typeof(SpriteRenderer))]
public class BaseHealth : MonoBehaviour
{
    // ── Configuración Inspector ──────────────────────────────────────────────
    [Header("Vida de la Base")]
    [SerializeField] private float maxHealth = 10f;

    [Tooltip("Daño que recibe la base por cada enemigo que la alcanza.")]
    [SerializeField] private float damagePerEnemy = 1f;

    [Header("UI")]
    [Tooltip("Arrastra aquí el componente Slider de la barra de vida.")]
    [SerializeField] private Slider healthSlider;

    [Header("Feedback Visual")]
    [SerializeField] private Color  hitColor       = Color.red;
    [SerializeField] private float  flashDuration  = 0.15f;

    // ── Estado interno ───────────────────────────────────────────────────────
    private float          _currentHealth;
    private SpriteRenderer _sr;
    private Color          _originalColor;
    private bool           _isDead;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    private void Awake()
    {
        _sr            = GetComponent<SpriteRenderer>();
        _originalColor = _sr.color;
        _currentHealth = maxHealth;
    }

    private void Start()
    {
        InitSlider();
    }

    // ── Detección de impactos ────────────────────────────────────────────────

    /// Detecta cuando un enemigo llega a la base.
    /// El Player necesita un Collider2D con Is Trigger = true.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        // Destruimos al enemigo a través de su sistema de salud para que
        // la animación de muerte y la notificación al WaveManager funcionen.
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(9999f);
        else
            Destroy(other.gameObject); // fallback si no tiene EnemyHealth

        TakeDamage(damagePerEnemy);
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// Aplica daño a la base. Puede llamarse también desde otros scripts.
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        UpdateSlider();
        StartCoroutine(HitFlashRoutine());

        if (_currentHealth <= 0f)
            StartCoroutine(BaseDestroyedRoutine());
    }

    // ── Corrutinas ───────────────────────────────────────────────────────────

    /// Parpadeo rojo para indicar que la base recibió daño.
    /// Tiempo no escalado: el flash dura lo mismo aunque timeScale cambie
    /// (pausa o cámara lenta del Game Over).
    private IEnumerator HitFlashRoutine()
    {
        _sr.color = hitColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        if (!_isDead) _sr.color = _originalColor;
    }

    /// Secuencia de derrota: flash rojo y notificación al GameManager.
    /// GameManager se encarga del efecto de cámara lenta y del estado.
    private IEnumerator BaseDestroyedRoutine()
    {
        _isDead = true;
        _sr.color = Color.red;

        // Pequeña pausa visual antes de ceder el control al GameManager.
        yield return new WaitForSeconds(0.3f);

        // Delegamos el Game Over al GameManager (cámara lenta + panel UI).
        GameManager.Instance?.TriggerGameOver();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void InitSlider()
    {
        if (healthSlider == null) return;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = maxHealth;
        healthSlider.value    = maxHealth;
    }

    private void UpdateSlider()
    {
        if (healthSlider != null)
            healthSlider.value = _currentHealth;
    }
}
