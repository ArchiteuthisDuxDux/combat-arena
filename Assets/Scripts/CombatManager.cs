using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class CombatManager : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField] private GameObject fighterPrefab;
    [SerializeField] private int totalAgents = 4;
    [SerializeField] private int teamCount = 2;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float roundRestartDelay = 1.0f;

    [Header("Visual options")]
    [SerializeField] private float colorSaturation = 0.8f;
    [SerializeField] private float colorBrightness = 0.9f;

    [Header("Round time limit")]
    [SerializeField] private float roundTimeLimit = 120f;

    [Header("Rewards")]
    [SerializeField] private CombatRewardSystem rewardSystem;

    private readonly List<FighterRuntime> fighters = new List<FighterRuntime>();
    private readonly Dictionary<int, int> aliveCountByTeam = new Dictionary<int, int>();
    private bool roundEnding;
    private Coroutine restartRoutine;

    private float roundStartTime;
    private bool roundTimerActive;

    private class FighterRuntime
    {
        public GameObject FighterObject;
        public FighterAgent Agent;
        public FighterHealth Health;
        public TeamMember TeamMember;
    }

    private void Start()
    {
        if (rewardSystem == null)
        {
            Debug.LogWarning("CombatRewardSystem is not assigned.");
        }
        if (fighterPrefab == null)
        {
            Debug.LogError("Fighter prefab is not assigned.");
            return;
        }

        if (teamCount < 2)
        {
            Debug.LogError("Team count must be at least 2.");
            return;
        }

        if (totalAgents < teamCount)
        {
            Debug.LogError("Total agents must be at least equal to team count.");
            return;
        }

        if (spawnCenter == null)
        {
            Debug.LogError("Spawn center is not assigned.");
            return;
        }

        SpawnAllFighters();
    }

    private void Update()
    {
        if (!roundTimerActive || roundEnding) return;

        if (Time.time - roundStartTime >= roundTimeLimit)
        {
            Debug.Log("Time's up, round end");
            EndRound();
        }
    }

    private void SpawnAllFighters()
    {
        roundEnding = false;
        roundStartTime = Time.time;
        roundTimerActive = true;

        fighters.Clear();
        aliveCountByTeam.Clear();

        int baseAmount = totalAgents / teamCount;
        int remainder = totalAgents % teamCount;

        for (int teamId = 0; teamId < teamCount; teamId++)
        {
            int teamSize = baseAmount + (teamId < remainder ? 1 : 0);

            for (int i = 0; i < teamSize; i++)
            {
                SpawnFighter(teamId);
            }
        }
    }

    private void SpawnFighter(int teamId)
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0f;

        Vector3 spawnPos = spawnCenter.position + randomOffset;
        float randomYaw = Random.Range(0f, 360f);
        Quaternion spawnRot = spawnCenter.rotation * Quaternion.Euler(0f, randomYaw, 0f);

        GameObject fighterObject = Instantiate(fighterPrefab, spawnPos, spawnRot);

        TeamMember teamMember = fighterObject.GetComponent<TeamMember>();
        if (teamMember == null)
        {
            Debug.LogError("TeamMember component is missing on fighter prefab.");
            Destroy(fighterObject);
            return;
        }
        teamMember.SetTeam(teamId);

        FighterVisual visual = fighterObject.GetComponent<FighterVisual>();
        if (visual == null)
        {
            Debug.LogError("FighterVisual component is missing on fighter prefab.");
            Destroy(fighterObject);
            return;
        }
        visual.SetColor(GetTeamColor(teamId));

        FighterHealth health = fighterObject.GetComponent<FighterHealth>();
        if (health == null)
        {
            Debug.LogError("FighterHealth component is missing on fighter prefab.");
            Destroy(fighterObject);
            return;
        }

        FighterAgent agent = fighterObject.GetComponent<FighterAgent>();
        if (agent == null)
        {
            Debug.LogError("FighterAgent component is missing on fighter prefab.");
            Destroy(fighterObject);
            return;
        }

        health.Died += OnFighterDied;

        fighters.Add(new FighterRuntime
        {
            FighterObject = fighterObject,
            Agent = agent,
            Health = health,
            TeamMember = teamMember
        });

        if (!aliveCountByTeam.ContainsKey(teamId))
            aliveCountByTeam[teamId] = 0;

        aliveCountByTeam[teamId]++;

        //Debug.Log($"Spawned fighter {fighterObject.name} | Team {teamId} at {spawnPos}");
    }

    private void OnFighterDied(FighterHealth deadHealth)
    {
        if (deadHealth == null)
            return;

        FighterRuntime runtime = fighters.Find(f => f.Health == deadHealth);

        if (runtime == null)
            return;

        deadHealth.Died -= OnFighterDied;

        int deadTeamId = runtime.TeamMember != null
            ? runtime.TeamMember.TeamId
            : -1;


        // Штраф за смерть
        if (rewardSystem != null && runtime.Agent != null)
        {
            rewardSystem.GiveDeathPenalty(runtime.Agent);
        }


        // Удаляем из списка живых
        fighters.Remove(runtime);


        if (deadTeamId >= 0 && aliveCountByTeam.ContainsKey(deadTeamId))
        {
            aliveCountByTeam[deadTeamId]--;

            if (aliveCountByTeam[deadTeamId] <= 0)
                aliveCountByTeam.Remove(deadTeamId);
        }


        Debug.Log(
            $"Fighter died | Team {deadTeamId} | " +
            $"Alive teams: {aliveCountByTeam.Count}"
        );


        Destroy(runtime.FighterObject);


        // Проверка победы
        if (!roundEnding && aliveCountByTeam.Count <= 1)
        {
            GiveVictoryReward();
            EndRound();
        }
    }
    private void GiveVictoryReward()
    {
        if (rewardSystem == null)
            return;

        if (aliveCountByTeam.Count != 1)
            return;


        int winningTeam = -1;

        foreach (var team in aliveCountByTeam)
        {
            winningTeam = team.Key;
            break;
        }


        Debug.Log($"Victory team: {winningTeam}");


        foreach (FighterRuntime fighter in fighters)
        {
            if (fighter.TeamMember.TeamId == winningTeam)
            {
                rewardSystem.GiveVictoryReward(fighter.Agent);
            }
        }
    }
    private void EndRound()
    {
        if (roundEnding)
            return;

        roundEnding = true;

        if (restartRoutine != null)
            StopCoroutine(restartRoutine);

        restartRoutine = StartCoroutine(RestartRoundRoutine());

        roundTimerActive = false;
    }

    private IEnumerator RestartRoundRoutine()
    {
        yield return new WaitForSeconds(roundRestartDelay);

        for (int i = 0; i < fighters.Count; i++)
        {
            FighterRuntime runtime = fighters[i];

            if (runtime.Agent != null)
                runtime.Agent.EndEpisode();

            if (runtime.FighterObject != null)
                Destroy(runtime.FighterObject);
        }

        fighters.Clear();
        aliveCountByTeam.Clear();

        SpawnAllFighters();
        roundEnding = false;
        restartRoutine = null;
    }

    private Color GetTeamColor(int teamId)
    {
        float hue = (float)teamId / teamCount;
        return Color.HSVToRGB(hue, colorSaturation, colorBrightness);
    }
}