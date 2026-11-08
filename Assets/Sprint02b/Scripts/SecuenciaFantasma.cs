
using System.Collections;
using UnityEngine;
using Vuforia;

public class SecuenciaFantasma : MonoBehaviour
{
    [Header("Fase 1 - Cinematica")]
    [SerializeField] private AnimacionFantasma fantasmaA;
    [SerializeField] private Animator animatorA;
    [SerializeField] private Transform puntoAparicion;
    [SerializeField] private float tiempoFlotando = 2f;

    [Header("Fase 2 - Fantasma jugable")]
    [SerializeField] private GameObject prefabFantasmaB;
    [SerializeField] private Transform camaraRA;

    [Header("Zona de aparicion")]
    [SerializeField] private float distanciaMinima = 1.5f;
    [SerializeField] private float distanciaMaxima = 3f;
    [SerializeField] private float desplazamientoAltura = 0f;

    private bool secuenciaIniciada = false;

    public Transform ObjetivoActual { get; private set; }

    /// <summary>Si ya se ha escaneado este target y ha empezado su secuencia.</summary>
    public bool Iniciada => secuenciaIniciada;

    /// <summary>Si el fantasma de la cinemática ya ha salido disparado (empieza la persecución).</summary>
    public bool Huyendo { get; private set; }

    public void IniciarSecuencia()
    {
        if (secuenciaIniciada)
            return;

        if (fantasmaA == null || animatorA == null ||
            puntoAparicion == null ||
            prefabFantasmaB == null || camaraRA == null)
        {
            Debug.LogError(
                "Faltan referencias en SecuenciaFantasma"
            );
            return;
        }

        secuenciaIniciada = true;
        StartCoroutine(ReproducirSecuencia());
    }

    private IEnumerator ReproducirSecuencia()
    {
        // FASE 1: Aparicion
        fantasmaA.ActivarFantasma();

        Debug.Log("Fantasma A: apareciendo");

        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0)
                .IsName("Flotar")
        );

        // Permanecer flotando
        yield return new WaitForSeconds(tiempoFlotando);

        // Iniciar huida
        fantasmaA.EsconderFantasma();
        Huyendo = true;

        Debug.Log("Fantasma A: huyendo");

        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0)
                .IsName("Huir")
        );

        // Guardar la orientacion original
        Quaternion rotacionInicial = puntoAparicion.rotation;

        // Girar 180 grados mientras se reproduce Huir
        while (true)
        {
            AnimatorStateInfo estado =
                animatorA.GetCurrentAnimatorStateInfo(0);

            if (!estado.IsName("Huir") ||
                estado.normalizedTime >= 1f)
            {
                break;
            }

            float progreso = Mathf.Clamp01(
                estado.normalizedTime
            );

            // Giro alrededor del eje vertical global
            puntoAparicion.rotation =
                Quaternion.AngleAxis(
                    180f * progreso,
                    Vector3.up
                ) * rotacionInicial;

            yield return null;
        }

        // Completar exactamente la media vuelta
        puntoAparicion.rotation =
            Quaternion.AngleAxis(180f, Vector3.up)
            * rotacionInicial;

        // FASE 2: Crear fantasma jugable
        CrearFantasmaB();

        // Desactivar fantasma de la cinematica
        fantasmaA.gameObject.SetActive(false);

        Debug.Log("Cinematica finalizada");
    }

    private void CrearFantasmaB()
    {
        // Posicion aleatoria alrededor del jugador
        float angulo = Random.Range(
            0f, Mathf.PI * 2f
        );

        float distancia = Random.Range(
            distanciaMinima,
            distanciaMaxima
        );

        Vector3 posicion = camaraRA.position +
            new Vector3(
                Mathf.Cos(angulo) * distancia,
                desplazamientoAltura,
                Mathf.Sin(angulo) * distancia
            );

        GameObject fantasmaB = null;

        // Intentar crear un anclaje espacial
        if (VuforiaBehaviour.Instance != null &&
            VuforiaBehaviour.Instance.World.AnchorsSupported)
        {
            AnchorBehaviour anclaje =
                VuforiaBehaviour.Instance.ObserverFactory
                    .CreateAnchorBehaviour(
                        "Ancla_" + gameObject.name,
                        posicion,
                        Quaternion.identity
                    );

            if (anclaje != null)
            {
                fantasmaB = Instantiate(
                    prefabFantasmaB,
                    anclaje.transform
                );

                fantasmaB.transform.localPosition =
                    Vector3.zero;

                fantasmaB.transform.localRotation =
                    prefabFantasmaB.transform.localRotation;
            }
        }

        // Alternativa sin anclaje
        if (fantasmaB == null)
        {
            fantasmaB = Instantiate(
                prefabFantasmaB,
                posicion,
                prefabFantasmaB.transform.rotation
            );

            Debug.LogWarning(
                "Fantasma B creado sin anclaje espacial"
            );
        }

        // Activar movimiento aleatorio
        MovimientoFantasmaB movimiento =
            fantasmaB.GetComponent<MovimientoFantasmaB>();

        if (movimiento != null)
        {
            movimiento.Inicializar(camaraRA);
        }
        else
        {
            Debug.LogWarning(
                "Fantasma B no tiene MovimientoFantasmaB"
            );
        }

        // Referencia para los otros sistemas
        ObjetivoActual = fantasmaB.transform;

        Debug.Log(
            "Fantasma B creado en: " + posicion
        );
    }
}
