using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Muestra un indicador en el borde de la pantalla por cada fantasma jugable que
/// queda fuera de cámara. Se actualiza en tiempo real al girar el móvil.
/// Va en un RectTransform que ocupa todo el Canvas.
/// </summary>
public class IndicadoresFantasma : MonoBehaviour
{
    [SerializeField] private Camera camaraRA;

    [Tooltip("Secuencias de los targets: de ellas sale el fantasma jugable de cada uno.")]
    [SerializeField] private SecuenciaFantasma[] secuencias;

    [SerializeField] private IndicadorFantasma prefabIndicador;

    [Tooltip("Separación del indicador respecto al borde de la pantalla.")]
    [SerializeField] private float margen = 110f;

    [Tooltip("Separación respecto al borde superior, para no tapar el HUD (menú, tiempo y contador).")]
    [SerializeField] private float margenSuperior = 260f;

    private readonly List<IndicadorFantasma> indicadores = new List<IndicadorFantasma>();
    private RectTransform zona;

    void Awake()
    {
        zona = (RectTransform)transform;

        if (camaraRA == null)
            camaraRA = Camera.main;
    }

    void LateUpdate()
    {
        int usados = 0;

        foreach (SecuenciaFantasma secuencia in secuencias)
        {
            if (secuencia == null)
                continue;

            Transform objetivo = secuencia.ObjetivoActual;
            if (objetivo == null || !objetivo.gameObject.activeInHierarchy)
                continue;

            if (!FueraDeCamara(objetivo.position, out Vector2 direccion))
                continue;

            Obtener(usados).Mostrar(PosicionEnBorde(direccion), direccion);
            usados++;
        }

        // Ocultar los que sobran
        for (int i = usados; i < indicadores.Count; i++)
            indicadores[i].Ocultar();
    }

    /// <summary>
    /// Devuelve false si el fantasma se ve en pantalla. Si no, da la dirección
    /// hacia la que hay que girar, en coordenadas de pantalla.
    /// </summary>
    private bool FueraDeCamara(Vector3 posicion, out Vector2 direccion)
    {
        Vector3 vista = camaraRA.WorldToViewportPoint(posicion);
        bool delante = vista.z > 0f;

        direccion = Vector2.zero;
        if (delante && vista.x > 0f && vista.x < 1f && vista.y > 0f && vista.y < 1f)
            return false;

        // Dirección según la posición del fantasma respecto a la cámara (derecha/arriba).
        // No se usa la proyección en pantalla porque con el fantasma detrás o a un lado
        // de la cámara se invierte o se dispara y el indicador señalaba el lado contrario.
        Vector3 local = camaraRA.transform.InverseTransformPoint(posicion);
        direccion = new Vector2(local.x, local.y);

        // Si está detrás, lo que importa es hacia qué lado girar: priorizar el horizontal
        if (local.z < 0f)
            direccion.x = (local.x >= 0f ? 1f : -1f) * Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y), 0.5f);

        // Justo detrás: indicar un lado para que se sepa hacia dónde girar
        if (direccion.sqrMagnitude < 0.0001f)
            direccion = Vector2.right;

        direccion.Normalize();
        return true;
    }

    /// <summary>Punto del borde de la pantalla (con margen) en esa dirección.</summary>
    private Vector2 PosicionEnBorde(Vector2 direccion)
    {
        Vector2 mitad = zona.rect.size * 0.5f;
        float limiteX = mitad.x - margen;
        float limiteY = mitad.y - (direccion.y > 0f ? margenSuperior : margen);

        float escalaX = Mathf.Abs(direccion.x) > 0.0001f ? limiteX / Mathf.Abs(direccion.x) : float.MaxValue;
        float escalaY = Mathf.Abs(direccion.y) > 0.0001f ? limiteY / Mathf.Abs(direccion.y) : float.MaxValue;

        return direccion * Mathf.Min(escalaX, escalaY);
    }

    private IndicadorFantasma Obtener(int indice)
    {
        while (indicadores.Count <= indice)
        {
            IndicadorFantasma nuevo = Instantiate(prefabIndicador, zona);
            nuevo.Ocultar();
            indicadores.Add(nuevo);
        }

        return indicadores[indice];
    }
}
