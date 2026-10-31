using UnityEngine;

public class BotonesLibroAR : MonoBehaviour
{
    public Book scriptLibro;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Aviso por si la cámara sigue sin etiqueta
            if (Camera.main == null) 
            {
                Debug.LogError("Error: No encuentro la MainCamera. ¡Revisa el Tag de la ARCamera!");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Esto escribirá en la consola el nombre exacto de lo que has tocado
                Debug.Log("El rayo ha tocado el objeto: " + hit.collider.gameObject.name);

                if (hit.collider.gameObject.name == "btn-derecha")
                {
                    scriptLibro.DragRightPageToPoint(scriptLibro.EndBottomRight);
                    scriptLibro.ReleasePage();
                }
                else if (hit.collider.gameObject.name == "btn-izquierda")
                {
                    scriptLibro.DragLeftPageToPoint(scriptLibro.EndBottomLeft);
                    scriptLibro.ReleasePage();
                }
            }
            else 
            {
                Debug.Log("He hecho clic, pero no he tocado ningún Collider 3D.");
            }
        }
    }
}