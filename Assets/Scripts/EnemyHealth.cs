using System.Collections;
using UnityEngine;

/// Gestiona la vida del enemigo.
/// Expone TakeDamage() para que Projectile.cs lo llame al impactar.
/// Usa corrutinas para el flash de daño y la animación de muerte.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyMovement))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 3f;

    [Header("Feedback visual - Golpe")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("Feedback visual - Muerte")]
    [SerializeField] private float deathDuration = 0.35f;

    private float _currentHealth;
    private SpriteRenderer _sr;
    private Color _originalColor;
    private bool _isDead;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _originalColor = _sr.color;
        _currentHealth = maxHealth;
    }

    /// Llamado por Projectile.cs cuando la bala impacta al enemigo.
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        // Feedback de golpe siempre que aún esté vivo.
        StartCoroutine(HitFlashRoutine());

        if (_currentHealth <= 0f)
            StartCoroutine(DeathRoutine());
    }

    /// Parpadeo rápido en rojo para indicar que recibió daño.
    private IEnumerator HitFlashRoutine()
    {
        _sr.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);

        // Solo restauramos si no murió durante el flash.
        if (!_isDead)
            _sr.color = _originalColor;
    }

    /// Detiene al enemigo y lo desvanece + encoge antes de destruirlo.
    private IEnumerator DeathRoutine()
    {
        _isDead = true;

        // Detenemos el movimiento notificando a EnemyMovement.
        GetComponent<EnemyMovement>().StopMovement();

        float elapsed = 0f;
        Color startColor = _originalColor;
        Vector3 startScale = transform.localScale;

        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathDuration);

            // Fade-out del alpha
            _sr.color = new Color(startColor.r, startColor.g, startColor.b,
                                  Mathf.Lerp(1f, 0f, t));

            // Scale-down simultáneo
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}
