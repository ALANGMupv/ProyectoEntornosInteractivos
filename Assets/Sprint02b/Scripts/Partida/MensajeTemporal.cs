using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mensaje en pantalla que aparece con un fundido y se oculta solo pasados unos
/// segundos o al tocar la pantalla. Va en el objeto raíz del mensaje, que empieza desactivado.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MensajeTemporal : MonoBehaviour
{
    [SerializeField] private float duracion = 4f;
    [SerializeField] private float fundido = 0.3f;
    [Tooltip("Tiempo mínimo antes de poder cerrarlo tocando, para no quitarlo sin querer.")]
    [SerializeField] private float esperaToque = 0.4f;

    private CanvasGroup grupo;
    private float tiempo;
    private bool cerrando;

    /// <summary>True desde que se muestra hasta que termina de ocultarse.</summary>
    public bool Visible { get; private set; }

    public void Mostrar()
    {
        Visible = true;
        cerrando = false;
        tiempo = 0f;
        gameObject.SetActive(true);
        if (grupo == null)
            grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
    }

    public void Ocultar()
    {
        Visible = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Sin escala de tiempo: el mensaje no se congela si la partida está en pausa
        tiempo += Time.unscaledDeltaTime;

        bool toque = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        if (!cerrando && (tiempo >= duracion || (toque && tiempo >= esperaToque)))
            cerrando = true;

        float paso = Time.unscaledDeltaTime / fundido;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, cerrando ? 0f : 1f, paso);

        if (cerrando && grupo.alpha <= 0f)
            Ocultar();
    }
}
