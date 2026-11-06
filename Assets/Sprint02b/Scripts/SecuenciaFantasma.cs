
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
            Debug.LogError("Faltan referencias en SecuenciaFantasma.");
            return;
        }

        secuenciaIniciada = true;
        StartCoroutine(ReproducirSecuencia());
    }

    private IEnumerator ReproducirSecuencia()
    {
        // 1. Activar la aparicion
        fantasmaA.ActivarFantasma();

        Debug.Log("Fantasma A: apareciendo");

        // 2. Esperar hasta que llegue a Flotar
        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0).IsName("Flotar")
        );

        yield return new WaitForSeconds(tiempoFlotando);

        // 3. Activar la huida
        fantasmaA.EsconderFantasma();

        Debug.Log("Fantasma A: huyendo");

        // 4. Esperar a que comience Huir
        yield return new WaitUntil(() =>
            animatorA.GetCurrentAnimatorStateInfo(0).IsName("Huir")
        );

        // 5. Esperar hasta que termine Huir
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo estado =
                animatorA.GetCurrentAnimatorStateInfo(0);

            return estado.IsName("Huir") &&
                   estado.normalizedTime >= 1f;
        });

        // 6. Crear el fantasma jugable
        CrearFantasmaB();

        // 7. Desactivar el fantasma de la cinematica
        fantasmaA.gameObject.SetActive(false);

        Debug.Log("Cinematica finalizada");
    }

    private void CrearFantasmaB()
    {
        // Distancia y direccion aleatorias alrededor de la camara
        float angulo = Random.Range(0f, Mathf.PI * 2f);

        float distancia = Random.Range(
            distanciaMinima, distanciaMaxima
        );

        Vector3 posicion = camaraRA.position + new Vector3(
            Mathf.Cos(angulo) * distancia,
            desplazamientoAltura,
            Mathf.Sin(angulo) * distancia
        );

        GameObject fantasmaB = null;

        // Intentar crear un anclaje espacial de Vuforia
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

                fantasmaB.transform.localPosition = Vector3.zero;
                fantasmaB.transform.localRotation = Quaternion.identity;
            }
        }

        // Modo provisional si no hay soporte de anclajes
        if (fantasmaB == null)
        {
            fantasmaB = Instantiate(
                prefabFantasmaB,
                posicion,
                Quaternion.identity
            );

            Debug.LogWarning(
                "Fantasma B creado sin anclaje espacial"
            );
        }

        MovimientoFantasmaB movimiento =
            fantasmaB.GetComponent<MovimientoFantasmaB>();

        if (movimiento != null)
        {
            movimiento.Inicializar(camaraRA);
        }

        ObjetivoActual = fantasmaB.transform;

        Debug.Log("Fantasma B creado en: " + posicion);
    }
}
