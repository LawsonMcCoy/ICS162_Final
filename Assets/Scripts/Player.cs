using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
// [RequireComponent(typeof(PlayerAttack))]

public class Player : CombatEntity
{
    [SerializeField] public PlayerInput playerInput;
    [SerializeField] private Vector3 spawn;
    [SerializeField] private float spawnNumber;
    private PlayerHealth playerHealth;
    [SerializeField] private float yDeathDistance;
    [SerializeField] private float positionToCenterDistance;

    public Vector3 center
    {
        get;
        private set;
    }

    //A property with the most updated active movement mode
    public MovementMode activeMovementMode
    {
        get;
        set;
    }

    private Vector3 defaultScale;
    
    private void Start()
    {
        playerHealth = gameObject.GetComponent<PlayerHealth>();
        if (gameObject.GetComponent<HealthManager>().GetType() ==  typeof(HealthManager))
        {
            Destroy(gameObject.GetComponent<HealthManager>());
        }

        //events subscriptions
        EventManager.Instance.Subscribe(EventTypes.Events.PLAYER_DEATH, Respawn);
        EventManager.Instance.Subscribe(EventTypes.Events.SAVE, UpdateSpawn);

        defaultScale = this.transform.localScale;
    }

    private void Update()
    {
        if (this.transform.position.y <= yDeathDistance && spawn != null)
        {
            //this.transform.position = spawn.position;
            //kill player
            playerHealth.Subtract(playerHealth.ResourceAmount());
            playerHealth.Add(1000f);    //restore health

        }

        //Right now the position of the Ika's model is at the base of the model
        //This results in issues with other parts of the code. This line is meant 
        //to calculate the actually center of the model by translating the position 
        //up (locally) by half of the model y world scale.
        //NOTE: Update is before Coroutines, center will be a frame behind the player's 
        //position during dashing and other couroutine movements
        center = this.transform.position + (positionToCenterDistance * this.transform.up);
    }
    
    private void OnQuit()
    {
        Debug.Log("QUITTING");
        Application.Quit();
    }

    public void UpdateSpawn()
    {
        //Narration manager is not imported so this line is commented, the result of this is that
        //the spawn inputed in editor remains the only spawn point for the entire game, which should
        //be fine
        // spawn = NarrationManager.Instance.getSpawn();
    }
    private void Respawn()
    {
        gameObject.transform.position = spawn;  //reset player position;
    }

    private void OnDestroy()
    {
        //Events unsubscriptions
        EventManager.Instance.Unsubscribe(EventTypes.Events.PLAYER_DEATH, Respawn);
        EventManager.Instance.Unsubscribe(EventTypes.Events.SAVE, UpdateSpawn);
    }

    //Helper functions:
    public Vector3 GetPlayerScale
    {
        get { return defaultScale; }
        private set { }
    }
}
