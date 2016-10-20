using UnityEngine;
using System.Collections;
//using Cubiquity;

public class Fish : MonoBehaviour
{
    #region Variables
    private GlobalFlock flock;              //Reference to the instantiated "parent" script, Flock.cs 
    private FlockAIUtilities utilities;     //Own instance of utilities

    private GameObject[] allFish;           //Contains all members of this flock
    private bool instatiated = false;       //This becomes true when all members have been retrieved from the Flock.cs instantiation.

    private float initSpeed;                //Each members original speed. Not changing. 
    private float initTurnTimer;            //Used for resetting the turntimer
    //private float initInteractTimer;
    //private Vector3 averageHeading;
    //private Vector3 averagePosition;
    private Vector3 scaredDirection;        //Stores the direction to move in when scared
    private Vector3 avoidDirection;         //Stores the direction to move in when avoiding
    private Vector3 avoidTerrainDirection;
    private Vector3 headPos;

    private GameObject goalPos;             //The overall position the member is moving towards

    private bool turning = false;           //Determines whether the fish recently turned around from one of the sides
    //private bool interacting = false;       //Determines whether the fish recently interacted with a static object like terrain, or another fish

    private enum States { flocking, resting, playing };
    private States state = States.flocking;

    /* Public Variables Description
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

    public float turnTimer = 2f;
    //public float interactTimer = .2f;
    public float viewDistance = 2f;


    public string[] dontCollideWith;        //Strings with the names of objects you dont want fish to collide with. Other fish is one example since the rules and raycasts detrmine what to do when they get near. Glass sides are handled more efficiently by RotationSwitch() as another example, and should therefore not be included in CollisionDetection.
    //public string[] visibleObjects;         //Names of objects that will be processed by the fishVision function.
    public string terrainNodeName = "OctreeNode";   //(Not needed with regular terrains, use tag instead) Replace with the name (or part of it) of the terrain or terrain nodes you want to hit with raycasting. Can be done simpler with tags, but not when using a voxel terrain generated with Cubiquity. 
    public Transform head;

    #endregion

    void Start()
    {
        utilities = new FlockAIUtilities();

        //volume = GameObject.FindGameObjectWithTag("Terrain").GetComponent<TerrainVolume>();

        initSpeed = speed;
        initTurnTimer = turnTimer;
        //initInteractTimer = interactTimer;

        //Set the speed to a value slightly higher or lower, so the fish will move with varying speeds.
        speed = CalculateSpeed(1f, true);

        //Choose a random initial goal.
        goalPos = flock.getGoalPos();
    }

    void Update()
    {   
        //Fill the array of fish from the GlobalFlock instance. Can't do this in Start() since the array is not full yet until the last fish is instatiated. Every fish needs to do this of course
        if (!instatiated)
            InstatiateFish(); 
        //What needs to happen before the state update
        PreStateUpdate();

        //Apply rules depending on state.
        if(!turning)
            switch (state)
            {
                //State 1: Flocking
                case States.flocking:
                    FlockingUpdate();
                    break;
                //State 2: Resting
                case States.resting:
                    RestingUpdate();
                    break;
                //State 3: Playful
                case States.playing:
                    PlayingUpdate();
                    break;
            }

        if (speed < .5)
            speed = CalculateSpeed(.8f, true);

        //What needs to happen after the state update
        PostStateUpdate();

        //Move along the fish's local Z axis (Forward)
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
    }

    void FixedUpdate() {


    }

    #region Pre & Post Updates
    private void PreStateUpdate() //This will only happen if it is not overwritten by the current state, or PostAllStatesUpdate()
    {
        if (turning)
        {
            turnTimer -= Time.deltaTime;
            if (turnTimer <= 0)
            {
                turning = false;
                turnTimer = initTurnTimer;
            }
        }

        /*if (interacting)
        {
            interactTimer -= Time.deltaTime;
            if (interactTimer <= 0)
            {
                interacting = false;
                interactTimer = initInteractTimer;
            }
        }*/


    }

    private void PostStateUpdate() //This will owerwrite changes made by the current state.
    {
        //If the fish is out of allowed range, find a new target position within the area.
        if (TurnAroundSwitch())
        {
            //Turn the fish around ..
            Vector3 dir = utilities.setRandomPosInArea(flock.getSpawnArea(), .4f) - transform.position; //.. find a new target position
            Rotate(dir);
            CalculateSpeed(1f, true);// .. and randomize the speed
            turning = true;
        }

        //If an avoidable object is nearby ..
        if (AvoidObjectNearby())
        {
            Rotate(avoidDirection);
            CalculateSpeed(1f, true); // .. move away 
        }

        //If a scary object is nearby ..
        if (ScaryObjectNearby())
        {
            Rotate(scaredDirection);
            CalculateSpeed(fleeSpeed, true); // .. flee fast 
        }

        //Use raycasting to give the fish information on its surroundings
        fishVision();

        //If colliding with terrain
        /*if (TerrainCollision())
        {
            Rotate(avoidTerrainDirection);
            CalculateSpeed(1f, true);
        }*/

    }

    void OnCollisionEnter(Collision col)
    {
        //if(!interacting)
            CollisionReaction(col, Color.green);
    }


    void OnCollisionStay(Collision col)
    {
        //if (!interacting)
            CollisionReaction(col, Color.red);
    }

    private void CollisionReaction(Collision col, Color c)
    {
        foreach (ContactPoint contact in col.contacts)
        {
            bool collide = true;
            foreach (string dontCol in dontCollideWith)
            {
                if (contact.otherCollider.transform.name.Contains(dontCol))
                    collide = false;
            }

            if (collide)
            {
                //interacting = true;
                //print(contact.otherCollider.transform.name);
                Debug.DrawRay(contact.point, contact.normal, Color.green, .3f);
                avoidTerrainDirection = contact.normal*.5f;
                Rotate(avoidTerrainDirection);
                CalculateSpeed(1f, true);

            }
        }
    }


    private bool TerrainCollision()
    {
        //Collision safety check
        
        avoidTerrainDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, .2f);
        foreach (Collider c in hitColliders)
        {
            if (!c.transform.name.Contains("Sardine") && !c.transform.name.Contains("Bone") && 
                !c.transform.name.Contains("Glass") && !c.transform.name.Contains("Shark") &&
                !c.transform.name.Contains("Bush") && !c.transform.name.Contains("Goal"))
            {
                //print(c.transform.name);
                avoidTerrainDirection = -(c.gameObject.transform.position - transform.position);
                return true;
            }

        }
        return false;
    }
    #endregion

    #region Vision
    private void fishVision()
    {
        RaycastHit hit;
        headPos = head.transform.position;
        //bool turned = false;

        //Forward Ray
        //Quaternion rot = transform.rotation;
        Ray rayForward = new Ray(headPos, transform.forward);
        if (Physics.Raycast(rayForward, out hit, viewDistance))
        {

            //Rotate to match the x and y eulerAngles of the part of the terrain hit.
            if (hit.transform.name.Contains(terrainNodeName) || hit.transform.tag.Equals("Prop"))
            {
                Debug.DrawRay(headPos, transform.forward * viewDistance, Color.cyan, 2f);
                //print(hit.normal.x + " " +transform.position.normalized.x);
                //print(hit.transform.tag + " or "+ hit.transform.name);
                Rotate(new Vector3(hit.normal.x, hit.normal.y, 0));//hit.normal.x, hit.normal.z
                                                                   //turned = true; interacting = true;
                                                                   //Stransform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-transform.forward), rotationSpeed * Time.deltaTime);
            }

            //If the hit result is another fish in front of this one and it's in another group, sometimes switch to that group
            if (hit.transform.tag.Equals("FlockEntity"))
            {
                Debug.DrawRay(transform.position, transform.forward * viewDistance * 1.5f, Color.cyan, .3f);
                if (Random.Range(0, 20) < 1f && hit.transform.GetComponent<Fish>().GetGoal() != goalPos && transform.parent == hit.transform.parent)
                {
                    goalPos = hit.transform.GetComponent<Fish>().GetGoal();
                }
            }

        }

        //Up Ray
        /*
        Ray rayUp = new Ray(transform.position, transform.up);
        if (Physics.Raycast(rayUp, out hit, viewDistance) && !turned)
        {
            if (hit.transform.name.Contains("Octree"))
            {
                Debug.DrawRay(transform.position, transform.up * viewDistance, Color.cyan);
                Rotate(new Vector3(hit.normal.x, hit.normal.y, 0)*2);
                turned = true; interacting = true;
            }
        }*/
        Vector3 rayStartPos = new Vector3(0, 0, 0);
        //Down Ray
        Ray rayDown = new Ray(headPos, -transform.up);
        if (Physics.Raycast(rayDown, out hit, viewDistance))
        {
            if (hit.transform.name.Contains(terrainNodeName) || hit.transform.tag.Equals("Prop"))
            {
                Debug.DrawRay(headPos, -transform.up * viewDistance, Color.red, .3f);
                Rotate(new Vector3(0, hit.normal.y, 0));//*.65f
                                                        //turned = true; interacting = true;
            }
        }

        //Right Ray
        Ray rayRight = new Ray(headPos, transform.right);
        if (Physics.Raycast(rayRight, out hit, viewDistance))
        {
            if (hit.transform.name.Contains(terrainNodeName) || hit.transform.tag.Equals("Prop"))
            {
                Debug.DrawRay(transform.position, (transform.right) * viewDistance, Color.blue);
                Rotate(new Vector3(hit.normal.x, hit.normal.y, hit.normal.z));
                //turned = true; interacting = true;
            }
        }

        //Left Ray
        Ray rayLeft = new Ray(transform.position, -transform.right);
        if (Physics.Raycast(rayLeft, out hit, viewDistance))
        {
            if (hit.transform.name.Contains(terrainNodeName) || hit.transform.tag.Equals("Prop"))
            {
                Debug.DrawRay(transform.position, -transform.right * viewDistance, Color.blue);
                Rotate(new Vector3(hit.normal.x, hit.normal.y, hit.normal.z));
                //turned = true; interacting = true;
            }
        }



    }
    #endregion

    #region State Updates
    private void FlockingUpdate()
    {
        if (Random.Range(0, applyRulesFactor) < 1)
        {
            Vector3 thisPos = transform.position;
            Vector3 anotherPos;

            Vector3 groupCenter = Vector3.zero;
            Vector3 avoidFishDir = Vector3.zero;

            float groupSpeed = CalculateSpeed(groupSpeedReset, true);
            float dist;

            int groupSize = 1;

            foreach (GameObject anotherFish in allFish)
            {
                if (anotherFish != gameObject)//If not this fish
                {
                    anotherPos = anotherFish.transform.position;
                    dist = Vector3.Distance(thisPos, anotherPos);
                    if (dist <= neighborRange) //If the current other fish is in 'neighborRange'..
                    {
                        // .. add its' position to adjust the groupCenter, and increment the groupSize.
                        groupCenter += anotherPos;
                        groupSize++;

                        if (dist < tooCloseRange) // If the other fish gets too close however, turn away from it.
                        {
                            avoidFishDir = avoidFishDir + (thisPos - anotherPos);
                        }

                        //Prepare to adjust the groupSpeed by adding in the other fish's speed. 
                        groupSpeed += anotherFish.GetComponent<Fish>().GetSpeed();

                    }
                }
            }

            if (groupSize > 1) // If in a group
            {
                //Calculate direction to turn towards the center of the group, ..
                groupCenter = groupCenter / groupSize;
                // .. add in the direction of the goal..
                groupCenter += (goalPos.transform.position - thisPos)*1.35f;
                speed = groupSpeed / groupSize; //.. and change the speed of this fish to the avarage speed of the group.
                speed = utilities.slightlyRandomizeValue(speed, 0.3f);

                //Turn towards groupCenter and the goalPos, and add in avoidFishDir to turn away from nearby fish.
                Vector3 dir = groupCenter + avoidFishDir;
                if (dir != Vector3.zero)
                    Rotate(dir);
            }
            else { // If alone
                CalculateSpeed(aloneSpeed, false);
                Vector3 dir = groupCenter + goalPos.transform.position;

                if (dir != Vector3.zero)
                    Rotate(dir);
            }
        }
    }

    private void PlayingUpdate()
    {
        
    }

    private void RestingUpdate()
    {
        
    }
    #endregion

    #region Interactable Object Methods 
    private bool AvoidObjectNearby()
    {
        avoidDirection = Vector3.zero;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scaredDist);
        foreach (Collider c in hitColliders)
        {
            //if(c.transform.name.Contains(terrainName))
                //print(c.transform.name);
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
    #endregion

    #region Utilities
    private void Rotate(Vector3 dir)
    {
        if(dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    }

    private float CalculateSpeed(float burst, bool init)
    {   //The burst parameter in CalulateSpeed is a speed modifyer. 1f means base speed, higher = faster, lower = slower.
        if (init)
            speed = initSpeed;

        speed *= burst;
        return utilities.slightlyRandomizeValue(speed, speedModifyer);
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

    private void InstatiateFish()
    {
        allFish = flock.getAllFish();
        instatiated = true;
    }

    private void SwimAlongSurface() {
        print("SwimAlongSurface() Not implemented yet");
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

    public GameObject GetGoal() {
        return goalPos;
    }

    #endregion
}
