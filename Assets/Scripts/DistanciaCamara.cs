using UnityEngine;

/// <summary>
/// Muestra por consola la distancia entre la AR Camera y el marcador.
/// Sirve para elegir un Shadow Distance razonable: se activa solo para medir.
/// </summary>
public class DistanciaCamara : MonoBehaviour
{
    public Transform arcamera;

    void Update()
    {
        float distancia = Vector3.Distance(transform.position, arcamera.position);
        print(distancia);
    }
}
