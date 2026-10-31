
using UnityEngine;
using UnityEngine.UI;

public class SelectorModelos : MonoBehaviour
{
    public GameObject[] modelos;
    public Button[] botones;

    public GameObject ModeloActual { get; private set; }

    [Header("Colores")]
    public Color colorSeleccionado = new Color32(80, 0, 22, 255);
    public Color colorNormal = new Color32(208, 208, 208, 255);

    private Vector3[] posicionesIniciales;
    private Quaternion[] rotacionesIniciales;
    private Vector3[] escalasIniciales;

    void Start()
    {
        // Guardar el estado original de cada modelo
        posicionesIniciales = new Vector3[modelos.Length];
        rotacionesIniciales = new Quaternion[modelos.Length];
        escalasIniciales = new Vector3[modelos.Length];

        for (int i = 0; i < modelos.Length; i++)
        {
            Transform modelo = modelos[i].transform;

            posicionesIniciales[i] = modelo.localPosition;
            rotacionesIniciales[i] = modelo.localRotation;
            escalasIniciales[i] = modelo.localScale;
        }

        SeleccionarModelo(0);
    }

    public void SeleccionarModelo(int indice)
    {
        if (indice < 0 || indice >= modelos.Length)
            return;

        // Restaurar posicion, rotacion y escala originales
        Transform modeloSeleccionado = modelos[indice].transform;

        modeloSeleccionado.localPosition = posicionesIniciales[indice];
        modeloSeleccionado.localRotation = rotacionesIniciales[indice];
        modeloSeleccionado.localScale = escalasIniciales[indice];

        // Mostrar solo el modelo seleccionado
        for (int i = 0; i < modelos.Length; i++)
        {
            modelos[i].SetActive(i == indice);
        }

        ModeloActual = modelos[indice];

        // Actualizar el fondo de los botones
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] != null)
            {
                botones[i].image.color =
                    i == indice ? colorSeleccionado : colorNormal;
            }
        }
    }
}
