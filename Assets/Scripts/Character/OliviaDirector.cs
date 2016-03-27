using UnityEngine;
using System.Collections;

public class OliviaDirector : CharDirector {

    #region Public and Private Variables
    public float walkSpd = 1f;
    public float walkTurnSpd = 2f;
    public float sprintMaxSpd = 6f;
    public float sprintTurnSpd = 2f;
    public float sprintAccel = 1f;
    public float fric = 0.25f;
    public float skidFricMod = 0.15f;
    public float rollMin = 5f;
    public float rollSpd = 6f;
    public bool canReceiveInput = true;
    public Transform handXform;
    public Transform saberXform;

    private Quaternion targetRot;
    private float fricMod = 0.0f;

    // State control parameters
    private float walk = 0.0f;
    private float sprint = 0.0f;
    private float fdir = 0.0f;
    private float dir = 0.0f;
    private bool holdSprint = false;
    private bool isSkidding = false;
    private bool canRoll = false;

    // State Hash IDs
    private int idleState;
    private int walkState;
    private int fallState;
    private int sprintState;
    private int skidState;
    private int rollState;
    private int crouchState;
    private int fallSprintTransition;

    private int prevState = -1;

    #endregion


    #region Unity Callbacks
    // Use this for initialization
    protected override void Start () {
        base.Start ();

        targetRot = animator.transform.rotation;

        // Determine the ID's related to each state
        idleState = Animator.StringToHash("Base Layer.Idle");
        walkState = Animator.StringToHash("Base Layer.Walk");
        fallState = Animator.StringToHash("Base Layer.Falling");
        sprintState = Animator.StringToHash("Base Layer.SprintState");
        skidState = Animator.StringToHash("Base Layer.Skid");
        rollState = Animator.StringToHash("Base Layer.Roll");
        crouchState = Animator.StringToHash("Base Layer.Crouch");

        fallSprintTransition = Animator.StringToHash("Base Layer.Falling -> Base Layer.SprintState");
    }

    // Update is called once per frame
    void Update () {
        float h = 0;
        float v = 0;
        bool sp = false;

        if (canReceiveInput) {
            // Get the inputs
            h = Input.GetAxisRaw ("Horizontal");
            v = Input.GetAxis ("Vertical");
            sp = Input.GetButton ("Sprint");
        }

        holdSprint = sp;
        fdir = Mathf.Sign(v);

        // Calculate some of the state variables
        walk = v * v;
        sprint = 0f;
        if (sp)
        {
            sprint = v * v;
        }

        // Turning (interpolated for smoothness)
        dir = dir + (h - dir) * 0.1f;


        animator.transform.rotation = Quaternion.Lerp(animator.transform.rotation, targetRot, 7f * Time.deltaTime);
        UpdateCharacterController();
        // SnapForwardDir(cc.transform.forward);
    }

    protected override void FixedUpdate() {
        base.FixedUpdate ();

        // Apply the state variables
        if (animator)
        {
            float absFwdSpd = GetForwardVelocity().magnitude;
            float fwdNess = Vector3.Dot(new Vector2(cc.transform.forward.x, cc.transform.forward.y), GetForwardVelocity ());
            fwdNess = Mathf.Sign (fwdNess);
            animator.SetFloat("Walk", walk);
            animator.SetFloat("Sprint", sprint);
            animator.SetBool("OnGround", OnGround());
            animator.SetBool("HoldSprint", holdSprint);
            animator.SetBool("IsSkidding", isSkidding);
            animator.SetBool("CanRoll", canRoll);
            animator.SetFloat("FwdSpd", absFwdSpd * fwdNess);
            animator.SetFloat("AbsFwdSpd", absFwdSpd);
            animator.SetFloat("VSpd", velocity.y);
        }
    }

    #endregion


    #region Inherited Methods

    protected override void Move() {
        base.Move();

        // Friction
        if (OnGround ()) {
            float netFriction = Mathf.Max (fric + fricMod, 0);
            Accelerate (netFriction * -1f, 0f, 1000f);
        }
        fricMod = 0.0f;

        // Get the state info and act according the current state
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        if (info.nameHash != idleState)
        {
            // Match the animator position with the cc position as soon as the cc moves
            UpdateAnimator();
        }
    }

    protected override void Step() {
        base.Step();

        float dt = Time.fixedDeltaTime;

        // Make sure we are in the right orientation (mainly for the forward orientation)
        UpdateCharacterController();

        // Get the state info and act according the current state
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        int currentState = info.fullPathHash;
        int currentTransition = animator.GetAnimatorTransitionInfo (0).fullPathHash;


        bool onground = OnGround();

        if (currentState == idleState) {
            Idle (dt);
        } else if (currentState == fallState) {
            Fall ();
        } else if (currentState == walkState) {
            Walk (dt);
        } else if (currentState == sprintState) {
            Sprint (dt);
        } else if (currentState == skidState) {
            Skid ();
        } else if (currentState == rollState) {
            Roll (dt);
        } else if (currentState == crouchState) {
            Crouch ();
        }

        if (currentTransition == fallSprintTransition) {
            Sprint (dt);
        }
            
        // If the player presses the jump key
        if (Input.GetButtonDown("Jump"))
        {
            bool isValidState =
                (currentState != crouchState
                && currentState != rollState
                && currentState != skidState);
            
            if (onground && isValidState) { // AKA a normal jump
                JumpGround ();
            }
        }

        prevState = currentState;
    }

