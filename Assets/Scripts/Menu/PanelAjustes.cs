using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Submenú de ajustes (botón ☰): volumen de 0 a 100 y salir del juego (o volver al menú, en las secciones).
/// Va en el objeto raíz del panel, que empieza desactivado.
/// </summary>
public class PanelAjustes : MonoBehaviour
{
    public Slider sliderVolumen;

    void Awake()
    {
        sliderVolumen.minValue = 0f;
        sliderVolumen.maxValue = 100f;
        sliderVolumen.wholeNumbers = true;
        sliderVolumen.onValueChanged.AddListener(valor => AjustesAudio.Volumen = valor / 100f);
    }

    void OnEnable()
    {
        sliderVolumen.SetValueWithoutNotify(AjustesAudio.Volumen * 100f);
    }

    public void Abrir() => gameObject.SetActive(true);

    public void Cerrar() => gameObject.SetActive(false);

    /// <summary>Para el menú dentro de las secciones (preview...): vuelve al menú principal.</summary>
    public void VolverAlMenuPrincipal() => UnityEngine.SceneManagement.SceneManager.LoadScene(MenuPrincipal.k_EscenaMenu);

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
