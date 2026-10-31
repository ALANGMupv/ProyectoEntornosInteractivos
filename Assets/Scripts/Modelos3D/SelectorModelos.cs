
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

    void Start()
    {
        SeleccionarModelo(0);
    }

    public void SeleccionarModelo(int indice)
    {
        if (indice < 0 || indice >= modelos.Length)
            return;

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
