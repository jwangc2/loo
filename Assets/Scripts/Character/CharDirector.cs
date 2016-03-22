using UnityEngine;
using System.Collections;

public class CharDirector : MonoBehaviour {

    public Animator animator;
    public CharacterController cc;
    public float grav = 9.8f;

    public Vector3 velocity = Vector3.zero;

    protected Vector3 impactVelocity = Vector3.zero;

    // Use this for initialization
    protected virtual void Start () {

    }

    // Update is called once per frame
    protected virtual void FixedUpdate () {
        Move();
    }

    protected virtual void Move() {
        float dt = Time.fixedDeltaTime;

        // If on the ground, reset velocity
        if (OnGround() && this.velocity.y < 0f)
        {
            impactVelocity = velocity;

            int layerMask = 1 << 8; // Mask for player
            layerMask = ~layerMask; // Everything but the player

            if (Physics.Raycast(cc.transform.position, Vector3.down, 0.25f))
                cc.Move(Vector3.down * 0.25f);

            this.velocity.y = -1f * cc.height * 0.5f;
        }
        else
        {
            // Debug.LogWarning("Gravity is in effect");
        }

        this.velocity += Vector3.down * grav * dt;

        // For custom movement after applying gravity
        Step();

        // Debug.Log("Speed: " + this.velocity.magnitude);
        // Debug.Log("Y-Speed: " + this.velocity.y);

        cc.Move(velocity * dt);
        UpdateAnimator();
    }

    protected virtual void Step() {
    }

    protected virtual bool OnGround() {
        return cc.isGrounded;
    }

    public Vector3 GetVelocity()
    {
        return this.velocity;
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

}
