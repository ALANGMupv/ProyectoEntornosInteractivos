
using UnityEngine;

public class ManipulacionModelos : MonoBehaviour
{
    public SelectorModelos selector;

    [Header("Rotacion")]
    public float velocidadRotacion = 0.3f;

    [Header("Escala")]
    public float velocidadEscala = 0.005f;
    public float escalaMinima = 0.5f;
    public float escalaMaxima = 3f;

    void Update()
    {
        if (selector == null || selector.ModeloActual == null)
            return;

        Transform modelo = selector.ModeloActual.transform;

        // Controles tactiles
        if (Input.touchCount == 1)
        {
            Touch dedo = Input.GetTouch(0);

            if (dedo.phase == TouchPhase.Moved)
                Rotar(modelo, dedo.deltaPosition.x);
        }
        else if (Input.touchCount == 2)
        {
            Touch dedo1 = Input.GetTouch(0);
            Touch dedo2 = Input.GetTouch(1);

            float anterior = Vector2.Distance(
                dedo1.position - dedo1.deltaPosition,
                dedo2.position - dedo2.deltaPosition
            );

            float actual = Vector2.Distance(
                dedo1.position, dedo2.position
            );

            Escalar(modelo, (actual - anterior) * velocidadEscala);
        }
        // Controles con raton en Unity
        else if (Input.touchCount == 0)
        {
            if (Input.GetMouseButton(0))
                Rotar(modelo, Input.GetAxis("Mouse X") * 10f);

            float rueda = Input.mouseScrollDelta.y;

            if (rueda != 0)
                Escalar(modelo, rueda * 0.1f);
        }
    }

    void Rotar(Transform modelo, float movimiento)
    {
        modelo.Rotate(
            Vector3.up,
            -movimiento * velocidadRotacion,
            Space.World
        );
    }

    void Escalar(Transform modelo, float cambio)
    {
        float factor = Mathf.Clamp(
            1f + cambio,
            0.1f,
            10f
        );

        Vector3 nuevaEscala = modelo.localScale * factor;

        // Limites relativos a la escala inicial del modelo
        int indice = System.Array.IndexOf(
            selector.modelos,
            selector.ModeloActual
        );

        if (indice < 0)
            return;

        float referencia = escalasIniciales[indice].magnitude;
        float proporcion = nuevaEscala.magnitude / referencia;

        if (proporcion >= escalaMinima &&
            proporcion <= escalaMaxima)
        {
            modelo.localScale = nuevaEscala;
        }
    }

    private Vector3[] escalasIniciales;

    void Start()
    {
        escalasIniciales = new Vector3[selector.modelos.Length];

        for (int i = 0; i < selector.modelos.Length; i++)
        {
            escalasIniciales[i] =
                selector.modelos[i].transform.localScale;
        }
    }
}
