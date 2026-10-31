using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Animación del menú al elegir una sección: los botones se desvanecen, la cámara avanza hacia
/// la casa mientras se abre la puerta (fotogramas renderizados en Maya) y se funde a negro antes
/// del aviso de escanear el target. Al cerrar el aviso se hace al revés. Se puede saltar tocando.
/// </summary>
public class TransicionPuerta : MonoBehaviour
{
    [Header("Escena")]
    [Tooltip("Raíz del fondo animado: es lo que se acerca hacia la puerta.")]
    public RectTransform escenario;
    [Tooltip("Imagen de la casa, para saber dónde está la puerta al recortarse la pantalla.")]
    public RectTransform casa;
    [Tooltip("Punto de la puerta en el render de 1920x1080 (origen arriba a la izquierda).")]
    public Vector2 puntoPuerta = new Vector2(965f, 700f);
    public Image puerta;
    [Tooltip("Fotogramas de la puerta abriéndose, de cerrada a abierta.")]
    public Sprite[] fotogramasPuerta;

    [Header("Interfaz")]
    [Tooltip("Logo, botones... se desvanecen durante la animación.")]
    public CanvasGroup[] interfaz;
    [Tooltip("Imagen negra a pantalla completa para el fundido.")]
    public Image negro;
    public AvisoTarget aviso;

    [Header("Tiempos")]
    public float duracionEntrada = 1.6f;
    public float duracionSalida = 1.1f;
    public float zoomFinal = 2.6f;

    [Header("Sonido (opcional)")]
    public AudioSource sonidoPuerta;

    Coroutine m_Animacion;
    bool m_Saltar;

    public bool Animando => m_Animacion != null;

    void Awake()
    {
        if (negro != null)
            negro.gameObject.SetActive(false);
        if (aviso != null)
            aviso.alCerrar += Salir;
    }

    void OnDestroy()
    {
        if (aviso != null)
            aviso.alCerrar -= Salir;
    }

    void Update()
    {
        // Tocar la pantalla durante la animación la termina de golpe
        if (Animando && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            m_Saltar = true;
    }

    /// <summary>Entra en la casa y muestra el aviso del target de esa sección.</summary>
    public void Entrar(AccesoTarget acceso)
    {
        if (Animando)
            return;
        m_Animacion = StartCoroutine(AnimarEntrada(acceso));
    }

    /// <summary>Sale de la casa y vuelve a mostrar el menú (se llama al cerrar el aviso).</summary>
    public void Salir()
    {
        if (Animando)
            return;
        m_Animacion = StartCoroutine(AnimarSalida());
    }

    IEnumerator AnimarEntrada(AccesoTarget acceso)
    {
        m_Saltar = false;
        BloquearInterfaz(true);
        if (sonidoPuerta != null)
            sonidoPuerta.Play();
        negro.gameObject.SetActive(true);

        for (float t = 0f; t < 1f && !m_Saltar; t += Time.deltaTime / duracionEntrada)
        {
            Aplicar(t);
            yield return null;
        }
        Aplicar(1f);

        aviso.Mostrar(acceso);
        m_Animacion = null;
    }

    IEnumerator AnimarSalida()
    {
        m_Saltar = false;
        negro.gameObject.SetActive(true);
        for (float t = 1f; t > 0f && !m_Saltar; t -= Time.deltaTime / duracionSalida)
        {
            Aplicar(t);
            yield return null;
        }
        Aplicar(0f);

        negro.gameObject.SetActive(false);
        BloquearInterfaz(false);
        m_Animacion = null;
    }

    /// <summary>Estado de la animación: 0 = menú, 1 = dentro de la casa (negro).</summary>
    void Aplicar(float progreso)
    {
        // Interfaz: desaparece en el primer 20 %
        float alfaInterfaz = 1f - Mathf.Clamp01(progreso / 0.2f);
        foreach (var grupo in interfaz)
            if (grupo != null)
                grupo.alpha = alfaInterfaz;

        // Puerta: se abre entre el 10 % y el 65 %
        if (puerta != null && fotogramasPuerta.Length > 0)
        {
            float apertura = Mathf.Clamp01((progreso - 0.1f) / 0.55f);
            int indice = Mathf.Min(fotogramasPuerta.Length - 1, Mathf.FloorToInt(apertura * fotogramasPuerta.Length));
            puerta.sprite = fotogramasPuerta[indice];
            puerta.enabled = progreso > 0f;
        }

        // Cámara: avanza hacia la puerta, cada vez más rápido (como al cruzarla).
        // Desplazar -puerta * (zoom - 1) mantiene la puerta quieta en pantalla mientras todo crece a su alrededor.
        float avance = Mathf.Lerp(progreso * progreso * (3f - 2f * progreso), progreso * progreso, 0.5f);
        float zoom = Mathf.Lerp(1f, zoomFinal, avance);
        escenario.localScale = new Vector3(zoom, zoom, 1f);
        escenario.anchoredPosition = -PosicionPuerta() * (zoom - 1f);

        // Negro: entra en el último 30 %
        float oscuridad = Mathf.Clamp01((progreso - 0.7f) / 0.3f);
        negro.color = new Color(0f, 0f, 0f, oscuridad);
    }

    /// <summary>Posición de la puerta respecto al centro del escenario, teniendo en cuenta el recorte de la imagen.</summary>
    Vector2 PosicionPuerta()
    {
        var tamano = casa.rect.size;
        var normalizado = new Vector2(puntoPuerta.x / 1920f - 0.5f, 0.5f - puntoPuerta.y / 1080f);
        return Vector2.Scale(normalizado, tamano);
    }

    void BloquearInterfaz(bool bloquear)
    {
        foreach (var grupo in interfaz)
        {
            if (grupo == null)
                continue;
            grupo.interactable = !bloquear;
            grupo.blocksRaycasts = !bloquear;
        }
    }
}
