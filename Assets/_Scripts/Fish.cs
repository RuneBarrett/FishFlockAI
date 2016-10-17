using UnityEngine;
using System.Collections;

public class Fish : MonoBehaviour
{
    #region Variables
    private GlobalFlock flock;              //Reference to the instantiated "parent" script, Flock.cs 
    private GameObject[] allFish;           //Contains all members of this flock
    private bool instatiated = false;       //This becomes true when all members have been retrieved from the Flock.cs instantiation.

    private float initSpeed;                //Each members original speed. Not changing. 
    //private Vector3 averageHeading;
    //private Vector3 averagePosition;
    private bool turnAround = false;        //Determines whether the fish is out of range
    private Vector3 scaredDirection;        //Stores the direction to move in when scared
    private Vector3 avoidDirection;         //Stores the direction to move in when avoiding
    private Vector3 goalPos;


    public float speed = 0.03f;             //The approximate starting speed
    public float speedModifyer = 0.2f;      //The modifier used to make the fish move with varying speeds. 
    public float rotationSpeed = 1f;        //How fast the member turns
    public float applyRulesFactor = 10f;    //Apply the 3 basic rules only one in <applyRulesFactor> times. This allows fish to sometimes swim away from the group, etc.
    public float neighborRange = 2f;        //When is a member considered a neighbor, and is thus eligible for grouping.
    public float tooCloseRange = 1f;        //When is another member too close.
    public float groupSpeedReset = 1.5f;    //The approximate speed a group will start with in the beginning of a frame. The avarage speed is then calculated including this number.

    public float scaredDist = 3f;           // How close to a scary object does the member need to be, to start fleeing.
    public float fleeSpeed = 5f;            //The speed modifier used for fleeing

    public float avoidDist = 3f;            //How close to an avoidable object does the member need to be, to swim away.

    //public Vector3 destination;
    #endregion

    void Start()
    {
        //destination = transform.position;
        initSpeed = speed;

        //Set the speed to a value slightly higher or lower, so the fish will move with varying speeds.
        speed = CalculateSpeed(1f);//The float parameter in CalulateSpeed is a speed modifyer. 1f means base speed, higher = faster, lower = slower. 

        goalPos = flock.getGoalPos();
    }

    void Update()
    {
        if (Random.Range(0, 1000) <= 1f)
            goalPos = flock.getGoalPos();
        if (!instatiated)
            InstatiateFish(); //Fill the array of fish from the GlobalFlock instance. We cant do this in Start() since the array is not full yet until the last fish is instatiated. Every fish needs to do this of course

        //1. If the fish is out of allowed range turnAround = true, otherwise turnAround = false
        TurnAroundSwitch();

        if (turnAround)
        {
            //Turn the fish around ..
            Vector3 dir = flock.setRandomPosInArea(.8f) - transform.position;
            Rotate(dir);
            speed = CalculateSpeed(1f);// .. and randomize the speed
        }

        //2. If a scary object is nearby ..
        else if (ScaryObjectNearby())
        {
            Rotate(scaredDirection);
            CalculateSpeed(fleeSpeed); // .. flee fast 
        }

        //3. If an avoidable object is nearby ..
        else if (AvoidObjectNearby()) {
            Rotate(avoidDirection);
            CalculateSpeed(1f); // .. move away 
        }

        //4. Otherwise apply flocking rules
        else if (Random.Range(0, applyRulesFactor) < 1)
        {
            ApplyBasicRules();
        }
        if (speed < .5)
            speed = CalculateSpeed(1f);

        //destination = new Vector3(0, 0, speed * Time.deltaTime);
        //transform.position = Vector3.Lerp(transform.position, destination, speed*Time.deltaTime);
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));

    }

    private bool AvoidObjectNearby()
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

    private bool ScaryObjectNearby()
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

        goalPos = flock.getGoalPos();
        Vector3 thisPos = transform.position;

        float groupSpeed = CalculateSpeed(groupSpeedReset);
        float dist;

        int groupSize = 1;

        foreach (GameObject anotherFish in allFish)
        {
            if (anotherFish != gameObject)//If not this fish
            {
                dist = Vector3.Distance(thisPos, anotherFish.transform.position);
                if (dist <= neighborRange) //If the current other fish is in 'neighborRange'..
                {
                    // .. add its' position to adjust the groupCenter, and increment the groupSize.
                    groupCenter += anotherFish.transform.position;
                    groupSize++;

                    if (dist < tooCloseRange) // If the other fish gets too close however, turn away from it.
                    {
                        avoidFishDir = (avoidFishDir + (thisPos - anotherFish.transform.position));
                    }

                    //Prepare to adjust the groupSpeed by adding in the other fish's speed. 
                    groupSpeed += anotherFish.GetComponent<Fish>().GetSpeed();
                    goalPos = anotherFish.GetComponent<Fish>().GetGoalPos();
                }
            }
        }

        if (groupSize > 1)
        {
            //Turn towards the center of the group and in the direction of the goal .. 
            groupCenter = groupCenter / groupSize + (goalPos - thisPos);
            speed = groupSpeed / groupSize; //.. then change the speed of this fish to the avarage speed of the group.

            Vector3 dir = groupCenter + avoidFishDir;

            if (dir != Vector3.zero)
                Rotate(dir);
        }
    }

    private Vector3 GetGoalPos()
    {
        return goalPos;
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

        return flock.SlightlyRandomizeValue(speed, speedModifyer);
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
    #endregion
}
