using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum MovementState
    {
        Standing,
        Walking,
        Running,
        Crouching,
        Falling
    }

    [HideInInspector] public MovementState State;

    [Header("Debug Mode")]

    [SerializeField] bool _debugMode = false;

    [Header("References")]

    [SerializeField] CharacterController controller;
    [SerializeField] InvestigatorStaminaManager staminaManager;
    [SerializeField] HeadBobController headBobController;
    //[SerializeField] MouseLook mouseLook;
    [SerializeField] Transform cameraRoot;
    [SerializeField] FOVManager mainCameraFOVManager;
    [SerializeField] Transform groundCheck;
    [SerializeField] PostProcessingController postProcessing;
    [SerializeField] PlayerAudioController audioController;

    [Header("Control Abilities")]

    public bool enableMovement = true;
    public bool enableJumping = true;
    public bool enableRunning = true;
    public bool enableCrouching = true;

    [Header("Collisions")]

    public bool detectCollisions = true;

    [Header("Ground Check")]

    [SerializeField] float groundDistanceOffset = 0.05f;
    [SerializeField] LayerMask groundMask;

    [Header("Gravity")]

    public bool useGravity = true;
    [SerializeField] float terminalFallingVelocity = 50f;
    [SerializeField] float defaultDownwardsForce = -1.5f;

    [Header("Crouching")]
    
    [SerializeField] float crouchReduceHeightBy = 0.6f;
    [SerializeField] float crouchToggleSpeed = 6f;
    [SerializeField] float crouchMinCeilingGap = 0.05f;

    [Header("Jumping")]

    [SerializeField] float jumpHeight = 0.4f;
    [SerializeField] float jumpMaxWallDistance = 0.05f;

    [Header("Speed")]

    [SerializeField] float speed = 1.5f;
    [SerializeField] float crouchingSpeed = 0.8f;
    [SerializeField] float walkingSpeed = 1.5f;
    [SerializeField] float runningSpeed = 4.5f;

    Vector3 velocity;

    Vector3 lastPosition;
    float actualSpeed = 0f;

    bool isGrounded = false;
    bool wasGrounded = false;
    bool isCrouching = false;
    bool isRunning = false;
    bool stoppedRunning = false;
    bool isJumping = false;

    float standingHeight;
    float crouchingHeight;
    float halfCrouchingHeight;

    float windVolume;

    RaycastHit hit;

    Collider[] groundOverlaps;

    PhysicsMaterial currentPhysicsMaterial;
    PhysicsMaterial lastPhysicsMaterial;

    [Header("Test Variables")]

    [SerializeField] GameObject isGroundedIndicator;
    [SerializeField] GameObject isJumpingIndicator;
    [SerializeField] GameObject checkCapsuleIndicator;

    void Start()
    {
        standingHeight = controller.height;
        crouchingHeight = standingHeight - crouchReduceHeightBy;
        halfCrouchingHeight = standingHeight - (crouchReduceHeightBy / 2);
    }

    void Update()
    {
        groundCheck.localPosition = new Vector3(0f, GetBottomY(), 0f);

        if (groundCheck.localPosition.y - controller.height > transform.position.y)
        {
            transform.position += new Vector3(0f, controller.height, 0f);
            controller.center -= new Vector3(0f, controller.height, 0f);
            cameraRoot.localPosition -= new Vector3(0f, controller.height, 0f);
        }

        //isGrounded = Physics.CheckSphere(groundCheck.position + new Vector3(0f, controller.radius - groundDistanceOffset, 0f), controller.radius, groundMask);
        groundOverlaps = Physics.OverlapSphere(groundCheck.position + new Vector3(0f, controller.radius - groundDistanceOffset, 0f), controller.radius, groundMask);
        isGrounded = groundOverlaps.Length > 0;

        if (isGrounded)
        {
            currentPhysicsMaterial = groundOverlaps[0].material;

            if (currentPhysicsMaterial != lastPhysicsMaterial)
                audioController.SetMovementSounds(currentPhysicsMaterial);

            lastPhysicsMaterial = currentPhysicsMaterial;

            if (!wasGrounded)
                audioController.PlayLandingSound();

            if (velocity.y <= 0f)
                isJumping = false;

            if (velocity.y < defaultDownwardsForce)
                velocity.y = defaultDownwardsForce;

            if (Input.GetButtonDown("Jump") && enableJumping)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * (-2f) * Physics.gravity.y);
                isJumping = true;
                audioController.PlayJumpSound();
            }
        }
        else
        {
            currentPhysicsMaterial = null;

            if (velocity.y <= 0)
                mainCameraFOVManager.SetFallingFOV();
        }

        wasGrounded = isGrounded;

        if (Input.GetButton("Crouch") && enableCrouching)
        {
            isCrouching = true;
            stoppedRunning = false;
            mainCameraFOVManager.SetCrouchingFOV();
            postProcessing.AddCrouchingEffects();
            audioController.SetCrouchingMovementVolume();

            if (controller.height > crouchingHeight)
            {
                float previousHeight = controller.height;

                controller.height = Mathf.Lerp(controller.height, crouchingHeight, crouchToggleSpeed * Time.deltaTime);
                float heightChange = controller.height - previousHeight;
                
                if (isGrounded)
                {
                    controller.center += new Vector3(0f, heightChange / 2, 0f);
                    cameraRoot.localPosition += new Vector3(0f, heightChange, 0f);
                }
                else
                {
                    controller.center -= new Vector3(0f, heightChange / 2, 0f);
                }
            }
        }
        else
        {
            isCrouching = false;
            
            if (controller.height >= halfCrouchingHeight)
            {
                mainCameraFOVManager.SetNormalFOV();
                postProcessing.RemoveCrouchingEffects();
                audioController.SetNormalMovementVolume();
            }

            if (controller.height < standingHeight)
            {
                float previousHeight = controller.height;

                if (!Physics.Raycast(transform.position + new Vector3(0f, GetTopY(), 0f), transform.up, out hit, crouchMinCeilingGap))
                {
                    controller.height = Mathf.Lerp(controller.height, standingHeight, crouchToggleSpeed * Time.deltaTime);
                    float heightChange = controller.height - previousHeight;

                    controller.center += new Vector3(0f, heightChange / 2, 0f);
                    cameraRoot.localPosition += new Vector3(0f, heightChange, 0f);
                }
            }
        }

        if (enableMovement)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 move = Vector3.ClampMagnitude(transform.right * horizontal + transform.forward * vertical, 1f);

            if (Input.GetButton("Run") && enableRunning && !stoppedRunning && staminaManager.CurrentStamina > 0)
            {
                if (isGrounded)
                {
                    if (controller.height >= halfCrouchingHeight)
                    {
                        speed = runningSpeed;
                        isRunning = true;

                        if (move.magnitude > 0f)
                        {
                            mainCameraFOVManager.SetRunningFOV();
                        }
                    }
                    else
                    {
                        if (isRunning)
                        {
                            isRunning = false;
                            stoppedRunning = true;
                        }

                        if (isCrouching || controller.height < halfCrouchingHeight)
                        {
                            speed = crouchingSpeed;
                            mainCameraFOVManager.SetCrouchingFOV();
                        }
                        else
                        {
                            speed = walkingSpeed;
                            mainCameraFOVManager.SetNormalFOV();
                        }
                    }
                }
                else
                {
                    if (!isRunning)
                    {
                        speed = walkingSpeed;
                    }
                    else if (move.magnitude > 0f)
                    {
                        mainCameraFOVManager.SetRunningFOV();
                    }
                }
            }
            else
            {
                if (isRunning)
                {
                    isRunning = false;
                    stoppedRunning = true;
                    mainCameraFOVManager.SetNormalFOV();
                }

                if (isCrouching || controller.height < halfCrouchingHeight)
                {
                    speed = crouchingSpeed;
                    mainCameraFOVManager.SetCrouchingFOV();
                }
                else
                {
                    speed = walkingSpeed;
                    mainCameraFOVManager.SetNormalFOV();
                }
            }

            if (!Input.GetButton("Run") && !staminaManager.IsCriticalStamina)
                stoppedRunning = false;

            if (isGrounded && move.magnitude > 0f)
            {
                if (isRunning)
                    headBobController.Run();
                else
                    headBobController.Walk();
            }
            else
            {
                headBobController.Reset();
            }

            Vector3 upperSphere = new(transform.position.x + controller.center.x, transform.position.y + controller.center.y + (controller.height / 2) - controller.radius, transform.position.z);
            Vector3 lowerSphere = new(transform.position.x + controller.center.x, transform.position.y + controller.center.y - (controller.height / 2) + controller.radius, transform.position.z);
            
            bool obstacle = Physics.CapsuleCast(upperSphere, lowerSphere, controller.radius, move.normalized, out hit, jumpMaxWallDistance, groundMask);

            if (obstacle && isJumping)
            {
                if (hit.normal.y <= controller.slopeLimit / 90f)
                {
                    move += new Vector3(hit.normal.x, 0f, hit.normal.z);
                    if (_debugMode)
                        checkCapsuleIndicator.SetActive(true);
                }
                else if (_debugMode)
                {
                    checkCapsuleIndicator.SetActive(false);
                }
            }
            else if (_debugMode)
            {
                checkCapsuleIndicator.SetActive(false);
            }

            controller.Move(speed * Time.deltaTime * move);
        }

        if (useGravity)
        {
            //mouseLook.horizontalLookRotatesCamera = false;

            velocity.y = Mathf.Clamp(velocity.y + Physics.gravity.y * Time.deltaTime, -terminalFallingVelocity, float.PositiveInfinity);
            controller.Move(velocity * Time.deltaTime);
        }

        actualSpeed = ((transform.position + controller.center - lastPosition) / Time.deltaTime).magnitude;
        lastPosition = transform.position + controller.center;

        // speed = [0; 1] * (4rs - 0.5rs) + 0.5rs
        // [0; 1] = (speed - 0.5rs) / (4rs - 0.5rs)
        windVolume = (actualSpeed - (runningSpeed * 0.5f)) / (runningSpeed * 3.5f);
        audioController.SetWindSoundsVolume(windVolume);

        if (_debugMode)
        {
            isGroundedIndicator.SetActive(isGrounded);
            isJumpingIndicator.SetActive(isJumping);
        }
    }

    float GetTopY()
    {
        return controller.center.y + (controller.height / 2);
    }

    float GetBottomY()
    {
        return controller.center.y - (controller.height / 2);
    }

    public void BlockEverything()
    {
        enableMovement = false;
        enableRunning = false;
        enableJumping = false;
        enableCrouching = false;

        velocity = Vector3.zero;
        
        useGravity = false;
    }

    public void UnblockEverything()
    {
        enableMovement = true;
        enableRunning = true;
        enableJumping = true;
        enableCrouching = true;

        useGravity = true;
    }
    public float GetCameraYOffset()
    {
        return cameraRoot.localPosition.y;
    }
}
