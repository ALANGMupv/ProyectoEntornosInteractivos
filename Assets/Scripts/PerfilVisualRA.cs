using UnityEngine;
using UnityEngine.Rendering;
/// <summary>Perfil de sombras de la escena RA. Restaura el perfil anterior al salir.</summary>
public sealed class PerfilVisualRA : MonoBehaviour
{
    public RenderPipelineAsset perfil;
    RenderPipelineAsset anteriorGeneral;
    RenderPipelineAsset anteriorCalidad;
    void OnEnable()
    {
        if (!perfil) return;
        anteriorGeneral = GraphicsSettings.defaultRenderPipeline;
        anteriorCalidad = QualitySettings.renderPipeline;
        GraphicsSettings.defaultRenderPipeline = perfil;
        QualitySettings.renderPipeline = perfil;
    }
    void OnDisable()
    {
        if (!perfil) return;
        if (GraphicsSettings.defaultRenderPipeline == perfil) GraphicsSettings.defaultRenderPipeline = anteriorGeneral;
        if (QualitySettings.renderPipeline == perfil) QualitySettings.renderPipeline = anteriorCalidad;
    }
}
