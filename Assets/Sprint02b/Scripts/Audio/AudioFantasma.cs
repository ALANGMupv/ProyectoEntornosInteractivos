using UnityEngine;

/// <summary>
/// Voz del fantasma jugable con sonido por proximidad: cuanto más cerca está de la
/// cámara, más fuerte se oye, y suena desde su posición (izquierda/derecha). Lanza su
/// audio de vez en cuando con pausas aleatorias, alterna alguna pedorreta y se ríe si
/// se escapa de la mira. Nunca suenan dos de sus sonidos a la vez.
/// AudioPartida lo añade al fantasma cuando aparece.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioFantasma : MonoBehaviour
{
    private AudioPartida config;
    private AudioSource fuente;
    private Camera camara;

    private float volumenClip;
    private float espera;
    private float siguientePedorretaPermitida;
    private bool callado;
    private bool pausado;

    public void Configurar(AudioPartida audioPartida)
    {
        config = audioPartida;
        camara = Camera.main;

        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 1f;
        fuente.dopplerLevel = 0f;
        fuente.spread = 60f;

        // El volumen por distancia lo calcula este script (ver Update): la atenuación de
        // Unity se deja plana para que no se sumen las dos y se pierda el sonido de lejos
        fuente.rolloffMode = AudioRolloffMode.Custom;
        fuente.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
        fuente.minDistance = 1f;
        fuente.maxDistance = 100f;

        // Que no arranque encima del sonido de inicio de la persecución
        espera = Random.Range(1.2f, 2f);
        siguientePedorretaPermitida = Time.time + config.separacionPedorretas * 0.5f;
    }

    /// <summary>Se ríe al escaparse de la mira (corta lo que estuviera sonando).</summary>
    public void Reirse()
    {
        if (callado || config.risaBurlona == null)
            return;

        Reproducir(config.risaBurlona, config.volumenRisa);
    }

    /// <summary>Deja de sonar para siempre (al capturarlo o al terminar la partida).</summary>
    public void Callar()
    {
        callado = true;
        if (fuente != null)
            fuente.Stop();
    }

    void Update()
    {
        if (config == null || callado)
            return;

        // Con la partida en pausa (menú abierto) se calla y retoma donde estaba
        bool enPausa = Time.timeScale == 0f;
        if (enPausa != pausado)
        {
            pausado = enPausa;
            if (pausado) fuente.Pause();
            else fuente.UnPause();
        }
        if (pausado)
            return;

        if (camara == null)
            camara = Camera.main;

        fuente.volume = volumenClip * FactorDistancia();

        if (fuente.isPlaying)
            return;

        espera -= Time.deltaTime;
        if (espera > 0f)
            return;

        // De vez en cuando, una pedorreta en lugar del audio normal (nunca a la vez)
        bool pedorreta = config.pedorretaBurlona != null &&
                         Time.time >= siguientePedorretaPermitida &&
                         Random.value < config.probabilidadPedorreta;

        if (pedorreta)
        {
            siguientePedorretaPermitida = Time.time + config.separacionPedorretas;
            Reproducir(config.pedorretaBurlona, config.volumenPedorreta);
        }
        else if (config.audioFantasma != null)
        {
            Reproducir(config.audioFantasma, config.volumenAudioFantasma);
        }
    }

    private void Reproducir(AudioClip clip, float volumen)
    {
        fuente.Stop();
        fuente.clip = clip;
        volumenClip = volumen;
        fuente.volume = volumenClip * FactorDistancia();
        fuente.Play();

        // Pausa tras terminar: más corta fuera de cámara, para ayudar a localizarlo
        espera = FueraDeCamara()
            ? Random.Range(config.pausaFueraDeCamara.x, config.pausaFueraDeCamara.y)
            : Random.Range(config.pausaEnCamara.x, config.pausaEnCamara.y);
    }

    /// <summary>1 de cerca y baja hasta volumenLejos al alejarse (sistema de proximidad).</summary>
    private float FactorDistancia()
    {
        if (camara == null)
            return 1f;

        float distancia = Vector3.Distance(camara.transform.position, transform.position);
        float t = Mathf.InverseLerp(config.distanciaCerca, config.distanciaLejos, distancia);
        return Mathf.Lerp(1f, config.volumenLejos, t * t * (3f - 2f * t));
    }

    private bool FueraDeCamara()
    {
        if (camara == null)
            return true;

        Vector3 vista = camara.WorldToViewportPoint(transform.position);
        return vista.z <= 0f || vista.x < 0f || vista.x > 1f || vista.y < 0f || vista.y > 1f;
    }
}
