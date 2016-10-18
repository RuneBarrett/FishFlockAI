using UnityEngine;
using System.Collections;

public class GlobalFlock : MonoBehaviour
{
    #region Refs
    // Flocking reference, AI for Game Developers by Glenn Seemann, David M Bourg Chapter 4
    // https://www.safaribooksonline.com/library/view/ai-for-game/0596005555/ch04.html

    // The ApplyBasicRules() function handles the three basic flocking rules, and is originally based on Holistic3d's imlementation described here.
    // https://www.youtube.com/watch?v=eMpI1eCsIyM
    #endregion
    #region Description
    /*
    Description of the project

    */
    #endregion

    //private Vector3 goalPos = Vector3.zero; //A common goal posistion that used to adjust the position of all members in the group.
    private GameObject[] allFish;           //Holds all members

    public GameObject[] goals;              //Contains the amount of goal meshes to use. 
    //public GameObject goalMesh;             //A visible goalPos.
    public GameObject fishPrefab;           //Whatever fish/bird/human/particle/bacteria you want. The Fish.cs needs to be attached to the prefab.

    public int numFish = 10;                //How many members
    //public int spawnArea = 5;               //(half the actual size) //NOTE: use mesh for size instead
    public GameObject SpawnArea; 
    public bool randomizePrefabSize = true; //Whether or not the size of the prefab should be randomized (Only cosmetic)
    public float sizeModifier = .4f;        //If size is randomized, this is how much they will differ from their original size
    public float changeGoalPosFreq = .5f;   //Randomizes goalPos around <changeGoalPosFreq> times in 100 frames

    void Start()
    {
        allFish = new GameObject[numFish];

        for (int i = 0; i < numFish; i++)
        {
            Vector3 pos = setRandomPosInArea(.5f); 
            GameObject fishObj = Instantiate(fishPrefab, pos, Quaternion.identity) as GameObject;
            fishObj.GetComponent<Fish>().SetFlockReference(this);
            fishObj.transform.parent = transform;
            allFish[i] = fishObj;
        }

        if (randomizePrefabSize)
            RandomizeSize();
        
    }

    private void RandomizeSize()
    {
        if (sizeModifier >= 1f)
            sizeModifier = 0.9f;
             
        foreach (GameObject fish in allFish) {
            fish.gameObject.transform.localScale += new Vector3(
                SlightlyRandomizeValue(fish.transform.localScale.x, sizeModifier),
                SlightlyRandomizeValue(fish.transform.localScale.y, sizeModifier),
                SlightlyRandomizeValue(fish.transform.localScale.z, sizeModifier)); 
        }

    }

    public float SlightlyRandomizeValue(float val, float modifier)
    {
        return val + val * Random.Range(-modifier, modifier);
    }

    void Update()
    {
        foreach (GameObject goal in goals)
        {
            if (Random.Range(0, 1000) < changeGoalPosFreq)
            {
                goal.transform.position = setRandomPosInArea(.8f); //This mesh is just to get a better understanding. To avoid this, simply remove the line below, and replace 'goalMesh.transform.position' with goalPos. You can remove the now unused goalMesh variable as well
                //goalPos = goalMesh.transform.position;
            }
        }
        /*if (Random.Range(0, 1000) < changeGoalPosFreq)
        {
            goalMesh.transform.position = setRandomPosInArea(.8f); //This mesh is just to get a better understanding. To avoid this, simply remove the line below, and replace 'goalMesh.transform.position' with goalPos. You can remove the now unused goalMesh variable as well
            goalPos = goalMesh.transform.position;
        }*/
    
    }

    public Vector3 setRandomPosInArea(float areaPercentage) {
        /*Vector3 p = new Vector3(Random.Range(-spawnArea * areaPercentage, spawnArea * areaPercentage),
                                Random.Range(-spawnArea * areaPercentage, spawnArea * areaPercentage),
                                Random.Range(-spawnArea * areaPercentage, spawnArea * areaPercentage));*/
        if (areaPercentage > 1)
            areaPercentage = 1;

        Transform t = SpawnArea.transform;
        Vector3 p = new Vector3(Random.Range(-t.localScale.x / 2 * areaPercentage, t.localScale.x / 2 * areaPercentage),
                                Random.Range(-t.localScale.y / 2 * areaPercentage, t.localScale.y / 2 * areaPercentage),
                                Random.Range(-t.localScale.z / 2 * areaPercentage, t.localScale.z / 2 * areaPercentage));
        /* //print(t.localScale.x * (1f - areaPercentage/2) + " " + t.localScale.x * areaPercentage/2 + " " +t.localScale.x);
         float x = Random.Range(t.localScale.x * (1f - areaPercentage/2), t.localScale.x * areaPercentage/2);
         float y = Random.Range(t.localScale.y * (1f - areaPercentage/2), t.localScale.y * areaPercentage/2);
         float z = Random.Range(t.localScale.z * (1f - areaPercentage/2), t.localScale.z * areaPercentage/2);
         //print(x+" "+y+" "+ z);
         Vector3 p = new Vector3(x, y, z);*/
        return p;
    }

    internal GameObject[] getAllFish() {
        return allFish;
    }

    internal GameObject getGoalPos()
    {
        //return goalPos;
        return goals[Random.Range(0, goals.Length)];
    }
}
