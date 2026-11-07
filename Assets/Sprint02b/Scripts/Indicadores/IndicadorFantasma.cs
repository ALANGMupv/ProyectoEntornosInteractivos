using UnityEngine;

/// <summary>
/// Un indicador en el borde de la pantalla: la flecha apunta hacia el fantasma
/// y el icono se queda un poco hacia dentro, sin girar.
/// </summary>
public class IndicadorFantasma : MonoBehaviour
{
    [SerializeField] private RectTransform flecha;
    [SerializeField] private RectTransform icono;

    [Tooltip("Distancia del icono a la flecha, hacia el centro de la pantalla.")]
    [SerializeField] private float separacionIcono = 120f;

    [Tooltip("Corrección si el dibujo de la flecha no apunta exactamente hacia arriba.")]
    [SerializeField] private float correccionAngulo = 7f;

    private RectTransform rect;

    public void Mostrar(Vector2 posicion, Vector2 direccion)
    {
        if (rect == null)
            rect = (RectTransform)transform;

        gameObject.SetActive(true);
        rect.anchoredPosition = posicion;

        // El sprite de la flecha apunta hacia arriba
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        flecha.localRotation = Quaternion.Euler(0f, 0f, angulo - 90f + correccionAngulo);

        icono.anchoredPosition = -direccion * separacionIcono;
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }
}
