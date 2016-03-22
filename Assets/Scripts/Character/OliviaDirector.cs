using UnityEngine;
using System.Collections;

public class OliviaDirector : CharDirector {

    #region Public and Private Variables
    public float walkSpd = 1f;
    public float sprintMaxSpd = 6f;
    public float sprintAccel = 1f;
    public float fric = 0.25f;

    private Quaternion targetRot;
    private float fricMod = 0.0f;

    // State control parameters
    private float walk = 0.0f;
    private float sprint = 0.0f;
    private float fdir = 0.0f;
    private float dir = 0.0f;
    private bool holdSprint = false;
    private bool isSkidding = false;

    // State Hash IDs
    private int idleState;
    private int walkState;
    private int fallState;
    private int sprintState;
    private int skidState;

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
    }

    // Update is called once per frame
    void Update () {
        fricMod = 0.0f;

        // Get the inputs
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool sp = Input.GetButton("Sprint");
        holdSprint = sp;
        fdir = Mathf.Sign(v);

        Debug.Log (v);

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
        SnapForwardDir(cc.transform.forward);
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();

        // Apply the state variables
        if (animator)
        {
            animator.SetFloat("Walk", walk);
            animator.SetFloat("Sprint", sprint);
            animator.SetBool("OnGround", OnGround ());
            animator.SetBool("HoldSprint", holdSprint);
            animator.SetBool("IsSkidding", isSkidding);
        }
    }

    #endregion


    #region Inherited Methods

    protected override void Move() {
        base.Move();

        // Friction
        if (OnGround())
            Accelerate((fric + fricMod) * -1f, 0f, 10f);

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
        int currentState = info.nameHash;

        bool onground = OnGround();

        if (currentState == idleState) {
            Idle (dt);
        } else if (currentState == fallState) {
            Fall ();
        } else if (currentState == walkState && onground) {
            Walk (dt);
        } else if (currentState == sprintState && onground) {
            Sprint (dt);
        } else if (currentState == skidState && onground) {
            Skid();
        }
            
        // If the player presses the jump key
        if (Input.GetButtonDown("Jump"))
        {
            if (onground) { // AKA a normal jump
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
        Vector3 look = animator.transform.forward;
        look.y = 0f;

        LookAt(animator.transform.position + look);
    }

    void Walk(float dt)
    {
        // Move forward at a speed of 1
        Vector3 spd = cc.transform.forward * walkSpd * fdir;
        this.velocity = new Vector3(spd.x, this.velocity.y, spd.z);
        targetRot = animator.transform.rotation;
        TurnInPlace(2, dt);
    }

    void Sprint(float dt)
    {
        // If we're trying to turn while sprinting, jank it
        // TurnInPlace(2f, dt);

        // Move forward at 1m/s^2 to a max speed of 6
        // SnapVelocityDir(cc.transform.forward);
        Accelerate(sprintAccel, 0f, sprintMaxSpd);
        TurnInPlace(1, dt);
    }

    void Skid()
    {
        fricMod = 0.1f;
        isSkidding = (GetForwardVelocity().magnitude > fric + fricMod);
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
        Vector2 fwd = new Vector2(this.velocity.x, this.velocity.z);
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
