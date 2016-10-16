using UnityEngine;
using System.Collections;

public class Fish : MonoBehaviour
{
    #region variables
    private GlobalFlock flock;
    private bool instatiated = false;
    GameObject[] allFish;

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
    private Vector3 avoidDirection;
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
            InstatiateFish(); //Fill the array of fish from the GlobalFlock instance. We cant do this in start since the array is not full yet until the last fish is instatiated. And every fish needs to do this

        //If the fish is out of allowed range turnAround = true, otherwise turnAround = false
        TurnAroundSwitch();

        //Turn the fish around if 'turnAround'
        if (turnAround)
        {
            Vector3 dir = flock.setPosInArea() - transform.position;
            Rotate(dir);
            speed = CalculateSpeed(1f);
        }
        //Else if a scary object is near, flee fast
        else if (BecomeScared())
        {
            Rotate(scaredDirection);
            CalculateSpeed(6);
        }

        else if (AvoidObject()) {
            Rotate(avoidDirection);
            CalculateSpeed(1f);
        }

        //Otherwise apply flocking rules
        else if (Random.Range(0, applyRulesFactor) < 1)
        {
            ApplyBasicRules();
        }
        if (speed < .5)
            speed = CalculateSpeed(1f);

        //destination = (transform.rotation.eulerAngles);
        //transform.position = Vector3.Lerp(transform.position, destination, speed*Time.deltaTime);
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));

    }

    private bool AvoidObject()
    {
        avoidDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scaredDist);
        foreach (Collider c in hitColliders)
        {
            if (c.gameObject.GetComponent<AvoidObject>() != null)
            {
                avoidDirection = -(c.gameObject.transform.position - transform.position);
                return true;
            }

        }
        return false;
    }

    private bool BecomeScared()//Vector3 center, float radius
    {
        scaredDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scaredDist);
        foreach (Collider c in hitColliders)
        {
            if (c.gameObject.GetComponent<ScaryObject>() != null)
            {
                scaredDirection = -(c.gameObject.transform.position - transform.position);
                return true;
            }

        }
        return false;
    }

    private void ApplyBasicRules()
    {
        Vector3 groupCenter = Vector3.zero;
        Vector3 avoidFishDir = Vector3.zero;

        Vector3 goalPos = flock.getGoalPos();
        Vector3 thisPos = transform.position;

        float groupSpeed = CalculateSpeed(2f);
        float dist;

        int groupSize = 1;

        foreach (GameObject fish in allFish)
        {
            if (fish != gameObject)
            {
                dist = Vector3.Distance(thisPos, fish.transform.position);
                if (dist <= neighborDistance)
                {

                    groupCenter += fish.transform.position;
                    groupSize++;

                    if (dist < 1f)
                    {
                        avoidFishDir = (avoidFishDir + (thisPos - fish.transform.position));
                        //print(dist + " too close. adding "+closeFishPos);
                    }

                    Fish anotherFish = fish.GetComponent<Fish>();
                    groupSpeed = groupSpeed + anotherFish.GetSpeed();
                }
            }
        }

        if (groupSize > 1)
        {
            //Move towards the center of the group, move with the speed of the group

            //print("group with size == "+groupSize);
            groupCenter = groupCenter / groupSize + (goalPos - thisPos);
            speed = groupSpeed / groupSize;

            Vector3 dir = groupCenter + avoidFishDir;

            if (dir != Vector3.zero)
                Rotate(dir);
        }
    }

    #region Utilities
    private void Rotate(Vector3 dir)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }

    private float CalculateSpeed(float burst)
    {
        speed = initSpeed;
        speed *= burst;

        return speed + speed * Random.Range(-speedModifyer, speedModifyer);
    }

    private void TurnAroundSwitch()
    {
        if (Vector3.Distance(transform.position, Vector3.zero) > flock.spawnArea)
            turnAround = true;
        else
            turnAround = false;
    }
    private void InstatiateFish()
    {
        allFish = flock.getAllFish();
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
