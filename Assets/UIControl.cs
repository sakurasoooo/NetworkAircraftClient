using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIControl : MonoBehaviour
{
    // create a timer 
    private float timer = 0.0f;
    
    [SerializeField]
    private NetworkManager networkManager;
    
    // Start is called before the first frame update
    void Start()
    {
        if (networkManager == null) networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // decrease the timer, but clamp it at -10
        timer = Mathf.Clamp(timer - Time.deltaTime, -10.0f, 10.0f);
    }
    
    public void  Shoot()
    {
        // print button pressed
        // Debug.Log("Shoot");
        if (timer <= 0)
        {
            if (networkManager == null) networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
            networkManager.SendAttackData();
            timer = 1.0f;
        }
    }
}
