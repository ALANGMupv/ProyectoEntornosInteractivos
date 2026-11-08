
using UnityEngine;

public class MovimientoFantasmaB : MonoBehaviour
{
    [Header("Movimiento")]
    public float distanciaMinima = 1.5f;
    public float distanciaMaxima = 3f;
    public float velocidadGiro = 40f;
    public float velocidadMovimiento = 0.5f;
    [Tooltip("Segundos para acelerar y frenar: evita los cambios bruscos de direccion.")]
    public float suavizado = 0.8f;

    private Transform camara;
    private Quaternion rotacionBase;

    private float alturaBase;
    private float angulo;
    private float radio;
    private float altura;

    private float anguloDestino;
    private float radioDestino;
    private float alturaDestino;

    private float velocidadAngulo;
    private float velocidadRadio;
    private float velocidadAltura;

    public void Inicializar(Transform camaraJugador)
    {
        camara = camaraJugador;

        // Guardar la orientacion original del prefab
        rotacionBase = transform.rotation;

        alturaBase = camara.position.y;

        Vector3 diferencia =
            transform.position - camara.position;

        angulo = Mathf.Atan2(
            diferencia.z, diferencia.x
        ) * Mathf.Rad2Deg;

        radio = Mathf.Clamp(
            new Vector2(
                diferencia.x, diferencia.z
            ).magnitude,
            distanciaMinima,
            distanciaMaxima
        );

        altura = Mathf.Clamp(
            transform.position.y,
            alturaBase - 0.2f,
            alturaBase + 0.4f
        );

        ElegirDestino();
    }

    void LateUpdate()
    {
        if (camara == null)
            return;

        // Cambiar suavemente de direccion: acelera y frena en vez
        // de arrancar o girar en seco. La velocidad maxima no cambia.
        angulo = Mathf.SmoothDampAngle(
            angulo,
            anguloDestino,
            ref velocidadAngulo,
            suavizado,
            velocidadGiro
        );

        // Acercarse y alejarse del jugador
        radio = Mathf.SmoothDamp(
            radio,
            radioDestino,
            ref velocidadRadio,
            suavizado,
            velocidadMovimiento
        );

        // Variar la altura
        altura = Mathf.SmoothDamp(
            altura,
            alturaDestino,
            ref velocidadAltura,
            suavizado,
            velocidadMovimiento
        );

        float radianes = angulo * Mathf.Deg2Rad;

        // Moverse alrededor del jugador
        transform.position = new Vector3(
            camara.position.x +
                Mathf.Cos(radianes) * radio,
            altura,
            camara.position.z +
                Mathf.Sin(radianes) * radio
        );

        // Mirar hacia el jugador sin inclinarse
        Vector3 direccion =
            camara.position - transform.position;

        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion mirarJugador =
                Quaternion.LookRotation(
                    direccion, Vector3.up
                );

            transform.rotation =
                mirarJugador * rotacionBase;
        }

        // Elegir un nuevo destino al llegar
        if (
            Mathf.Abs(Mathf.DeltaAngle(
                angulo, anguloDestino
            )) < 1f &&
            Mathf.Abs(radio - radioDestino) < 0.05f &&
            Mathf.Abs(altura - alturaDestino) < 0.05f
        )
        {
            ElegirDestino();
        }
    }

    private void ElegirDestino()
    {
        anguloDestino =
            angulo + Random.Range(-120f, 120f);

        radioDestino = Random.Range(
            distanciaMinima,
            distanciaMaxima
        );

        alturaDestino = alturaBase +
            Random.Range(-0.2f, 0.4f);
    }
}
