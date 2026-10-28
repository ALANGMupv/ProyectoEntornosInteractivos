using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Botón de volver (flecha) de las secciones: carga el menú principal.</summary>
public class VolverAlMenu : MonoBehaviour
{
    public void Volver() => SceneManager.LoadScene(MenuPrincipal.k_EscenaMenu);
}
