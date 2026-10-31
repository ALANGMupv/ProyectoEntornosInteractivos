
using UnityEngine;
using Vuforia;

public class DeteccionModelos3D : MonoBehaviour
{
    public GameObject panelSeleccionModelos;

    private ObserverBehaviour observador;

    void Start()
    {
        observador = GetComponent<ObserverBehaviour>();

        panelSeleccionModelos.SetActive(false);

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

        panelSeleccionModelos.SetActive(detectado);
    }

    void OnDestroy()
    {
        if (observador != null)
            observador.OnTargetStatusChanged -= CambiarEstadoTarget;
    }
}
