using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float m_JumpForce = 400f;                          // Amount of jump force adden when the player jumps
    [Range(0, 1f)] [SerializeField] private float m_CrouchSpeed = .36f;          // Amount of max speed applied when courching
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;   // How much to smooth the movement (1 = 100%)
    [Range(0, 5f)] [SerializeField] private float fallMultiplier = 2f;       // Multiplier to determine how fast player should be falling.
    [SerializeField] private bool m_AirControl = false;                         // If the player has control in the air
    [SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground for the character
    [SerializeField] private Transform m_CeilingCheck;                          // A position to check for ceilings.
    [SerializeField] private Transform m_GroundCheck;                           // A position to check for ground.
    [SerializeField] private Collider m_CrouchDisableCollider;                  // A collider that will be disabled when crouching
    [SerializeField] private Transform m_AttackPoint;                            // The area that the attack is performed

    const float k_GroundedRadius = .2f;         // Radius of the overlap circle to determine if grounded
    const float k_CeilingRadius = .2f;          // Radius of the overlap circle to deterrmine if the player can stand up
    private bool m_FacingRight = true;          // Determine which way the player is facing.
    public bool m_Directionright = true;
    private bool m_wasCrouching = false;
    private bool m_Grounded = true;                    // whether or not the player is grounded.
    private Vector3 m_Velocity = Vector3.zero;  // The speed of the player
    private Rigidbody m_Rigidbody;

    private readonly Collider[] _colliders = new Collider[10];
    private static PlayerController playerInstance; // IMPORTANT: the player instance to avoid duplication

    [Header("Events")]
    [Space]

    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;

    private void Awake()
    {
        // Ensure the Player object is not destroyed/duplicated when loading new scenes
        DontDestroyOnLoad(this);

        if(playerInstance == null) 
        {
            playerInstance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        m_Rigidbody = GetComponent<Rigidbody>();

        OnLandEvent ??= new UnityEvent();
        OnCrouchEvent ??= new BoolEvent();
    }

    private void Update()
    {
        // Adjust the falling speed
        if (m_Rigidbody.velocity.y < 0)
        {
            m_Rigidbody.velocity += fallMultiplier * Physics.gravity.y * Time.deltaTime * Vector3.up;
        }
    }

    private void FixedUpdate()
    {
        bool wasGrounded = m_Grounded;
        m_Grounded = false;
        // Check a radius around the groundcheck position for any objects designated as ground
        int numColliders = Physics.OverlapSphereNonAlloc(m_GroundCheck.position, k_GroundedRadius, _colliders, m_WhatIsGround);
        for (int i = 0; i < numColliders; i++)
        {
            //if at least 1 object is colliding, that is not the player
            if (_colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                // check when player has landed
                if (!wasGrounded)
                    OnLandEvent.Invoke();
            }
        }
    }

    public void Move(float HorizontalMove, float VerticalMove, bool crouch, bool jump)
    {
        // If Crouching, check to see if player can stand up
        if (!crouch)
        {
            //If the player has a ceiling preventing them from standing up, keep them crouching
            int numColliders = Physics.OverlapSphereNonAlloc(m_CeilingCheck.position, k_CeilingRadius, _colliders, m_WhatIsGround);
            if (numColliders > 0)
                crouch = true;
        }

        // Only control player if grounded or airControl is true
        if (m_Grounded || m_AirControl)
        {
            if (crouch)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // Reduce the speed by the multiplier
                HorizontalMove *= m_CrouchSpeed;
                VerticalMove *= m_CrouchSpeed;

                // Disable one of the colliders wwhen crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;

            }
            else
            {
                // Enable collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            // Move the character by finding the target velocity
            Vector3 targetVelocity = new(HorizontalMove * 10f, m_Rigidbody.velocity.y, VerticalMove * 10f);
            // And then smoothing it out and applying it to the character
            m_Rigidbody.velocity = Vector3.SmoothDamp(m_Rigidbody.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            if(HorizontalMove > 0)
            {
                m_Directionright = true;
            }
            else
            {
                m_Directionright = false;
            }

            // If the input is moving the player right and the player is facing left...
            if (HorizontalMove > 0 && !m_FacingRight)
            {
                FlipAttackPoint();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (HorizontalMove < 0 && m_FacingRight)
            {
                FlipAttackPoint();
            }

        }
        // If the player should jump
        if (m_Grounded && jump)
        {
            m_Rigidbody.AddForce(new Vector3(0f, m_JumpForce, 0f));
        }
    }

    private void FlipAttackPoint()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        //Flip the position of the attack point to be always in front of the player
        m_AttackPoint.RotateAround(transform.position, Vector3.up, -45);
        
    }
}
