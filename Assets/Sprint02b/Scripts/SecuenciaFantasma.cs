
using System.Collections;
using UnityEngine;
using Vuforia;

public class SecuenciaFantasma : MonoBehaviour
{
    [Header("Fase 1 - Cinematica")]
    [SerializeField] private AnimacionFantasma fantasmaA;
    [SerializeField] private Animator animatorA;
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

    public void IniciarSecuencia()
    {
        if (secuenciaIniciada)
            return;

        if (fantasmaA == null || animatorA == null ||
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
        // Aparicion del Fantasma A
        fantasmaA.ActivarFantasma();

        Debug.Log("Fantasma A: apareciendo");

        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0)
                .IsName("Flotar")
        );

        // Esperar mientras flota
        yield return new WaitForSeconds(tiempoFlotando);

        // Activar huida
        fantasmaA.EsconderFantasma();

        Debug.Log("Fantasma A: huyendo");

        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0)
                .IsName("Huir")
        );

        // Esperar hasta que termine la huida
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo estado =
                animatorA.GetCurrentAnimatorStateInfo(0);

            return estado.IsName("Huir") &&
                   estado.normalizedTime >= 1f;
        });

        // Crear Fantasma B
        CrearFantasmaB();

        // Ocultar Fantasma A
        fantasmaA.gameObject.SetActive(false);

        Debug.Log("Cinematica finalizada");
    }

    private void CrearFantasmaB()
    {
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

        // Crear un anclaje espacial si esta disponible
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

                // Respetar la rotacion del prefab
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

        // Referencia para audio, indicadores y captura
        ObjetivoActual = fantasmaB.transform;

        Debug.Log(
            "Fantasma B creado en: " + posicion
        );
    }
}
