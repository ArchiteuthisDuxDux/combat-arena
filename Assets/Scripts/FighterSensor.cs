using UnityEngine;

public class FighterSensor : MonoBehaviour
{
    [Header("Detection")]
    public float detectionDistance = 10f;
    public float sphereRadius = 0.5f;
    public LayerMask fighterLayer;

    [Range(1, 32)]
    public int rayCount = 12;

    [Header("Observations")]
    public int[] ObjectTypes;
    public float[] Distances;

    private TeamMember myTeam;

    private Vector3[] debugOrigins;
    private Vector3[] debugHitPoints;
    private bool[] debugHits;

    private void Start()
    {
        myTeam = GetComponentInParent<TeamMember>();

        InitializeArrays();
    }

    private void Update()
    {
        //DetectFighter360();
    }
    private void InitializeArrays()
    {
        ObjectTypes = new int[rayCount];
        Distances = new float[rayCount];

        debugOrigins = new Vector3[rayCount];
        debugHitPoints = new Vector3[rayCount];
        debugHits = new bool[rayCount];
    }
    private void DetectFighter360()
    {
        EnsureArrays();
        Vector3 origin = transform.position;
        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            ObjectTypes[i] = 0;
            Distances[i] = 1f;

            Vector3 localDir = Quaternion.Euler(0f, i * angleStep, 0f) * Vector3.forward;
            Vector3 direction = transform.rotation * localDir;

            debugOrigins[i] = origin;
            debugHits[i] = false;
            debugHitPoints[i] = origin + direction * detectionDistance;

            if (Physics.SphereCast(
                origin,
                sphereRadius,
                direction,
                out RaycastHit hit,
                detectionDistance,
                fighterLayer))
            {
                debugHits[i] = true;
                debugHitPoints[i] = hit.point;

                TeamMember otherTeam = hit.collider.GetComponentInParent<TeamMember>();
                if (otherTeam != null && myTeam != null)
                {
                    bool ally = otherTeam.TeamId == myTeam.TeamId;
                    ObjectTypes[i] = ally ? 1 : 2;
                    Distances[i] = hit.distance / detectionDistance;

                    /*
                    Debug.Log($"{name} Ray {i} sees {otherTeam.name} | " +
                              $"Type {(ally ? "ALLY" : "ENEMY")} | " +
                              $"Distance {Distances[i]:F2}");
                    */
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 localDir = Quaternion.Euler(0f, i * angleStep, 0f) * Vector3.forward;
            Vector3 direction = transform.rotation * localDir;
            Vector3 endPoint = origin + direction * detectionDistance;

            bool hit = false;
            Vector3 hitPoint = endPoint;

            if (Application.isPlaying && debugHits != null && i < debugHits.Length)
            {
                hit = debugHits[i];
                hitPoint = debugHitPoints[i];
            }

            Gizmos.color = hit ? Color.green : Color.yellow;
            Gizmos.DrawLine(origin, hit ? hitPoint : endPoint);
            Gizmos.DrawWireSphere(hit ? hitPoint : endPoint, sphereRadius * 0.2f);
        }
    }

    private void EnsureArrays()
    {
        if (ObjectTypes == null || ObjectTypes.Length != rayCount)
        {
            InitializeArrays();
        }
    }

    public void UpdateSensor()
    {
        DetectFighter360();
    }
}