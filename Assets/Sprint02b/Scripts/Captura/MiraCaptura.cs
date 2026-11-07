using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mira del centro de la pantalla para la captura: el anillo rojo se llena con el progreso,
/// la mira se agranda y cambia de color al estar sobre un fantasma, y hace un pulso al capturarlo.
/// La controla CapturaFantasma.
/// </summary>
public class MiraCaptura : MonoBehaviour
{
    [SerializeField] private Image anilloProgreso;
    [SerializeField] private Image mira;

    [Header("Colores")]
    [SerializeField] private Color colorMira = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color colorMiraApuntando = new Color(1f, 0.85f, 0.35f, 1f);

    [Header("Animación")]
    [Tooltip("Tamaño de la mira al estar sobre un fantasma.")]
    [SerializeField] private float escalaApuntando = 1.15f;
    [SerializeField] private float suavizado = 12f;
    [SerializeField] private float duracionPulso = 0.35f;

    private RectTransform rect;
    private float escalaActual = 1f;
    private float tiempoPulso = -1f;

    void Awake()
    {
        rect = (RectTransform)transform;
        Actualizar(0f, false);
    }

    public void Actualizar(float progreso, bool apuntando)
    {
        if (anilloProgreso != null)
            anilloProgreso.fillAmount = progreso;
        if (mira != null)
            mira.color = Color.Lerp(mira.color, apuntando ? colorMiraApuntando : colorMira, Time.deltaTime * suavizado);

        // Se agranda al apuntar y late suavemente mientras carga
        float objetivo = apuntando ? escalaApuntando + Mathf.Sin(Time.time * 12f) * 0.03f * progreso : 1f;
        escalaActual = Mathf.Lerp(escalaActual, objetivo, Time.deltaTime * suavizado);

        float pulso = 0f;
        if (tiempoPulso >= 0f)
        {
            float t = (Time.time - tiempoPulso) / duracionPulso;
            if (t >= 1f)
                tiempoPulso = -1f;
            else
                pulso = Mathf.Sin(t * Mathf.PI) * 0.35f;
        }

        if (rect != null)
            rect.localScale = Vector3.one * (escalaActual + pulso);
    }

    /// <summary>Pulso de la mira al completar una captura.</summary>
    public void Completar()
    {
        tiempoPulso = Time.time;
    }
}