    /*protected override bool OnGround() 
    {
        // Check each of the foot checkers with a gigantic OR gate
        bool footOn = false;
        foreach (ColliderCheck collCheck in footChecks)
        {
            if (collCheck.isMeeting)
            {
                footOn = true;
                break;
            }
        }

        return (base.OnGround() || footOn);
    }*/

    #endregion


    #region State Functions

    void Idle(float dt)
    {
        // If we're trying to turn while sprinting, jank it
        TurnInPlace(1f, dt);

        // Stay still
        this.velocity.x = 0f;
        this.velocity.z = 0f;
    }

    void Fall()
    {
        if (!OnGround())
            canRoll = ((this.velocity.y * -1f) >= rollMin);

        Vector3 look = animator.transform.forward;
        look.y = 0f;

        LookAt(animator.transform.position + look);
    }

    void Walk(float dt)
    {
        targetRot = animator.transform.rotation;
        TurnInPlace(walkTurnSpd, dt);
        // Move forward at a speed of 1
        Vector3 spd = cc.transform.forward * walkSpd * fdir;
        this.velocity = new Vector3(spd.x, this.velocity.y, spd.z);
    }

    void Sprint(float dt)
    {
        // If we're trying to turn while sprinting, jank it
        // TurnInPlace(2f, dt);

        // Move forward at 1m/s^2 to a max speed of 6
        // SnapVelocityDir(cc.transform.forward);
        Accelerate(sprintAccel, 0f, sprintMaxSpd);
        TurnInPlace(sprintTurnSpd, dt);
    }

    void Skid()
    {
        fricMod = skidFricMod;
        isSkidding = (GetForwardVelocity ().magnitude > fric + fricMod);            
    }

    void Roll(float dt)
    {
        if (prevState != rollState)
        {
            Vector3 fwdVel = (cc.transform.forward * rollSpd);
            velocity.x = fwdVel.x;
            velocity.z = fwdVel.z;
        }
        fricMod = -0.9f * fric;
    }

    void Crouch()
    {
        // Can't move forward
        this.velocity.x = 0f;
        this.velocity.z = 0f;
    }


    void JumpGround()
    {
        // Nomrally jump with vert. speed of 7
        this.velocity.y = 7f;

        // Get off the ground and make sure it doesn't register as being on the ground (and setting y-velocity to 0)
        cc.transform.position += Vector3.up * 0.5f;
        cc.Move(Vector3.zero);
    }

    #endregion


    #region Helper Functions

    void Accelerate(float acc, float minSpd, float maxSpd)
    {
        Vector2 fwd = GetForwardVelocity();
        if (fwd == Vector2.zero) {
            // fwd = new Vector2(cc.transform.forward.x, cc.transform.forward.z);
        }
        float newSpd = Mathf.Min(Mathf.Max(fwd.magnitude + acc, minSpd), maxSpd);
        fwd = fwd.normalized * newSpd;
        this.velocity.x = fwd.x;
        this.velocity.z = fwd.y;
    }

    Vector2 GetForwardVelocity()
    {
        return new Vector2(this.velocity.x, this.velocity.z);
    }

    void SnapVelocityDir(Vector3 newDir)
    {
        Vector3 dir = new Vector3(newDir.x, newDir.y, newDir.z);
        Vector2 fwd = new Vector2(this.velocity.x, this.velocity.z);
        this.velocity = dir.normalized * fwd.magnitude;
    }

    void SnapForwardDir(Vector3 newDir)
    {
        Vector2 dir = new Vector2(newDir.x, newDir.z);
        Vector2 fwd = new Vector2(this.velocity.x, this.velocity.z);
        Vector3 newFwd = dir.normalized * fwd.magnitude;
        this.velocity.x = newFwd.x;
        this.velocity.z = newFwd.y;
    }

    // Matches the animator position to the cc position
    void UpdateAnimator()
    {
        animator.transform.position = cc.transform.position;
    }

    void UpdateCharacterController()
    {
        cc.transform.rotation = animator.transform.rotation;
    }

    void TurnInPlace(float degrees, float dt)
    {
        // If we're trying to turn while doing something, jank it
        RotateAround(animator.transform.up, dir * degrees * dt);
        if (degrees != 0) {
            SnapForwardDir(animator.transform.forward);
        }
    }

    void LookAt(Vector3 pos)
    {
        Quaternion save = animator.transform.rotation;
        animator.transform.LookAt(pos);
        targetRot = animator.transform.rotation;
        animator.transform.rotation = save;
    }

    void RotateAround(Vector3 axis, float degrees)
    {
        animator.transform.RotateAround(axis, degrees);
        targetRot = animator.transform.rotation;
    }

    #endregion

}
