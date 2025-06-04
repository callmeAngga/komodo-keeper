using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform player, destination;
    public GameObject playerg;
    
    [Header("Teleport Settings")]
    public float teleportCooldown = 5f; 
    public float teleportDelay = 0.1f; 
    
    private bool canTeleport = true;
    private static bool isTeleporting = false; 
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canTeleport && !isTeleporting)
        {
            StartCoroutine(TeleportPlayer());
        }        
    }
    
    IEnumerator TeleportPlayer()
    {
        isTeleporting = true;
        canTeleport = false;
        
        yield return new WaitForSeconds(teleportDelay);
        
        playerg.SetActive(false);
        player.position = destination.position;
        playerg.SetActive(true);
        
        yield return new WaitForSeconds(teleportCooldown);
        
        canTeleport = true;
        isTeleporting = false;
    }
}