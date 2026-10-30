using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Una sección del menú que se abre escaneando un target (Preview, Trailer...).</summary>
[System.Serializable]
public class AccesoTarget
{
    [Tooltip("Nombre que se muestra en el aviso, p. ej. \"Preview\".")]
    public string nombre;
    [Tooltip("Escena que se carga al pulsar Aceptar (tiene que estar en Build Profiles → Scene List).")]
    public string escena;
    [Tooltip("Imagen del target que hay que escanear. Si se deja vacía no se muestra miniatura.")]
    public Texture imagenTarget;
}

/// <summary>
/// Aviso "Escanee el target correspondiente". Aceptar abre la escena con la cámara RA
/// y la X vuelve al menú principal. Va en el objeto raíz del aviso, que empieza desactivado.
/// </summary>
public class AvisoTarget : MonoBehaviour
{
    public TMP_Text textoTarget;
    public RawImage miniaturaTarget;

    /// <summary>Se lanza al cerrar el aviso con la X (la transición de la puerta lo usa para salir de la casa).</summary>
    public event System.Action alCerrar;

    AccesoTarget m_Acceso;

    public void Mostrar(AccesoTarget acceso)
    {
        m_Acceso = acceso;
        textoTarget.text = $"Target: {acceso.nombre}";
        miniaturaTarget.texture = acceso.imagenTarget;
        miniaturaTarget.gameObject.SetActive(acceso.imagenTarget != null);
        gameObject.SetActive(true);
    }

    public void Aceptar()
    {
        if (m_Acceso == null)
            return;
        if (!Application.CanStreamedLevelBeLoaded(m_Acceso.escena))
        {
            Debug.LogWarning($"[AvisoTarget] La escena '{m_Acceso.escena}' no está en Build Profiles → Scene List.");
            return;
        }
        SceneManager.LoadScene(m_Acceso.escena);
    }

    public void Cerrar()
    {
        gameObject.SetActive(false);
        alCerrar?.Invoke();
    }
}
