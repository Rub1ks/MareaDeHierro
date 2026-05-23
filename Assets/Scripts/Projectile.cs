using System.Collections;
using UnityEngine;

/// Mueve el proyectil hacia adelante (transform.up) usando rb.linearVelocity en
/// FixedUpdate. Se destruye tras 'lifetime' segundos o al entrar en trigger con
/// un objeto etiquetado "Enemy". Incluye fade-out por corrutina como feedback visual.
/// El Rigidbody2D debe ser Dynamic con Gravity Scale = 0.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float fadeOutDuration = 0.08f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Vector2 _travelDirection;
    private bool _isDestroying;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        // El proyectil viaja hacia el "frente" local del transform al ser instanciado.
        _travelDirection = transform.up;
    }

    private void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private void FixedUpdate()
    {
        if (_isDestroying) return;
        _rb.linearVelocity = _travelDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            StartCoroutine(DestroyRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        if (_isDestroying) yield break;
        _isDestroying = true;

        _rb.linearVelocity = Vector2.zero;

        // Fade-out visual antes de destruir el objeto.
        float elapsed = 0f;
        Color original = _sr.color;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            _sr.color = new Color(original.r, original.g, original.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        Destroy(gameObject);
    }
}
