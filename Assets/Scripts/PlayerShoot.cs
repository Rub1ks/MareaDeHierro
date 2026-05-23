using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Dispara proyectiles desde el firePoint cuando se activa la acción "Fire".
/// Requiere un componente PlayerInput configurado con "Send Messages" y una
/// acción "Fire" mapeada a Mouse/leftButton o similar.
[RequireComponent(typeof(PlayerInput))]
public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldown = 0.25f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer muzzleFlashRenderer;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    private bool _canFire = true;

    private void Start()
    {
        if (muzzleFlashRenderer != null)
            muzzleFlashRenderer.enabled = false;
    }

    // PlayerInput (Send Messages) invoca este método cuando la acción "Fire" se activa.
    private void OnFire(InputValue value)
    {
        if (value.isPressed && _canFire)
            StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        _canFire = false;

        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        if (muzzleFlashRenderer != null)
            StartCoroutine(MuzzleFlashRoutine());

        yield return new WaitForSeconds(fireCooldown);
        _canFire = true;
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        muzzleFlashRenderer.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        muzzleFlashRenderer.enabled = false;
    }
}
