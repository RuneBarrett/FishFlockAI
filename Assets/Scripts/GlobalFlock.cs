using UnityEngine;
using System.Collections;

public class GlobalFlock : MonoBehaviour
{
    //Flocking reference, AI for Game Developers by Glenn Seemann, David M Bourg Chapter 4
    // https://www.safaribooksonline.com/library/view/ai-for-game/0596005555/ch04.html

    private string globalObjName;
    public GameObject fishPrefab;

    public int numFish = 10;
    private GameObject[] allFish;

    public int spawnArea = 5;//(half the actual size) //use gameobject for size instead
    public Vector3 goalPos = Vector3.zero;

    void Start()
    {
        allFish = new GameObject[numFish];

        for (int i = 0; i < numFish; i++)
        {
            Vector3 pos = setPosInArea(); 
            GameObject fishObj = Instantiate(fishPrefab, pos, Quaternion.identity) as GameObject;
            fishObj.GetComponent<Fish>().SetFlockReference(this);
            fishObj.transform.parent = transform;
            allFish[i] = fishObj;
        }
        
    }

    void Update()
    {
        if (Random.Range(0, 100) < .5f)
            goalPos = setPosInArea();
    }

    private Vector3 setPosInArea() {
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
