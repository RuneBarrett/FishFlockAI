using UnityEngine;
using System.Collections;

public class CausticsMov : MonoBehaviour {

    private Projector p;
    public MovieTexture movTex;

	void Start () {
        p = GetComponent<Projector>();
        p.material.SetTexture("_ShadowTex", movTex);
        movTex.loop = true;
        movTex.Play();
	}
}
