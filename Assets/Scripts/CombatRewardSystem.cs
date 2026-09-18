using UnityEngine;

public class CombatRewardSystem : MonoBehaviour
{
    [Header("Rewards")]
    [SerializeField] private float damageRewardMultiplier = 0.02f;
    [SerializeField] private float damagePenaltyMultiplier = 0.02f;
    [SerializeField] private float blockedReward = 0.1f;
    [SerializeField] private float blockedAttackPenalty = -0.05f;

    [Header("End game rewards")]
    [SerializeField] private float deathPenalty = -5f;
    [SerializeField] private float victoryReward = 5f;

    private void OnEnable()
    {
        CombatResolver.OnDamageDealt += GiveDamageReward;
        CombatResolver.OnBlocked += GiveBlockReward;
    }

    private void OnDisable()
    {
        CombatResolver.OnDamageDealt -= GiveDamageReward;
        CombatResolver.OnBlocked -= GiveBlockReward;
    }


    private void GiveDamageReward(
        FighterAgent attacker,
        FighterAgent defender,
        int damage)
    {
        float reward = damage * damageRewardMultiplier;
        float negativereward = damage * damagePenaltyMultiplier;

        attacker.AddReward(reward);
        defender.AddReward(-negativereward);

        Debug.Log(
            $"[REWARD] {GetTeamInfo(attacker)} damaged {GetTeamInfo(defender)} | " +
            $"Damage: {damage} | " +
            $"Attacker reward: +{reward:F2} | " +
            $"Defender reward: -{reward:F2}"
        );
    }


    private void GiveBlockReward(
        FighterAgent attacker,
        FighterAgent defender)
    {
        attacker.AddReward(blockedAttackPenalty);
        defender.AddReward(blockedReward);

        Debug.Log(
            $"[REWARD] BLOCK | " +
            $"{GetTeamInfo(defender)} blocked attack from {GetTeamInfo(attacker)} | " +
            $"Attacker reward: {blockedAttackPenalty:F2} | " +
            $"Defender reward: +{blockedReward:F2}"
        );
    }

    private string GetTeamInfo(FighterAgent agent)
    {
        TeamMember team = agent.GetComponentInParent<TeamMember>();

        if (team != null)
        {
            return $"{agent.name} [Team {team.TeamId}]";
        }

        return agent.name;
    }

    public void GiveDeathPenalty(FighterAgent deadAgent)
    {
        deadAgent.AddReward(deathPenalty);

        Debug.Log($"[REWARD] DEATH | {GetTeamInfo(deadAgent)} reward {deathPenalty:F2}");
    }

    public void GiveVictoryReward(FighterAgent winner)
    {
        winner.AddReward(victoryReward);

        Debug.Log($"[REWARD] VICTORY | {GetTeamInfo(winner)} reward +{victoryReward:F2}");
    }
}