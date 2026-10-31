# T013 — Tráiler en realidad aumentada

Abre `Assets/Escenas/Trailer.unity` y pulsa Play. Apunta la cámara a `Assets/Texturas/Targets/target_trailer.jpg`, impresa o en otra pantalla. Usa la misma imagen que muestra el aviso del menú principal.

El vídeo asignado es `Assets/Videos/trailer_pick_a_boo.mp4`. El panel y los controles se colocan sobre el target. Incluye reproducción/pausa, búsqueda con slider, contador de tiempo, silencio y repetición al terminar. La flecha de vuelta al menú permanece en pantalla. Al perder el target se pausa el vídeo; al recuperarlo continúa si estaba reproduciéndose. Si estaba pausado, conserva la pausa.

En el objeto `TrailerRA`, `Ancho Target Metros` indica el ancho físico de la imagen (por defecto 15 cm). Ajusta este valor a la imagen que uses. `Probar Sin Target` solo tiene efecto en el editor: permite revisar el vídeo y pulsar los controles sin cámara; para RA debe quedar desactivado.

## Validación

`Proyecto > Trailer > Validar controles sin cámara` ejecuta las pruebas en Play Mode y vuelve al editor al terminar. Revisa clip, preparación, reproducción, pausa, silencio, búsqueda pausada, visibilidad de controles, pérdida/recuperación simulada del target, final, repetición y retorno al menú. Resultado local: `Temp/TrailerTrabajo/validacion.txt`.

`Proyecto > Trailer > Validar arranque RA` comprueba la inicialización de Vuforia, creación del target y preparación del vídeo con el modo RA real. Resultado local: `Temp/TrailerTrabajo/validacionRA.txt`. La detección física y el audio en el dispositivo de entrega deben comprobarse con la cámara y el altavoz de ese dispositivo.

`Proyecto > Trailer > Reconstruir escena` regenera la escena con sus referencias; sobrescribe su diseño guardado. Guarda primero cualquier cambio que quieras conservar. La copia previa se guarda en `Temp/TrailerTrabajo` (carpeta temporal, excluida de Git).

El paquete local de Vuforia no se incluye en Git porque supera 100 MB. Para abrir el proyecto en otro equipo, coloca `com.ptc.vuforia.engine-10.17.4.tgz` en `Packages`.
`Proyecto > Trailer > Aplicar estilo compartido` actualiza la interfaz existente con los mismos sprites de flecha, botones redondeados y papel que las demás escenas, sin reconstruir sus objetos ni modificar los controles.
El panel mide por defecto el 80 % del ancho del target, con escala física fija. El campo Ancho Panel Respecto Target permite ajustar ese tamaño. Al acercar la cámara se ve más grande, como cualquier objeto RA; no cambia de escala para encajar en pantalla. El primer fotograma se muestra en pausa antes de pulsar Reproducir.

