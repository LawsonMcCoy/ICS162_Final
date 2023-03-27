using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openStateName = "Open";
    [SerializeField] private string closeStateName = "Close";
    private bool isOpen = false;

    [SerializeField] private Player player;
    [SerializeField] private float interactDistance = 30.0f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) //left click
        {
            Debug.Log($"Left click Distance: {Vector3.Distance(player.rigidbody.position, transform.position)}");
            Vector3 playerPosition = player.rigidbody.position;

            if (Vector3.Distance(playerPosition, transform.position) <= interactDistance)
            {
                Debug.Log($"Interacting {Vector3.Distance(playerPosition, transform.position)}");
                //interact with the door
                if (!isOpen)
                {
                    Debug.Log("Open");
                    //close the door
                    doorAnimator.SetTrigger(openStateName);
                    isOpen = true;
                }
                else
                {
                    Debug.Log("Close");
                    doorAnimator.SetTrigger(closeStateName);
                    isOpen = false;
                }
            }
        }
    }
}
