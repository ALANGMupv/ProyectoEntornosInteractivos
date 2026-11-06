
using UnityEngine;

public class MovimientoFantasmaB : MonoBehaviour
{
    [Header("Movimiento")]
    public float distanciaMinima = 1.5f;
    public float distanciaMaxima = 3f;
    public float velocidadGiro = 40f;
    public float velocidadMovimiento = 0.5f;

    private Transform camara;
    private float alturaBase;
    private float angulo;
    private float radio;
    private float altura;

    private float anguloDestino;
    private float radioDestino;
    private float alturaDestino;

    public void Inicializar(Transform camaraJugador)
    {
        camara = camaraJugador;
        alturaBase = camara.position.y;

        Vector3 diferencia = transform.position - camara.position;

        angulo = Mathf.Atan2(diferencia.z, diferencia.x)
                 * Mathf.Rad2Deg;

        radio = Mathf.Clamp(
            new Vector2(diferencia.x, diferencia.z).magnitude,
            distanciaMinima, distanciaMaxima
        );

        altura = Mathf.Clamp(
            transform.position.y,
            alturaBase - 0.2f,
            alturaBase + 0.4f
        );

        ElegirDestino();
    }

    void Update()
    {
        if (camara == null) return;

        angulo = Mathf.MoveTowardsAngle(
            angulo, anguloDestino,
            velocidadGiro * Time.deltaTime
        );

        radio = Mathf.MoveTowards(
            radio, radioDestino,
            velocidadMovimiento * Time.deltaTime
        );

        altura = Mathf.MoveTowards(
            altura, alturaDestino,
            velocidadMovimiento * Time.deltaTime
        );

        float radianes = angulo * Mathf.Deg2Rad;

        transform.position = new Vector3(
            camara.position.x + Mathf.Cos(radianes) * radio,
            altura,
            camara.position.z + Mathf.Sin(radianes) * radio
        );

        if (Mathf.Abs(Mathf.DeltaAngle(angulo, anguloDestino)) < 1f &&
            Mathf.Abs(radio - radioDestino) < 0.05f &&
            Mathf.Abs(altura - alturaDestino) < 0.05f)
        {
            ElegirDestino();
        }
    }

    private void ElegirDestino()
    {
        anguloDestino = angulo + Random.Range(-120f, 120f);
        radioDestino = Random.Range(distanciaMinima, distanciaMaxima);
        alturaDestino = alturaBase + Random.Range(-0.2f, 0.4f);
    }
}
