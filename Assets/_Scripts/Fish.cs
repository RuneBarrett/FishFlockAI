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

    public Vector3 destination;
    #endregion

    void Start()
    {
        destination = transform.position;
        initSpeed = speed;

        //Set the speed to a value slightly higher or lower, so the fish will move with varying speeds.
        speed = CalculateSpeed(1f);//The parameter in CalulateSpeed is a speed modifyer. 1f is base speed, higher = faster, lower = slower. 
    }

    void Update()
    {
        if (!instatiated)
            InstatiateFish(); //Fill the array of fish from the GlobalFlock instance. We cant do this in Start() since the array is not full yet until the last fish is instatiated. Every fish needs to do this of course

        //If the fish is out of allowed range turnAround = true, otherwise turnAround = false
        TurnAroundSwitch();

        //Turn the fish around if 'turnAround'
        if (turnAround)
        {
            Vector3 dir = flock.setRandomPosInArea() - transform.position;
            Rotate(dir);
            speed = CalculateSpeed(1f);
        }
        //Else if a scary object is near, flee fast
        else if (BecomeScared())
        {
            Rotate(scaredDirection);
            CalculateSpeed(fleeSpeed);
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

    private bool BecomeScared()
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
                }
            }
        }

        if (groupSize > 1)
        {
            //Turn towards the center of the group, and change the speed of this fish, to the avarage speed of the group.
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
