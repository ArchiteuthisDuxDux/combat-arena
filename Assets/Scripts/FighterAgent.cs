using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class FighterAgent : Agent
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float knockbackDamping = 18f;

    [Header("Animator")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string shieldBoolName = "ShieldUp";

    private Rigidbody rb;
    private Animator animator;
    private FighterAnimationEvents animState;
    private FighterSensor fighterSensor;
    private FighterHealth health;
    private FighterWeaponSensor weaponSensor;

    private Vector2 moveInput;
    private float rotateInput;
    private Vector3 knockbackVelocity;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animState = GetComponent<FighterAnimationEvents>();
        fighterSensor = GetComponentInChildren<FighterSensor>();
        health = GetComponent<FighterHealth>();
        weaponSensor = GetComponentInChildren<FighterWeaponSensor>();
    }

    public override void OnEpisodeBegin()
    {
        moveInput = Vector2.zero;
        rotateInput = 0f;
        knockbackVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ApplyKnockback(Vector3 velocityChange)
    {
        knockbackVelocity += velocityChange;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (weaponSensor != null)
        {
            weaponSensor.UpdateSensor();

            for (int i = 0; i < weaponSensor.WeaponTagIDs.Length; i++)
                sensor.AddObservation((float)weaponSensor.WeaponTagIDs[i]);

            for (int i = 0; i < weaponSensor.Distances.Length; i++)
                sensor.AddObservation(weaponSensor.Distances[i]);
        }

        if (fighterSensor != null)
        {
            fighterSensor.UpdateSensor();

            for (int i = 0; i < fighterSensor.ObjectTypes.Length; i++)
                sensor.AddObservation((float)fighterSensor.ObjectTypes[i]);

            for (int i = 0; i < fighterSensor.Distances.Length; i++)
                sensor.AddObservation(fighterSensor.Distances[i]);
        }

        if (health != null)
            sensor.AddObservation((float)health.CurrentHealth / Mathf.Max(1f, health.MaxHealth));
        else
            sensor.AddObservation(0f);

        sensor.AddObservation(animState != null && animState.ShieldRaised ? 1f : 0f);
        sensor.AddObservation(animState != null && animState.IsShieldBusy ? 1f : 0f);
        sensor.AddObservation(animState != null && animState.SwordActive ? 1f : 0f);
        sensor.AddObservation(animState != null && animState.IsSwordBusy ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        moveInput = new Vector2(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1]
        );
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        rotateInput = actions.ContinuousActions[2];

        int attack = actions.DiscreteActions[0];
        int shield = actions.DiscreteActions[1];

        if (attack == 1)
        {
            animator.SetTrigger(attackTriggerName);
        }

        animator.SetBool(shieldBoolName, shield == 1);
    }

    private void FixedUpdate()
    {
        AddReward(-0.00025f);

        Quaternion yawOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        rb.rotation = yawOnly;

        if (rb == null)
            return;

        Vector3 localMove = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        Vector3 worldMove = transform.TransformDirection(localMove);

        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 planarVelocity =
            new Vector3(worldMove.x, 0f, worldMove.z) +
            new Vector3(knockbackVelocity.x, 0f, knockbackVelocity.z);

        rb.linearVelocity = new Vector3(planarVelocity.x, currentVelocity.y, planarVelocity.z);

        float rotationAmount = rotateInput * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotationAmount, 0f));

        knockbackVelocity = Vector3.MoveTowards(
            knockbackVelocity,
            Vector3.zero,
            knockbackDamping * Time.fixedDeltaTime
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        var discrete = actionsOut.DiscreteActions;

        continuous[0] = 0f;
        continuous[1] = 0f;
        continuous[2] = 0f;

        if (Keyboard.current.aKey.isPressed) continuous[0] = -1f;
        if (Keyboard.current.dKey.isPressed) continuous[0] = 1f;
        if (Keyboard.current.sKey.isPressed) continuous[1] = -1f;
        if (Keyboard.current.wKey.isPressed) continuous[1] = 1f;

        if (Keyboard.current.qKey.isPressed) continuous[2] = -1f;
        if (Keyboard.current.eKey.isPressed) continuous[2] = 1f;

        discrete[0] = Keyboard.current.spaceKey.isPressed ? 1 : 0;
        discrete[1] = Keyboard.current.bKey.isPressed ? 1 : 0;
    }
}