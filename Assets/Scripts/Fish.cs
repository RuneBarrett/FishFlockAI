using UnityEngine;
using System.Collections;

public class Fish : MonoBehaviour
{
    #region variables
    private GlobalFlock flock;
    private bool instatiated = false;
    GameObject[] fishInGroup;

    public float speed = 0.03f;
    private float initSpeed;
    public float speedModifyer = 0.2f;
    public float rotationSpeed = 1f;

    public float applyRulesFactor = 10f; //will happen one in <applyRulesFactor> times.

    private Vector3 averageHeading;
    private Vector3 averagePosition;

    bool turnAround = false;

    public float neighborDistance = 2f;

    private Vector3 scaredDirection;
    private bool scared;
    public float scaredDist = 3f;

    public Vector3 destination;
    #endregion

    void Start()
    {
        destination = transform.position;
        initSpeed = speed;
        //Set the speed to a value slightly higher or lower so the fish will move with varying speeds.
        speed = CalculateSpeed(1f);
    }

    void Update()
    {
        if (!instatiated)
            InstatiateFish();//Fill the array of fish from the GlobalFlock instance. We cant do this in start since the array is not full yet until the last fish is instatiated.

        //If the fish is out of range, turnAround = true
        ConstraintArea();

        //Turn the fish around if 'turnAround'
        if (turnAround)
        {
            Vector3 dir = Vector3.zero - transform.position;
            Rotate(dir);
            speed = CalculateSpeed(1f);
        }
        //Else if a scary object is near, flee fast
        else if (BecomeScared())
        {
            Rotate(scaredDirection);
            CalculateSpeed(7);
        }
        
        //Otherwise apply flocking rules
        else if(Random.Range(0, applyRulesFactor) < 1)
        {    
                ApplyRules();
        }
        if (speed < .5)
            speed = CalculateSpeed(1f);
        //destination = (transform.rotation.eulerAngles);
        //transform.position = Vector3.Lerp(transform.position, destination, speed*Time.deltaTime);
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));

    }

    private bool BecomeScared()//Vector3 center, float radius
    {
        scaredDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scaredDist);
        //int i = 0;
        foreach (Collider c in hitColliders)
        {
            if (c.gameObject.name == "ColliderCube")
            {
                //print("scared");
                scaredDirection = -(c.gameObject.transform.position - transform.position);
                return true;
            }

        }
        return false;
    }
    private void Rotate(Vector3 dir)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }
    private void ApplyRules()
    {
        Vector3 groupCenter = Vector3.zero;
        Vector3 closeFishPos = Vector3.zero;

        Vector3 goalPos = flock.getGoalPos();
        Vector3 thisPos = transform.position;

        float groupSpeed = CalculateSpeed(2f);
        float dist;

        int groupSize = 1;

        foreach (GameObject go in fishInGroup)
        {
            if (go != gameObject)
            {
                dist = Vector3.Distance(thisPos, go.transform.position);
                if (dist <= neighborDistance)
                {

                    groupCenter += go.transform.position;
                    groupSize++;

                    if (dist < 1f)
                    {
                        closeFishPos = (closeFishPos + (thisPos - go.transform.position));
                        //print(dist + " too close. adding"+closeFishPos);
                    }

                    Fish anotherFish = go.GetComponent<Fish>();
                    groupSpeed = groupSpeed + anotherFish.GetSpeed();
                }
            }
        }

        if (groupSize > 1)
        {
            //print("group with size == "+groupSize);
            groupCenter = groupCenter / groupSize + (goalPos - thisPos);
            speed = groupSpeed / groupSize;

            Vector3 dir = groupCenter + closeFishPos;

            if (dir != Vector3.zero)
                Rotate(dir);
            //transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),rotationSpeed*Time.deltaTime);
        }
    }
    private float CalculateSpeed(float burst)
    {
        speed = initSpeed;
        speed *= burst;

        return speed + speed * Random.Range(-speedModifyer, speedModifyer);
    }

    #region Utilities
    private void ConstraintArea()
    {
        if (Vector3.Distance(transform.position, Vector3.zero) > flock.spawnArea)
            turnAround = true;
        else
            turnAround = false;
    }
    private void InstatiateFish()
    {
        fishInGroup = flock.getAllFish();
        instatiated = true;
    }
    #endregion
    #region Accessors
    private float GetSpeed()
    {
        return speed;
    }

    public void SetFlockReference(GlobalFlock f)
    {
        flock = f;
    }

    public void setScaredDir(Vector3 scaryObjectPos)
    {
        scaredDirection = -(scaryObjectPos - transform.position);
        scared = true;
    }
    #endregion
}
