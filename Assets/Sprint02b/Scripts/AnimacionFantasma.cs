using UnityEngine;

public class AnimacionFantasma : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Funciones para probar desde el Inspector
    [ContextMenu("1. Simular Escaneo (Aparecer)")]
    public void ActivarFantasma()
    {
        animator.SetTrigger("AparecerTrigger");
    }

    [ContextMenu("2. Simular Huida (Huir)")]
    public void EsconderFantasma()
    {
        animator.SetTrigger("HuirTrigger");
    }
}