using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerNetworkController : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerData playerData;
    
    // Start is called before the first frame update
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerData = GetComponent<PlayerData>();
        
        // Start the SendMoveData coroutine
        StartCoroutine(SendMoveData());
    }

    private void Update()
    {
        Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
        transform.position += new Vector3(move.x, move.y, 0) * Time.deltaTime * 0.2f;
    }

    // Coroutine to send move data every 0.2 seconds
    IEnumerator SendMoveData()
    {
        while (playerData.health > 0)
        {
            // Here you can send the "move" data to the server.
            // For example:
            // NetworkManager.Instance.SendMoveData(playerData.uuid, move);
            Vector2 move = playerInput.actions["Move"].ReadValue<Vector2>();
            NetworkManager.Instance.SendMoveData(playerData.uuid, move);
            // Wait for 0.2 seconds
            yield return new WaitForSeconds(0.2f);
        }
    }
}
