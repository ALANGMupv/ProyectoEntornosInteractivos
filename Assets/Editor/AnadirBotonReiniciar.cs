using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script temporal: añade el botón "Reiniciar" al Canvas de la escena de control y lo conecta
/// con ControlFurgoneta.Reiniciar(). Menú: PickABoo → Añadir botón reiniciar. Borrar después de usarlo.
/// </summary>
public static class AnadirBotonReiniciar
{
    const string k_RutaEscena = "Assets/Escenas/Escena control de boorgoneta.unity";

    [MenuItem("PickABoo/Añadir botón reiniciar")]
    static void Anadir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var escena = EditorSceneManager.OpenScene(k_RutaEscena, OpenSceneMode.Single);

        var canvas = GameObject.Find("/Canvas");
        var control = Object.FindFirstObjectByType<ControlFurgoneta>();
        if (canvas == null || control == null)
        {
            Debug.LogError("[AnadirBotonReiniciar] Falta el Canvas o la Furgoneta con ControlFurgoneta en la escena.");
            return;
        }
        if (canvas.transform.Find("BotonReiniciar") != null)
        {
            Debug.Log("[AnadirBotonReiniciar] El botón ya existe.");
            return;
        }

        int capaUI = LayerMask.NameToLayer("UI");

        // Botón arriba a la derecha
        var boton = new GameObject("BotonReiniciar", typeof(RectTransform), typeof(Image), typeof(Button));
        boton.layer = capaUI;
        boton.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)boton.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(260f, 90f);

        var imagen = boton.GetComponent<Image>();
        imagen.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        imagen.type = Image.Type.Sliced;
        imagen.color = new Color(1f, 1f, 1f, 0.85f);

        var texto = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        texto.layer = capaUI;
        texto.transform.SetParent(boton.transform, false);
        var rectTexto = (RectTransform)texto.transform;
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = rectTexto.offsetMax = Vector2.zero;
        var componenteTexto = texto.GetComponent<Text>();
        componenteTexto.text = "Reiniciar";
        componenteTexto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        componenteTexto.fontSize = 40;
        componenteTexto.alignment = TextAnchor.MiddleCenter;
        componenteTexto.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        UnityEventTools.AddPersistentListener(boton.GetComponent<Button>().onClick, control.Reiniciar);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[AnadirBotonReiniciar] Botón añadido y conectado a ControlFurgoneta.Reiniciar().");
    }
}
