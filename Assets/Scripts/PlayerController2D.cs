using UnityEngine;
using UnityEngine.InputSystem;

/// Rota el fuerte/torreta para apuntar hacia la posición del puntero del mouse.
/// Requiere un componente PlayerInput configurado con "Send Messages" y una
/// acción "Look" mapeada a Mouse/position.
[RequireComponent(typeof(PlayerInput))]
public class PlayerController2D : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector2 _pointerScreenPos;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    // PlayerInput (Send Messages) invoca este método cuando la acción "Look" cambia.
    private void OnLook(InputValue value)
    {
        _pointerScreenPos = value.Get<Vector2>();
    }

    private void Update()
    {
        AimAtPointer();
    }

    private void AimAtPointer()
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(_pointerScreenPos.x, _pointerScreenPos.y, _mainCamera.nearClipPlane));

        Vector2 direction = (Vector2)worldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
