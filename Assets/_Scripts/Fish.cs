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
    private bool turnAround = false;        //Determines whether the member is out of range
    private Vector3 scaredDirection;        //Stores the direction to move in when scared
    private Vector3 avoidDirection;         //Stores the direction to move in when avoiding
    private GameObject goalPos;             //The overall position the member is moving towards

    /*
    All of the below public varibles, and the similar fields in GlobalFlock.cs, change the behavior of the members. Be aware that many of them have 
    conseqenses on the effect of other variables as well when changed. As an example, changing the amount of members in a given area without also changing 
    the neighborRange, will change the amount of flocking the members do. Another example is how the speedModifier, if set to a high value, will reduce 
    flocking as well, because the members are then more likely to move out of neighborRange.
    */

    public float speed = 2.8f;              //The approximate starting speed
    public float speedModifyer = 0.5f;      //The modifier used to make the member move with varying speeds. (between 0-1f)
    public float rotationSpeed = 2f;        //How fast the member turns
    public float applyRulesFactor = 5f;     //Apply the 3 basic rules only one in <applyRulesFactor> times. This allows fish to sometimes swim away from the group, etc.
    public float neighborRange = 5f;        //When is a member considered a neighbor, and is thus eligible for grouping.
    public float tooCloseRange = 1f;        //When is another member too close.
    public float groupSpeedReset = .7f;     //The approximate speed a group will start with in the beginning of a frame. The avarage speed is then calculated including this number.

    public float scaredDist = 1.5f;         // How close to a scary object does the member need to be, to start fleeing.
    public float fleeSpeed = 5f;            //The speed modifier used for fleeing

    public float avoidDist = .5f;           //How close to an avoidable object does the member need to be, to swim away.
    public float aloneSpeed = 1.5f;         //Move faster while alone

    //public Vector3 destination;
    #endregion

    void Start()
    {
        //destination = transform.position;
        initSpeed = speed;

        //Set the speed to a value slightly higher or lower, so the fish will move with varying speeds.
        speed = CalculateSpeed(1f, true);//The float parameter in CalulateSpeed is a speed modifyer. 1f means base speed, higher = faster, lower = slower. 

        goalPos = flock.getGoalPos();
    }

    void Update()
    {
        //if (Random.Range(0, 1000) <= 1f)
            //print(goalPos.transform.position);
            //goalPos = flock.getGoalPos();
        if (!instatiated)
            InstatiateFish(); //Fill the array of fish from the GlobalFlock instance. We cant do this in Start() since the array is not full yet until the last fish is instatiated. Every fish needs to do this of course

        //1. If the fish is out of allowed range turnAround = true, otherwise turnAround = false
        turnAround = TurnAroundSwitch();

        if (turnAround)
        {
            //Turn the fish around ..
            Vector3 dir = flock.setRandomPosInArea(.8f) - transform.position;
            Rotate(dir);
            speed = CalculateSpeed(1f, false);// .. and randomize the speed
        }

        //2. Apply flocking rules
        if (Random.Range(0, applyRulesFactor) < 1)
        {
            ApplyBasicRules();
        }
        if (speed < .5)
            speed = CalculateSpeed(1f, true);
        //3. If an avoidable object is nearby ..
        if (AvoidObjectNearby())
        {
            Rotate(avoidDirection);
            CalculateSpeed(1f, true); // .. move away 
        }

        //4. If a scary object is nearby ..
        if (ScaryObjectNearby())
        {
            Rotate(scaredDirection);
            CalculateSpeed(fleeSpeed, true); // .. flee fast 
        }

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

        //goalPos = flock.getGoalPos();
        Vector3 thisPos = transform.position;

        float groupSpeed = CalculateSpeed(groupSpeedReset, true);
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
                    //if(Random.Range(0,100) < 1)
                        //goalPos = anotherFish.GetComponent<Fish>().GetGoalPos();
                }
            }
        }

        if (groupSize > 1)
        {
            //Turn towards the center of the group and in the direction of the goal .. 
            groupCenter = groupCenter / groupSize + (goalPos.transform.position - thisPos);
            speed = groupSpeed / groupSize; //.. then change the speed of this fish to the avarage speed of the group.

            Vector3 dir = groupCenter + avoidFishDir;

            if (dir != Vector3.zero)
                Rotate(dir);
        }
        else {
            CalculateSpeed(aloneSpeed, false);

            Vector3 dir = groupCenter + goalPos.transform.position;

            if (dir != Vector3.zero)
                Rotate(dir);
        }
    }

   /* private GameObject GetGoalPos()
    {
        return goalPos;
    }*/

    #region Utilities
    private void Rotate(Vector3 dir)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }

    private float CalculateSpeed(float burst, bool init)
    {
        if(init)
            speed = initSpeed;

        speed *= burst;
        return flock.SlightlyRandomizeValue(speed, speedModifyer);
    }

    private bool TurnAroundSwitch()
    {
        Vector3 x = new Vector3(transform.position.x, 0, 0);
        Vector3 y = new Vector3(0, transform.position.y, 0);
        Vector3 z = new Vector3(0, 0, transform.position.z);
        Transform t = flock.SpawnArea.transform;

        if (Vector3.Distance(x, Vector3.zero) > t.localScale.x / 2 ||
            Vector3.Distance(y, Vector3.zero) > t.localScale.y / 2 ||
            Vector3.Distance(z, Vector3.zero) > t.localScale.z / 2)
            return true;
        else
            return false;
    }
    #endregion
    #region Accessors & Internal Utilities
    public float GetSpeed()
    {
        return speed;
    }

    public void SetFlockReference(GlobalFlock f)
    {
        flock = f;
    }

    private void InstatiateFish()
    {
        allFish = flock.getAllFish();
        instatiated = true;
    }
    #endregion
}
