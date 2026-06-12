# Marea de Hierro

Tower Defense de acción en 2D desarrollado en Unity. El jugador controla el cañón de un fuerte costero y debe repeler oleadas de barcos acorazados que avanzan por el mar hacia sus muros. Proyecto final del curso de Programación de Videojuegos.

## Descripción del juego

La fortaleza está en el centro del mapa y los enemigos aparecen desde distintos puntos del océano, navegando directo hacia ella. El jugador apunta el cañón con el ratón (rotación libre en 360 grados) y dispara proyectiles con el clic izquierdo. Cada barco que logra impactar contra el fuerte le resta vida a la base; si la barra de vida llega a cero, el fuerte cae y la partida termina. Si el jugador destruye todas las oleadas configuradas, gana la partida.

### Controles

| Acción | Entrada |
|---|---|
| Apuntar el cañón | Mover el ratón |
| Disparar | Clic izquierdo |
| Pausar / reanudar | Escape |

### Condiciones de fin de partida

- **Victoria**: se completan todas las oleadas y no queda ningún enemigo vivo.
- **Derrota**: la vida de la base llega a cero. Antes de mostrar la pantalla de derrota el juego hace un efecto de cámara lenta para remarcar el momento.

## Pantallas

1. **Menú principal** (`MainScene`): fondo ilustrado con las opciones JUGAR, INSTRUCCIONES y SALIR. Los botones son zonas invisibles colocadas sobre los textos que ya vienen dibujados en el arte, y las transiciones entre paneles se hacen con fundidos de CanvasGroup.
2. **Instrucciones**: panel con el texto de cómo jugar dentro de un marco ilustrado, con scroll vertical para leer el contenido completo, y botón VOLVER.
3. **Juego** (`SampleScene`): la escena de combate. El HUD muestra la oleada actual y su nombre, el estado (preparando, en curso, despejando), una cuenta regresiva entre oleadas, el número de enemigos vivos y la barra de vida de la base.
4. **Pausa, Derrota y Victoria**: paneles superpuestos que aparecen con fundido según el estado del juego, con botones para reanudar, reiniciar, volver al menú o salir.

## Tecnología

- **Motor**: Unity 6000.4.4f1
- **Render**: Universal Render Pipeline (URP) en su configuración 2D
- **Entrada**: Input System nuevo de Unity (`com.unity.inputsystem`), con `PlayerInput` en modo Send Messages
- **UI**: uGUI con TextMeshPro
- **Lenguaje**: C#. Todo el código y los comentarios están en español.

### Cómo abrir el proyecto

1. Clonar el repositorio.
2. Abrirlo desde Unity Hub con la versión 6000.4.4f1 (u otra del ciclo Unity 6 compatible).
3. Las escenas registradas en Build Settings son `MainScene` (índice 0, menú) y `SampleScene` (índice 1, juego). Para probar, abrir cualquiera de las dos y entrar en Play Mode.

## Arquitectura

El código vive en `Assets/Scripts/` dentro del ensamblado único `Assembly-CSharp`, sin namespaces. La configuración de niveles (oleadas, vida, velocidades, referencias de UI) se hace por campos serializados en el Inspector, de modo que se puede balancear el juego sin tocar código.

### Máquina de estados central

`GameManager` es un singleton que mantiene el estado global de la partida con el enum `GameState` (Playing, Paused, GameOver, Victory). Es el único script que modifica `Time.timeScale`: lo pone en 0 al pausar o ganar, lo reduce gradualmente en la secuencia de derrota y lo restaura a 1 al reiniciar o cambiar de escena. Cuando el estado cambia dispara el evento `OnStateChanged`, al que se suscribe la UI.

### Sistema de oleadas

`WaveManager` orquesta las oleadas con corrutinas y su propio enum `WaveState` (Waiting, Spawning, WaitingForClear, AllComplete). Cada oleada se define en el Inspector mediante las clases serializables `Wave` y `EnemyGroup`: nombre, grupos de enemigos con su cantidad e intervalo de aparición. Los enemigos de una oleada se mezclan en una lista única (barajada con Fisher-Yates) y se instancian en puntos de aparición aleatorios, orientados hacia el fuerte. El manager lleva la cuenta de enemigos vivos y, al despejarse la última oleada, notifica la victoria al `GameManager`.

