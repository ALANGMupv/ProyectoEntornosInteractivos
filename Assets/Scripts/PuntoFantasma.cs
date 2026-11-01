using UnityEngine;

/// <summary>
/// Punto del recorrido marcado con una luz. Cuando la furgoneta llega, el fantasma sale
/// del suelo, se mueve un poco y desaparece. Se rearma al reiniciar la furgoneta.
/// </summary>
public sealed class PuntoFantasma : MonoBehaviour
{
    [Header("Referencias")]
    public ControlFurgoneta furgoneta;
    public Transform fantasma;
    public Light luz;
    public Transform brillo;

    [Header("Activación (en unidades del mapa)")]
    public float radioActivacion = .015f;

    [Header("Luz")]
    public float alcanceEnMapa = .04f;
    public float intensidadBase = .004f;
    public float pulsosPorSegundo = .8f;

    [Header("Animación")]
    public float duracionSalida = .6f;
    public float duracionMovimiento = 1.8f;
    public float duracionSalidaFinal = .6f;
    public float alturaSalida = .012f;
    public float alturaFlotando = .006f;
    public float balanceoLateral = .006f;
    [Tooltip("Corrige hacia dónde mira el modelo para que la cara apunte a la furgoneta.")]
    public float giroCara = 90f;

    enum Estado { Esperando, Animando, Terminado }

    Estado m_Estado;
    float m_Tiempo;
    float m_EscalaBrillo;
    Vector3 m_PosicionFantasma;
    Vector3 m_EscalaFantasma;

    void Awake()
    {
        m_PosicionFantasma = fantasma.localPosition;
        m_EscalaFantasma = fantasma.localScale;
        m_EscalaBrillo = brillo.localScale.x;
    }

    void OnEnable()
    {
        furgoneta.Reiniciado += Rearmar;
        Rearmar();
    }

    void OnDisable()
    {
        furgoneta.Reiniciado -= Rearmar;
    }

    void Rearmar()
    {
        m_Estado = Estado.Esperando;
        fantasma.gameObject.SetActive(false);
        luz.enabled = true;
        brillo.gameObject.SetActive(true);
    }

    void Update()
    {
        if (m_Estado == Estado.Esperando)
        {
            ActualizarLuz(1f);
            if (FurgonetaEnElPunto())
                Empezar();
        }
        else if (m_Estado == Estado.Animando)
        {
            Animar();
        }
    }

    bool FurgonetaEnElPunto()
    {
        Transform mapa = transform.parent;
        Vector3 diferencia = mapa.InverseTransformPoint(furgoneta.transform.position) - transform.localPosition;
        diferencia.y = 0f;
        return diferencia.sqrMagnitude <= radioActivacion * radioActivacion;
    }

    void Empezar()
    {
        m_Estado = Estado.Animando;
        m_Tiempo = 0f;
        fantasma.gameObject.SetActive(true);
        AudioSprint.Instancia?.ReproducirFantasma();
    }

    void ActualizarLuz(float fuerza)
    {
        float escala = Mathf.Abs(transform.lossyScale.x);
        float pulso = .7f + .3f * Mathf.Sin(Time.time * pulsosPorSegundo * Mathf.PI * 2f);

        luz.range = alcanceEnMapa * escala;
        luz.intensity = intensidadBase * escala * escala * pulso * fuerza;

        brillo.localScale = Vector3.one * (m_EscalaBrillo * pulso * fuerza);
        Camera camara = Camera.main;
        if (camara)
            brillo.rotation = camara.transform.rotation;
    }

    void Animar()
    {
        m_Tiempo += Time.deltaTime;
        float finMovimiento = duracionSalida + duracionMovimiento;
        float duracionTotal = finMovimiento + duracionSalidaFinal;

        if (m_Tiempo >= duracionTotal)
        {
            m_Estado = Estado.Terminado;
            fantasma.gameObject.SetActive(false);
            luz.enabled = false;
            brillo.gameObject.SetActive(false);
            return;
        }

        float entrada = Mathf.SmoothStep(0f, 1f, m_Tiempo / duracionSalida);
        float salida = Mathf.SmoothStep(0f, 1f, (m_Tiempo - finMovimiento) / duracionSalidaFinal);
        float balanceo = Mathf.Sin(m_Tiempo / duracionTotal * Mathf.PI * 4f);
        float flotacion = Mathf.Sin(m_Tiempo * Mathf.PI * 2f) * alturaFlotando * .25f;

        // La luz se apaga mientras el fantasma ocupa su lugar
        ActualizarLuz(1f - entrada);

        Vector3 lateral = Vector3.right * (balanceo * balanceoLateral * entrada);
        float altura = Mathf.Lerp(-alturaSalida, alturaFlotando, entrada) + flotacion + salida * alturaSalida;
        fantasma.localPosition = m_PosicionFantasma + lateral + Vector3.up * altura;
        fantasma.localScale = m_EscalaFantasma * (entrada * (1f - salida));

        Vector3 haciaFurgoneta = Vector3.ProjectOnPlane(furgoneta.transform.position - fantasma.position, transform.up);
        if (haciaFurgoneta.sqrMagnitude > .0000001f)
            fantasma.rotation = Quaternion.LookRotation(haciaFurgoneta, transform.up) * Quaternion.Euler(0f, giroCara, 0f);
    }
}
