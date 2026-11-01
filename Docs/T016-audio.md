# T016 — Sonidos del sprint

El sonido se inicializa automáticamente al pulsar Play desde cualquier escena. No requiere asignaciones manuales en el Inspector. `Assets/Resources/SonidosSprint.asset` contiene los clips y los niveles por categoría; selecciónalo para cambiar la mezcla.

## Integración

- Menú principal: Trail of Crumbs, viento fantasmal y truenos lejanos ocasionales (primer trueno a los 12–22 segundos, después cada 30–45 segundos o más según la duración del clip). El trueno es ambientación sonora; no se ha añadido una animación de relámpagos.
- Botones: SonidoBoton, incluidos ajustes, navegación y controles del tráiler. El reproductor de clics persiste entre escenas para que no se corten al navegar.
- Puerta: apertura al entrar en una sección y cierre al regresar desde el aviso del target, con un retraso de 0,2 segundos configurable en Retraso Cierre Puerta. Se utilizan los recortes aportados por el usuario, abrePuerta.wav y door-close-soundbible (mp3cut.net).wav; Open, Creak 01 se conserva como alternativa sin reproducir ambas a la vez.
- Preview: ambiente nocturno y motor solo con el target detectado, respetando la opción existente de seguimiento extendido. Al perderlo se detienen; al recuperarlo vuelven y suena la confirmación.
- Motor: el mismo clip de ralentí para todo. El tono pasa suavemente de 1 a 1,3 según la velocidad real de conducción del controlador, independientemente del movimiento de la cámara RA. Al reiniciar se restablece el tono y suena el swoosh.
- Tráiler: mantiene su propia pista y su silencio local. No se reproduce música, ambiente ni motor del resto de escenas por encima.
- El volumen general existente sigue controlando todas las fuentes. Un valor de cero silencia también las confirmaciones. La preferencia se guarda al pausar o cerrar la aplicación.
- Sin claxon.

## Preparación de archivos

Los WAV iniciales se han convertido desde las descargas originales a PCM de 16 bits y 48 kHz. Los sonidos de puerta se han sustituido por copias exactas de abrePuerta.wav y door-close-soundbible (mp3cut.net).wav, conservando sus recortes y niveles; Unity los importa a 48 kHz. Los originales de Descargas permanecen intactos. Se han equilibrado los niveles de los clips con límite de picos; los valores finales de mezcla están en SonidosSprint.asset. Los bucles de viento, ambiente nocturno, música y motor llevan un fundido cruzado entre el final y el principio. Esto reduce discontinuidades; la continuidad musical y la mezcla final deben valorarse escuchando en el móvil.

Unity importa los ambientes y la música comprimidos en streaming, y los efectos cortos precargados. `Proyecto > Audio > Preparar catálogo del sprint` reconstruye las referencias sin restablecer los niveles personalizados ni modificar las escenas.

## Comprobación

`Proyecto > Audio > Validar integración del sprint` prueba las escenas reales en Play Mode: catálogo, reproducción del menú, deslizador, puerta, navegación, persistencia, detección/pérdida/recuperación simuladas del target, variación del motor, reinicio, pausa, silencio del tráiler y regreso sin duplicados. Restaura el volumen anterior y vuelve a la escena de edición al terminar. Informe local: `Temp/AudioTrabajo/validacion.txt`.

Para la entrega, comprobar además con el target físico y el móvil: volumen 100/50/0 %, motor al parar/conducir, ausencia de cortes molestos en los bucles y balance frente al tráiler.

## Procedencia

Archivos aportados por el usuario desde Descargas; se conservan aquí sus nombres para identificar los originales:

| Archivo en el proyecto | Original |
|---|---|
| UI_Boton.wav | SonidoBoton.wav |
| UI_TargetDetectado.wav | ES_User Interface, Alert, Notification, Attention, Positive, Funny, Short 08 - Epidemic Sound.wav |
| UI_Reiniciar.wav | ES_Swooshes, Swish, Short, Short Household - Epidemic Sound - 0835-2244.wav |
| Puerta_Abrir.wav | abrePuerta.wav (recorte aportado por el usuario) |
| Puerta_Cerrar.wav | door-close-soundbible (mp3cut.net).wav (aportado por el usuario) |
| Puerta_Abrir_Alternativa.wav | ES_Doors, Wood, Open, Creak 01 - Epidemic Sound.wav |
| Ambiente_Preview.wav | ES_Ambience, Desert, Night, Crickets, Wind, Quilomene Wildlife Area - Epidemic Sound - 0000-54000.wav |
| Ambiente_Menu.wav | ES_Wind, Designed, Spooky Winds, Tonal, Whispers, Ghosts - Epidemic Sound.wav |
| Ambiente_Trueno.wav | ES_Weather, Thunder, Tropical, Distant Thunder, Booming Thunder 02 - Epidemic Sound.wav |
| Motor_Furgoneta.wav | ES_Vehicles, Car, Nissan Qashqai I 1.6 Engine, Idle, Loop - Epidemic Sound.wav |
| Musica_Menu.wav | ES_Trail of Crumbs - Martin Landstrom.wav |
