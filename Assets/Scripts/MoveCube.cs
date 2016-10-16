using UnityEngine;
using System.Collections;

public class MoveCube : MonoBehaviour {

    public float speed = 30;
    public float highLimit = 2f;
    public float lowLimit = -20f;
    public Rigidbody rb;
    
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (transform.position.x > highLimit)
            speed = -speed;
        else if (transform.position.x < lowLimit)
            speed = -speed;

        transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        /*foreach (Collider c in hitColliders)
        {
            if (hitColliders[i].gameObject.name == "ColliderCube")
            {
                
            }

        }*/
    }
}
