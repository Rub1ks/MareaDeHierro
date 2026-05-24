using System.Collections;
using UnityEngine;

/// Mueve al enemigo hacia un objetivo (la base del jugador) usando
/// rb.linearVelocity en FixedUpdate sobre un Rigidbody2D Kinematic.
/// Incluye una corrutina de aparición (scale-in) al instanciarse.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Objetivo")]
    [Tooltip("Arrastra aquí el Transform de la base/fuerte del jugador.")]
    [SerializeField] private Transform target;

    [Header("Animación de aparición")]
    [SerializeField] private float spawnDuration = 0.3f;

    private Rigidbody2D _rb;
    private bool _isActive; // Falso mientras dura la animación de spawn

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Si el diseñador no asigna el target en el Inspector,
        // lo buscamos automáticamente por tag "Player".
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning($"{name}: no se encontró ningún objeto con tag 'Player'.");
        }

        StartCoroutine(SpawnRoutine());
    }

    private void FixedUpdate()
    {
        // Mientras el spawn no termina o no hay objetivo, el enemigo está quieto.
        if (!_isActive || target == null)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Calculamos la dirección normalizada hacia el objetivo y aplicamos velocidad.
        Vector2 direction = ((Vector2)target.position - _rb.position).normalized;
        _rb.linearVelocity = direction * moveSpeed;
    }

    /// Animación de escala 0 → 1 al aparecer en escena.
    private IEnumerator SpawnRoutine()
    {
        _isActive = false;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnDuration);
            // Curva suave de aceleración al inicio (EaseIn)
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t * t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _isActive = true;
    }

    /// Detiene el movimiento desde fuera (llamado por EnemyHealth al morir).
    public void StopMovement()
    {
        _isActive = false;
        _rb.linearVelocity = Vector2.zero;
    }
}
