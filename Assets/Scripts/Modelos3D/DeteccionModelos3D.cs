
using UnityEngine;
using Vuforia;

public class DeteccionModelos3D : MonoBehaviour
{
    public GameObject panelSeleccionModelos;
    public GameObject panelAvisoTarget;

    private ObserverBehaviour observador;

    void Start()
    {
        observador = GetComponent<ObserverBehaviour>();

        observador.OnTargetStatusChanged += CambiarEstadoTarget;
        ActualizarEstado(observador.TargetStatus.Status);
    }

    void CambiarEstadoTarget(ObserverBehaviour objeto, TargetStatus estado)
    {
        ActualizarEstado(estado.Status);
    }

    void ActualizarEstado(Status estado)
    {
        bool detectado = estado == Status.TRACKED;

        // Mostrar botones cuando se detecta el target
        panelSeleccionModelos.SetActive(detectado);

        // Mostrar aviso cuando NO se detecta el target
        panelAvisoTarget.SetActive(!detectado);
    }

    void OnDestroy()
    {
        if (observador != null)
            observador.OnTargetStatusChanged -= CambiarEstadoTarget;
    }
}
