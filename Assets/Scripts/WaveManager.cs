using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Estado en el que puede encontrarse el WaveManager en cada momento.
// HUDController lo lee para mostrar mensajes distintos en pantalla.
// ---------------------------------------------------------------------------
public enum WaveState
{
    Waiting,          // Cuenta regresiva antes de la próxima oleada
    Spawning,         // Instanciando enemigos
    WaitingForClear,  // Todos spawneados, esperando que mueran
    AllComplete       // Victoria: no quedan más oleadas
}

// ---------------------------------------------------------------------------
// Grupo de enemigos dentro de una oleada (serializable para el Inspector).
// ---------------------------------------------------------------------------
[System.Serializable]
public class EnemyGroup
{
    [Tooltip("Prefab del enemigo que se instanciará.")]
    public GameObject prefab;

    [Tooltip("Cuántos enemigos de este tipo aparecen en esta oleada.")]
    [Min(1)] public int count = 5;
}

// ---------------------------------------------------------------------------
// Definición completa de una oleada (serializable para el Inspector).
// ---------------------------------------------------------------------------
[System.Serializable]
public class Wave
{
    [Tooltip("Nombre descriptivo (ej. 'Oleada 1 – Exploradores').")]
    public string waveName = "Nueva Oleada";

    [Tooltip("Lista de grupos de enemigos que componen esta oleada.")]
    public EnemyGroup[] enemyGroups;

    [Tooltip("Segundos entre cada spawn individual.")]
    [Min(0.1f)] public float spawnInterval = 1f;
}

// ---------------------------------------------------------------------------
// WaveManager — Gestor central de oleadas.
//
// Propiedades públicas que expone para el HUD:
//   CurrentWaveNumber  → número de oleada actual (base 1)
//   TotalWaves         → total de oleadas configuradas
//   CurrentWaveName    → nombre de la oleada activa
//   EnemiesAlive       → enemigos aún en escena
//   State              → enum WaveState actual
//   Countdown          → segundos restantes en la cuenta regresiva
// ---------------------------------------------------------------------------
public class WaveManager : MonoBehaviour
{
    // Instancia estática: EnemyHealth y HUDController la usan para comunicarse.
    public static WaveManager Instance { get; private set; }

    // ── Configuración Inspector ──────────────────────────────────────────────
    [Header("Oleadas")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Puntos de Aparición")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Objetivo")]
    [Tooltip("El fuerte hacia el que miran los barcos al aparecer. " +
             "Si se deja vacío, se busca automáticamente por tag 'Player'.")]
    [SerializeField] private Transform target;

    // ── Estado interno (se ve en Inspector en Play Mode) ─────────────────────
    [Header("Estado — solo lectura en Play Mode")]
    [SerializeField] private int  _currentWaveIndex = 0;
    [SerializeField] private int  _enemiesAlive     = 0;
    [SerializeField] private WaveState _state       = WaveState.Waiting;
    [SerializeField] private float _countdown       = 0f;

    // ── Propiedades públicas para el HUD ─────────────────────────────────────
    public int       CurrentWaveNumber => _currentWaveIndex + 1;
    public int       TotalWaves        => waves != null ? waves.Length : 0;
    public string    CurrentWaveName   => IsValidIndex(_currentWaveIndex)
                                             ? waves[_currentWaveIndex].waveName
                                             : "";
    public int       EnemiesAlive      => _enemiesAlive;
    public WaveState State             => _state;
    public float     Countdown         => _countdown;

    // ── Ciclo de vida ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No hay oleadas configuradas.");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No hay SpawnPoints asignados.");
            return;
        }

        // Mismo fallback que EnemyMovement: si el diseñador no asigna el
        // objetivo en el Inspector, lo buscamos por tag "Player".
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning("[WaveManager] No se encontró el objetivo (tag 'Player'). " +
                                 "Los barcos aparecerán sin orientar.");
        }

        StartCoroutine(WaveSequenceRoutine());
    }

    // ── API pública ──────────────────────────────────────────────────────────

    /// EnemyHealth llama a esto justo antes de destruir al enemigo.
    public void RegisterEnemyDefeated()
    {
        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
    }

    // ── Corrutinas principales ───────────────────────────────────────────────

    /// Orquesta la secuencia completa de oleadas.
    private IEnumerator WaveSequenceRoutine()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            _currentWaveIndex = i;

            // --- Cuenta regresiva ---
            _state    = WaveState.Waiting;
            _countdown = timeBetweenWaves;

            while (_countdown > 0f)
            {
                _countdown -= Time.deltaTime;
                yield return null;
            }
            _countdown = 0f;

            // --- Spawn de enemigos ---
            _state = WaveState.Spawning;
            yield return StartCoroutine(SpawnWaveRoutine(waves[i]));

            // --- Esperar despeje ---
            _state = WaveState.WaitingForClear;
            yield return new WaitUntil(() => _enemiesAlive <= 0);
        }

        _state = WaveState.AllComplete;
        Debug.Log("[WaveManager] ¡Todas las oleadas completadas! Victoria.");

        // Notificamos al GameManager para que muestre el panel de Victoria.
        GameManager.Instance?.TriggerVictory();
    }

    /// Instancia todos los enemigos de una oleada con el intervalo configurado.
    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        List<GameObject> spawnList = BuildShuffledSpawnList(wave);

        foreach (GameObject prefab in spawnList)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[WaveManager] Prefab nulo detectado. Revisa el Inspector.");
                continue;
            }

            SpawnEnemy(prefab);
            _enemiesAlive++;

            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private void SpawnEnemy(GameObject prefab)
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(prefab, point.position, AimAtTarget(point.position));
    }

    /// Rotación que orienta la proa hacia el objetivo. Los sprites de los
    /// barcos están dibujados apuntando a la derecha (+X), por lo que el
    /// ángulo de Atan2 se usa directo, sin offset.
    private Quaternion AimAtTarget(Vector3 from)
    {
        if (target == null) return Quaternion.identity;

        Vector2 dir   = (Vector2)(target.position - from);
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    /// Mezcla todos los grupos en una sola lista plana (Fisher-Yates).
    private List<GameObject> BuildShuffledSpawnList(Wave wave)
    {
        List<GameObject> list = new List<GameObject>();
        foreach (EnemyGroup g in wave.enemyGroups)
        {
            if (g.prefab == null) continue;
            for (int i = 0; i < g.count; i++) list.Add(g.prefab);
        }
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private bool IsValidIndex(int index) =>
        waves != null && index >= 0 && index < waves.Length;

    // ── Gizmos (solo en el Editor) ───────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.85f);
        foreach (Transform p in spawnPoints)
            if (p != null) Gizmos.DrawSphere(p.position, 0.3f);
    }
}
