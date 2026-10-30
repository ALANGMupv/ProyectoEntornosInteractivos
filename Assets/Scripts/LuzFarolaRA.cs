using UnityEngine;
/// <summary>Ajusta alcance e intensidad de la farola al tamaño físico del mapa RA.</summary>
[RequireComponent(typeof(Light))]
public sealed class LuzFarolaRA : MonoBehaviour {
 public Transform mapa;
 public float alcanceEnMapa=.065f;
 public float intensidadBase=.004f;
 Light luz;
 void LateUpdate(){if(!luz)luz=GetComponent<Light>();float escala=mapa?Mathf.Abs(mapa.lossyScale.x):1f;luz.range=alcanceEnMapa*escala;luz.intensity=intensidadBase*escala*escala;}
}
