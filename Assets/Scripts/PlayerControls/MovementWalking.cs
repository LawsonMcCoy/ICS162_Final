using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementWalking : MovementMode
{
    [SerializeField] private float walkSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float jumpForceVertical; //How strong the player can jump upwards
    [SerializeField] private float jumpForceHonrizontal; //How strong the player can jump horizontally
    [SerializeField] private float jumpForceStationary; //How strong the player can jump when not moving
    [SerializeField] private float staminaRegainRate; //The amount of stamina regain per second while walking
    [SerializeField] private float dashJumpForce; //How strong the player can jump at the end of dash
    [SerializeField] private float walkingDampingCoefficient; //The damping coefficient to the stop the player
    [SerializeField] private float staminaSprintLostRate; //The amount of stamina lost per second whening sprinting
    [SerializeField] private float sprintModifier; //The multiplier to the speed when you are sprinting

    private float turnValue;
    private bool sprinting; //A boolean to check if the player is sprinting

    //readable data

    //A bool value that is true when on the ground and false otherwise updated in the CheckGroundStatus function
    public bool onGround
    {
        get;
        private set;
    }
    
    //Helper UI
    private bool midairTransition;
    private bool previousBoolean;

    protected override void Awake()
    {
        base.Awake();

        //Update to walk speed
        speed = walkSpeed;

        turnValue = 0;
        midairTransition = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        StartCoroutine(DelayInput());

        CheckGroundStatus();

        //Enable gravity when walking
        // self.rigidbody.useGravity = true; 

        CheckForTransitions();      //recalibrate transition after hovering/flying
        previousBoolean = !midairTransition;
    }

    //a helper function to check if the player is on the ground
    //and update values accordingly
    private bool CheckGroundStatus()
    {
        RaycastHit groundInfo;
        if (IsGrounded(out groundInfo))
        {
            onGround = true;

            //Disable physics rotation
            self.rigidbody.freezeRotation = true;

            //reset the rotate transform.up is the same as Vector3.up
            Vector3 currentEuler = self.rigidbody.rotation.eulerAngles; //get the Euler angles
            currentEuler.x = 0.0f; //set the rotation around x axis to 0
            currentEuler.z = 0.0f; //set the rotation around z axis to 0

            //Now we only have an rotation around the y axis, so up with this rotation is Vector3.up
            self.rigidbody.MoveRotation(Quaternion.Euler(currentEuler));

            return true;
        }
        else
        {
            onGround = false;

            //enable physics rotation while falling
            self.rigidbody.freezeRotation = false;

            return false;
        }
    }

    protected override void FixedUpdate()
    {
        //make sure that MovementMode fixed update is called first
        base.FixedUpdate();

        //check to see if the player is on the ground or in midair
        //I will likely changed this later it doesn't have to check
        //every update loop
        // Debug.Log(CheckGroundStatus());
        //Debug.Log($"Test is onGround {onGround}");

        if (CheckGroundStatus())
        {
            //On ground 

            //move the player 
            if (!forceNoMovement)
            {
                Vector3 horizontalVelocity = self.rigidbody.velocity;
                horizontalVelocity.y = 0.0f; //set vertical component to zero
                if (!sprinting)
                {
                    AddForce(walkingDampingCoefficient * (moveVector - horizontalVelocity), ForceMode.Force);
                }
                else
                {
                    AddForce(walkingDampingCoefficient * ((sprintModifier * moveVector) - horizontalVelocity), ForceMode.Force);
                }
            }

            //rotate the player
            Quaternion newRotation = self.rigidbody.rotation * Quaternion.Euler(0, turnValue * Time.fixedDeltaTime, 0);
            self.rigidbody.rotation = newRotation;

            if (!sprinting)
            {
                //Regain stamina when on ground only
                stamina.Add(staminaRegainRate * Time.fixedDeltaTime);
            }
            else
            {
                //when sprinting lose stamina
                stamina.Subtract(staminaSprintLostRate * Time.fixedDeltaTime);

                if (stamina.ResourceAmount() == 0)
                {
                    sprinting = false;
                }
            }
        }
        else
        {
            //In Midair
            
            //we will automatically transition to either hovering or gliding

            //First so that we can jump properly the transition will only occur 
            //if we are falling
            if (/*self.rigidbody.velocity.y < 0) ||*/ (!forceNoMovement && moveVector != Vector3.zero))
            {
                //next we will check if we are sprinting
                if (sprinting)
                {
                    //if yes then go into a glide
                    Transition(Modes.GLIDING);
                }
                else
                {
                    //if no then go into a hover
                    Transition(Modes.HOVERING);
                }
            }
        }
        
        CheckForTransitions();
        //Regain stamina whenever in walking mode whether on ground or falling
        // stamina.Add(staminaRegainRate * Time.fixedDeltaTime);
    }

    //A visitor function to determine which type of movement mode this script is
    public override void GetMovementUpdate(MovementUpdateReciever updateReciever)
    {
        updateReciever.WalkingUpdate(this);
    }

    private IEnumerator PerformDashJump()
    {
        Debug.Log("Dash Jump");
        //wait until dashing is completed
        yield return new WaitUntil(() => !isDashing);

        Debug.Log($"Perform Dash Jump {dashJumpForce * Vector3.up}");
        //perform the dash jump
        AddForce(dashJumpForce * Vector3.up, ForceMode.Impulse);
        DisableControlForTime(commonData.transitionMovementLockTime); //disable movement briefly after jump
    }
    
    //zeroing out rotational motion during movement restricted events
    public override void StartMovementRestrictedEvent()
    {
        //zero parent's motion
        base.StartMovementRestrictedEvent();

        //zero rotational motion
        turnValue = 0;
    }

    //************
    //player input
    //************

    //mouse input
    protected override void OnLook(InputValue input)
    {
        base.OnLook(input);

        //the x component will rotate the player
        turnValue = mouseInput.x * turnSpeed;
    }

    //space key input
    private void OnJumpTransition(InputValue input)
    {
        if (inputReady)
        {
            if (isDashing)
            {
                //perform a dash jump
                StartCoroutine(PerformDashJump());
            }
            else
            {
                //If on ground then jump otherwise transition to Hover
                if (onGround)
                {
                    //On Ground, jump into the air
                    if (input.isPressed)
                    {
                        //trigger animation for jumping
                        animator.SetTrigger(animatorJump);

                        Vector3 jumpForceVector; //A vector that will represent the force the player
                                                 //is jumping with

                        //compute vertical component
                        if (moveVector == Vector3.zero)
                        {
                            jumpForceVector = Vector3.up * jumpForceStationary;
                        }
                        else
                        {
                            jumpForceVector = Vector3.up * jumpForceVertical;
                        }

                        //add the horizontal component to allow the player to 
                        //jump over a distance by doing a running jump
                        jumpForceVector += moveVector.normalized * jumpForceHonrizontal;

                        //jump with impulse
                        AddForce(jumpForceVector, ForceMode.Impulse);
                        DisableControlForTime(commonData.transitionMovementLockTime); //disable movement briefly after a jump
                    } //end if (!input.isPressed)
                }//end if (onGround)
            } //end else (if (dashing))
        } //end if (inputReady)
    }

    //Shift key input
    private void OnSprintFly(InputValue input)
    {
        if (inputReady)
        {
            if (stamina.ResourceAmount() > 0)
            {
                //If space is held down on the ground then
                //the player is sprinting
                sprinting = input.isPressed;
            }
            else
            {
                if (input.isPressed)
                {
                    //previousBoolean = !midairTransition;
                    // Transition(Modes.GLIDING);
                }
            }
        }
    }

    private void CheckForTransitions()
    {
        if (onGround)
        {
            if (midairTransition)    // if midairTransition is true while on ground
                midairTransition = false;
        }
        else
        {
            if (!midairTransition)  // if midairTransition is false while in midair
                midairTransition = true;
        }
    }
}
