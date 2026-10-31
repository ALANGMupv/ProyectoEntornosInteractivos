using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fondo animado del menú principal, con la ambientación de la escena de la carretera del tráiler:
/// la casa de noche renderizada en Maya, cielo azul marino con luna, bandas de bruma azul que se
/// desplazan y "respiran", faroles que parpadean y ráfagas de viento con hojas.
/// Todo es UI 2D, así que es ligero en el móvil.
/// </summary>
public class FondoAnimado : MonoBehaviour
{
    [System.Serializable]
    public class CapaProfundidad
    {
        public RectTransform rect;
        [Tooltip("Zoom máximo de esta capa. Las capas cercanas se mueven más que las lejanas (parallax).")]
        public float zoom = 1.04f;
    }

    [System.Serializable]
    public class CapaBruma
    {
        [Tooltip("RawImage con una textura de bruma que se repite en horizontal.")]
        public RawImage imagen;
        [Tooltip("Vueltas de textura por segundo (negativo = hacia la izquierda).")]
        public float velocidad = -0.01f;
        [Tooltip("Cuánto sube y baja la opacidad (0 = fija).")]
        [Range(0f, 1f)] public float respiracion = 0.3f;
        public float periodoRespiracion = 9f;
        [Tooltip("Vaivén vertical en unidades de UI (0 = quieta).")]
        public float vaiven = 12f;
        [HideInInspector] public float alfaBase;
        [HideInInspector] public Vector2 posicionBase;
    }

    [Header("Movimiento de cámara (parallax)")]
    public CapaProfundidad[] capasProfundidad;
    public float periodoZoom = 26f;

    [Header("Bruma")]
    public CapaBruma[] capasBruma;

    [Header("Viento")]
    [Tooltip("Cuánto acelera el viento en las ráfagas (1 = sin ráfagas).")]
    public float fuerzaRafagas = 2.5f;
    public Sprite spriteHoja;
    public RectTransform zonaHojas;
    public int numeroHojas = 8;

    [Header("Faroles")]
    public Image[] brillosFaroles;
    [Range(0f, 1f)] public float parpadeo = 0.35f;

    class Hoja
    {
        public RectTransform rect;
        public float velocidad, giro, fase, alturaBase;
    }

    readonly List<Hoja> m_Hojas = new List<Hoja>();
    float m_Viento = 1f;
    float[] m_AlfaFaroles;

    void Start()
    {
        foreach (var capa in capasBruma)
        {
            if (capa.imagen == null)
                continue;
            capa.alfaBase = capa.imagen.color.a;
            capa.posicionBase = capa.imagen.rectTransform.anchoredPosition;
        }

        m_AlfaFaroles = new float[brillosFaroles.Length];
        for (int i = 0; i < brillosFaroles.Length; i++)
            m_AlfaFaroles[i] = brillosFaroles[i] != null ? brillosFaroles[i].color.a : 0f;

        CrearHojas();
    }

    void Update()
    {
        float t = Time.time;
        // Ráfagas: el viento sube y baja de forma irregular
        m_Viento = 1f + (fuerzaRafagas - 1f) * Mathf.Pow(Mathf.PerlinNoise(t * 0.15f, 0.3f), 3f) * 2f;

        float onda = 0.5f - 0.5f * Mathf.Cos(t * 2f * Mathf.PI / periodoZoom);
        foreach (var capa in capasProfundidad)
        {
            if (capa.rect == null)
                continue;
            float zoom = Mathf.Lerp(1f, capa.zoom, onda);
            capa.rect.localScale = new Vector3(zoom, zoom, 1f);
        }

        for (int i = 0; i < capasBruma.Length; i++)
        {
            var capa = capasBruma[i];
            if (capa.imagen == null)
                continue;
            // Mover el UV en sentido contrario desplaza la textura en el sentido de la velocidad
            var uv = capa.imagen.uvRect;
            uv.x = Mathf.Repeat(uv.x - capa.velocidad * m_Viento * Time.deltaTime, 1f);
            capa.imagen.uvRect = uv;

            var color = capa.imagen.color;
            float respira = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / capa.periodoRespiracion + i * 2.1f);
            color.a = capa.alfaBase * Mathf.Lerp(1f - capa.respiracion, 1f, respira);
            capa.imagen.color = color;

            float vaiven = Mathf.Sin(t * 2f * Mathf.PI / (capa.periodoRespiracion * 1.3f) + i * 1.7f) * capa.vaiven;
            capa.imagen.rectTransform.anchoredPosition = capa.posicionBase + new Vector2(0f, vaiven);
        }

        for (int i = 0; i < brillosFaroles.Length; i++)
        {
            if (brillosFaroles[i] == null)
                continue;
            // Parpadeo irregular, con algún bajón fuerte de vez en cuando
            float ruido = Mathf.PerlinNoise(t * 3f, i * 7.3f);
            float bajon = Mathf.PerlinNoise(t * 0.7f, i * 3.1f + 50f) > 0.78f ? 0.35f : 1f;
            var color = brillosFaroles[i].color;
            color.a = m_AlfaFaroles[i] * Mathf.Lerp(1f - parpadeo, 1f, ruido) * bajon;
            brillosFaroles[i].color = color;
        }

        MoverHojas(t);
    }

    // ---------- Hojas ----------

    void CrearHojas()
    {
        if (spriteHoja == null || zonaHojas == null)
            return;
        for (int i = 0; i < numeroHojas; i++)
        {
            var go = new GameObject("Hoja", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(zonaHojas, false);
            var imagen = go.GetComponent<Image>();
            imagen.sprite = spriteHoja;
            imagen.raycastTarget = false;
            var hoja = new Hoja { rect = (RectTransform)go.transform };
            ReiniciarHoja(hoja, true);
            m_Hojas.Add(hoja);
        }
    }

    void ReiniciarHoja(Hoja hoja, bool repartir)
    {
        var zona = zonaHojas.rect;
        float tamano = Random.Range(18f, 40f);
        hoja.rect.sizeDelta = new Vector2(tamano, tamano);
        hoja.velocidad = Random.Range(60f, 140f);
        hoja.giro = Random.Range(-180f, 180f);
        hoja.fase = Random.Range(0f, 10f);
        hoja.alturaBase = Random.Range(zona.yMin + zona.height * 0.1f, zona.yMax - zona.height * 0.2f);
        float x = repartir ? Random.Range(zona.xMin, zona.xMax) : zona.xMax + tamano;
        hoja.rect.anchoredPosition = new Vector2(x, hoja.alturaBase);
    }

    void MoverHojas(float t)
    {
        if (m_Hojas.Count == 0)
            return;
        var zona = zonaHojas.rect;
        foreach (var hoja in m_Hojas)
        {
            var posicion = hoja.rect.anchoredPosition;
            posicion.x -= hoja.velocidad * m_Viento * Time.deltaTime;
            posicion.y = hoja.alturaBase + Mathf.Sin(t * 1.7f + hoja.fase) * 40f - (zona.xMax - posicion.x) * 0.08f;
            hoja.rect.anchoredPosition = posicion;
            hoja.rect.Rotate(0f, 0f, hoja.giro * m_Viento * Time.deltaTime);
            if (posicion.x < zona.xMin - hoja.rect.sizeDelta.x)
                ReiniciarHoja(hoja, false);
        }
    }
}
