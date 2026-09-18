using UnityEngine;

public class TeamMember : MonoBehaviour
{
    [SerializeField] private int teamId;
    public int TeamId => teamId;

    public void SetTeam(int id)
    {
        teamId = id;
    }
}