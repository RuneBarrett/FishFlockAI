using UnityEngine;

public class MoveCube : MonoBehaviour
{

    public float speed = 30;
    public float highLimit = 2f;
    public float lowLimit = -20f;
    public string dir = "x";
    public Rigidbody rb;

    void Update()
    {


        if (dir.Equals("x"))
        {
            if (transform.position.x >= highLimit)
                speed = -speed;
            else if (transform.position.x <= lowLimit)
                speed = -speed;

            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        }
        if (dir.Equals("y"))
        {
            if (transform.position.y >= highLimit)
                speed = -speed;
            else if (transform.position.y <= lowLimit)
                speed = -speed;

            transform.Translate(new Vector3(0, -speed * Time.deltaTime, 0));
        }
        if (dir.Equals("z"))
        {
            if (transform.position.z >= highLimit)
                speed = -speed;
            else if (transform.position.z <= lowLimit)
                speed = -speed;

            transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime));
        }
    }
}
