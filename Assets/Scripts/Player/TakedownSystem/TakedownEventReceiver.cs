using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakedownEventReceiver : MonoBehaviour
{
    private PlayerTakedown playerTakedown;

    private void Start()
    {
        playerTakedown = GetComponent<PlayerTakedown>();
    }

    public void FinishTakedown()
    {
        if (playerTakedown != null)
        {
            playerTakedown.OnTakedownFinished(); 
        }
        else
        {
            Debug.LogWarning("PlayerTakedown component not found!");
        }
    }
}