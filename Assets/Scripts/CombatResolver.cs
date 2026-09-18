using System;
using UnityEngine;

public class CombatResolver : MonoBehaviour
{
    public static event Action<FighterAgent, FighterAgent, int> OnDamageDealt;
    public static event Action<FighterAgent, FighterAgent> OnBlocked;

    [Header("Knockback")]
    [SerializeField] private float normalKnockbackImpulse = 5f;
    [SerializeField] private float blockedKnockbackImpulse = 1.5f;

    [Header("Shield block")]
    [Range(0f, 180f)]
    [SerializeField] private float frontBlockAngle = 150f;

    [Header("Damage")]
    [SerializeField] private int normalDamage = 25;
    [SerializeField] private int blockedDamage = 0;

    private FighterHealth health;
    private TeamMember myTeam;
    private Rigidbody rootRb;
    private FighterAnimationEvents animState;
    private FighterAgent myAgent;

    private void Awake()
    {
        myTeam = GetComponentInParent<TeamMember>();
        rootRb = GetComponentInParent<Rigidbody>();
        animState = GetComponentInParent<FighterAnimationEvents>();
        health = GetComponentInParent<FighterHealth>();
        myAgent = GetComponentInParent<FighterAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("SwordActive"))
            return;

        TeamMember attackerTeam = other.GetComponentInParent<TeamMember>();

        if (attackerTeam == null || myTeam == null)
            return;

        if (attackerTeam.TeamId == myTeam.TeamId)
            return;

        FighterAgent attackerAgent =
            other.GetComponentInParent<FighterAgent>();

        if (attackerAgent == null || myAgent == null)
            return;

        Vector3 attackDir = (transform.position - other.transform.position).normalized;
        attackDir.y = 0f;
        attackDir.Normalize();

        bool shieldRaised = animState != null && animState.ShieldRaised;

        bool blocked = false;

        if (shieldRaised)
        {
            Vector3 attackerToTarget =
                (other.transform.position - transform.position).normalized;

            attackerToTarget.y = 0f;
            attackerToTarget.Normalize();

            float angle = Vector3.Angle(
                transform.forward,
                attackerToTarget
            );

            blocked = angle <= frontBlockAngle * 0.5f;
        }

        float impulse = blocked
            ? blockedKnockbackImpulse
            : normalKnockbackImpulse;

        myAgent.ApplyKnockback(attackDir * impulse);

        int damage = blocked
            ? blockedDamage
            : normalDamage;

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (blocked)
        {
            OnBlocked?.Invoke(
                attackerAgent,
                myAgent
            );
        }
        else
        {
            OnDamageDealt?.Invoke(
                attackerAgent,
                myAgent,
                damage
            );
        }

        Debug.Log(
            $"{name} hit by {attackerAgent.name} | " +
            $"Blocked={blocked} | " +
            $"ShieldRaised={shieldRaised} | " +
            $"Damage={damage} | " +
            $"Impulse={impulse:F2}"
        );
    }
}