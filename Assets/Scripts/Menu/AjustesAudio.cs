using UnityEngine;

/// <summary>
/// Volumen general del juego, guardado entre sesiones y compartido por todas las escenas
/// (menú principal, preview...). Se aplica solo al arrancar el juego.
/// </summary>
public static class AjustesAudio
{
    const string k_ClaveVolumen = "volumen";

    /// <summary>Volumen de 0 a 1.</summary>
    public static float Volumen
    {
        get => PlayerPrefs.GetFloat(k_ClaveVolumen, 1f);
        set
        {
            float volumen = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(k_ClaveVolumen, volumen);
            AudioListener.volume = volumen;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AplicarAlArrancar()
    {
        AudioListener.volume = Volumen;
    }
}