### Comunicación entre sistemas

Se usan tres mecanismos según el caso:

- **Eventos**: `GameManager.OnStateChanged` → `UIScreenManager` muestra u oculta los paneles de Pausa, Derrota y Victoria. La suscripción se hace en `Start()` y no en `OnEnable()`, porque en `OnEnable()` la instancia del singleton todavía no existe.
- **Polling**: `HUDController` lee cada frame las propiedades públicas de `WaveManager.Instance` (oleada actual, estado, cuenta regresiva, enemigos vivos) y refresca los textos del HUD.
- **Llamadas directas a singletons**: `BaseHealth` avisa la derrota, `WaveManager` la victoria y `EnemyHealth` descuenta enemigos del conteo global.

### Flujo de daño

El sistema de combate se apoya en los tags `Player` y `Enemy`:

1. `PlayerShoot` instancia el proyectil en la boca del cañón, con un tiempo de recarga entre disparos y un destello de fogonazo.
2. `Projectile` avanza con `rb.linearVelocity` en `FixedUpdate` y, al entrar en el trigger de un objeto con tag `Enemy`, le aplica daño y se desvanece.
3. `EnemyHealth` resta la vida, parpadea en rojo al recibir el golpe y, al morir, reproduce una animación de encogimiento y desvanecimiento; justo antes de destruirse notifica al `WaveManager` para que el conteo de enemigos no se pierda.
4. Si un enemigo alcanza la base, `BaseHealth` detecta el trigger, descuenta vida del fuerte, actualiza la barra de vida y elimina al enemigo a través de su propio sistema de salud (`TakeDamage(9999)`), de manera que la muerte pasa por el flujo normal y el conteo de oleadas sigue siendo correcto.

### Convenciones de implementación

- Todo el feedback visual (fundidos, pulsos de texto, flashes de daño, apariciones con escala) está hecho con corrutinas, sin librerías externas de tweening.
- La UI que aparece con el tiempo detenido (pausa, derrota, victoria) anima con `Time.unscaledDeltaTime` y `WaitForSecondsRealtime`; el gameplay usa tiempo escalado normal.
- El movimiento físico usa `rb.linearVelocity` dentro de `FixedUpdate`, la API actual de Unity 6.
- Los métodos públicos de los botones (`OnJugarPressed`, `OnRestartPressed`, etc.) se asignan desde el Inspector en los eventos OnClick.

## Scripts

| Script | Responsabilidad |
|---|---|
| `GameManager.cs` | Máquina de estados de la partida, control de `Time.timeScale`, pausa, reinicio, cambio de escena y salida. |
| `WaveManager.cs` | Definición y secuenciado de oleadas, aparición de enemigos, conteo de vivos y aviso de victoria. |
| `BaseHealth.cs` | Vida del fuerte, detección de enemigos que llegan a la base, barra de vida y secuencia de derrota. |
| `EnemyHealth.cs` | Vida del enemigo, flash de daño, animación de muerte y notificación al WaveManager. |
| `EnemyMovement.cs` | Avance del barco hacia el fuerte con física kinemática y animación de aparición. |
| `PlayerController2D.cs` | Rotación del cañón hacia el puntero del ratón (acción Look del Input System). |
| `PlayerShoot.cs` | Disparo con cadencia y fogonazo (acción Fire del Input System). |
| `Projectile.cs` | Movimiento del proyectil, impacto por trigger contra enemigos y autodestrucción con fade. |
| `HUDController.cs` | Textos del HUD durante el combate: oleada, estado, cuenta regresiva y enemigos vivos. |
| `UIScreenManager.cs` | Paneles de Pausa, Derrota y Victoria con sus animaciones y botones. |
| `MainMenuManager.cs` | Menú principal: transición entre paneles, carga del juego y salida de la aplicación. |

## Estructura del proyecto

```
Assets/
├── Scenes/          MainScene (menú) y SampleScene (juego)
├── Scripts/         Los 11 scripts de C# descritos arriba
├── Sprites/         Arte del juego: fuerte, barcos, cañón, proyectil,
│                    fondos del menú y pantallas de victoria/derrota
├── Prefabs/         Prefabs de enemigo y proyectil
└── *.inputactions   Mapas de acciones del Input System (Look, Fire)
```
