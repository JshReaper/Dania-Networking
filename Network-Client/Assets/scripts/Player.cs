using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
   [SerializeField]  private int id;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public int MyId { get {return id; } set {id = value; } }



    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void CmdFire()
    {
        // Create the Bullet from the Bullet Prefab
        var bullet = (GameObject)Instantiate(
            bulletPrefab,
            bulletSpawn.position,
            bulletSpawn.rotation);

        // Add velocity to the bullet
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 15;



        // Destroy the bullet after 2 seconds
        Destroy(bullet, 2.0f);
    }
}
