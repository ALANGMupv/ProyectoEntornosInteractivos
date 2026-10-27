using UnityEngine;
using Vuforia;

/// <summary>
/// Crea en tiempo de ejecución un Image Target a partir de una imagen y coloca el mapa encima.
/// El mapa solo se muestra mientras la cámara ve la imagen (sirve con la webcam del PC
/// enseñándole la imagen impresa o en la pantalla del móvil).
/// </summary>
public class MapaEnImageTarget : MonoBehaviour
{
    [Tooltip("Imagen que hace de target. No hace falta marcar Read/Write en la importación.")]
    public Texture2D imagenTarget;

    [Tooltip("Ancho real de la imagen impresa o mostrada en pantalla, en metros.")]
    public float anchoImagenMetros = 0.15f;

    [Tooltip("El mapa (Suelo) que se coloca encima de la imagen.")]
    public Transform mapa;

    [Tooltip("Ancho del mapa en sus propias unidades (el plano del Suelo mide 5).")]
    public float anchoMapa = 5f;

    void Start()
    {
        if (mapa != null)
            mapa.gameObject.SetActive(false);
        if (VuforiaApplication.Instance.IsRunning)
            CrearTarget();
        else
            VuforiaApplication.Instance.OnVuforiaStarted += CrearTarget;
    }

    void OnDestroy()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= CrearTarget;
    }

    void CrearTarget()
    {
        if (imagenTarget == null || mapa == null)
        {
            Debug.LogError("[MapaEnImageTarget] Falta asignar la imagen o el mapa.");
            return;
        }

        var target = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(
            CopiaLegible(imagenTarget), anchoImagenMetros, imagenTarget.name);
        target.OnTargetStatusChanged += AlCambiarEstado;
        Debug.Log($"[MapaEnImageTarget] Image Target '{imagenTarget.name}' creado ({anchoImagenMetros} m de ancho). Enséñale la imagen a la cámara.");

        // El mapa ocupa el ancho de la imagen, sea cual sea la escala del target
        mapa.SetParent(target.transform, false);
        mapa.localPosition = Vector3.zero;
        mapa.localRotation = Quaternion.identity;
        mapa.localScale = Vector3.one * (anchoImagenMetros / (anchoMapa * target.transform.lossyScale.x));
    }

    void AlCambiarEstado(ObserverBehaviour target, TargetStatus estado)
    {
        bool visible = estado.Status == Status.TRACKED || estado.Status == Status.EXTENDED_TRACKED;
        mapa.gameObject.SetActive(visible);
        Debug.Log($"[MapaEnImageTarget] {target.TargetName}: {estado.Status} ({estado.StatusInfo})");
    }

    /// <summary>Vuforia necesita leer los píxeles: se copia la textura para no depender de Read/Write.</summary>
    static Texture2D CopiaLegible(Texture2D origen)
    {
        var rt = RenderTexture.GetTemporary(origen.width, origen.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(origen, rt);
        var anterior = RenderTexture.active;
        RenderTexture.active = rt;
        var copia = new Texture2D(origen.width, origen.height, TextureFormat.RGBA32, false);
        copia.ReadPixels(new Rect(0, 0, origen.width, origen.height), 0, 0);
        copia.Apply();
        RenderTexture.active = anterior;
        RenderTexture.ReleaseTemporary(rt);
        return copia;
    }
}
