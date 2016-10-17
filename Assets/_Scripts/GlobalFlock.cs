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

    private Vector3 goalPos = Vector3.zero; //A common goal posistion that used to adjust the position of all members in the group.
    private GameObject[] allFish;           //Holds all members

    public GameObject fishPrefab;           //Whatever fish/bird/human/particle/bacteria you want. The Fish.cs needs to be attached to the prefab.

    public int numFish = 10;                //How many members
    public int spawnArea = 5;               //(half the actual size) //NOTE: use mesh for size instead
    public bool randomizePrefabSize = true; //Whether or not the size of the prefab should be randomized (Only cosmetic)
    public float sizeModifier = .4f;        //If size is randomized, this is how much they will differ from their original size
    public float changeGoalPosFreq = .5f;   //Randomizes goalPos around <changeGoalPosFreq> times in 100 frames

    void Start()
    {
        allFish = new GameObject[numFish];

        for (int i = 0; i < numFish; i++)
        {
            Vector3 pos = setRandomPosInArea(); 
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
        foreach (GameObject fish in allFish) {
            fish.gameObject.transform.localScale += new Vector3(
                SlightlyRandomizeValue(fish.transform.localScale.x, sizeModifier),
                SlightlyRandomizeValue(fish.transform.localScale.x, sizeModifier),
                SlightlyRandomizeValue(fish.transform.localScale.x, sizeModifier)); 
        }

    }

    public float SlightlyRandomizeValue(float val, float modifier)
    {
        return val + val * Random.Range(-modifier, modifier);
    }

    void Update()
    {
        if (Random.Range(0, 100) < changeGoalPosFreq)
            goalPos = setRandomPosInArea();
    }

    public Vector3 setRandomPosInArea() {
        Vector3 p = new Vector3(Random.Range(-spawnArea, spawnArea), Random.Range(-spawnArea, spawnArea), Random.Range(-spawnArea, spawnArea));
        return p;
    }

    internal GameObject[] getAllFish() {
        return allFish;
    }

    internal Vector3 getGoalPos()
    {
        return goalPos;
    }
}
