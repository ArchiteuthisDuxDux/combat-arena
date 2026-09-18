using UnityEngine;

public class FighterWeaponSensor : MonoBehaviour
{
    [Header("Detection")]
    public float detectionDistance = 10f;
    public float sphereRadius = 0.5f;
    public LayerMask weaponLayer;

    [Range(1, 128)]
    public int rayCount = 12;

    [Header("Observations")]
    public int[] WeaponTagIDs;
    public float[] Distances;

    private Transform agentRoot;

    private Vector3[] debugOrigins;
    private Vector3[] debugHitPoints;
    private bool[] debugHits;
    private TeamMember ownTeam;

    private RaycastHit[] hitBuffer = new RaycastHit[32];

    private void Awake()
    {
        agentRoot = transform.root;
        ownTeam = agentRoot.GetComponent<TeamMember>();
        InitializeArrays();
    }

    private void InitializeArrays()
    {
        WeaponTagIDs = new int[rayCount];
        Distances = new float[rayCount];

        debugOrigins = new Vector3[rayCount];
        debugHitPoints = new Vector3[rayCount];
        debugHits = new bool[rayCount];
    }

    private void EnsureArrays()
    {
        if (WeaponTagIDs == null || WeaponTagIDs.Length != rayCount ||
            Distances == null || Distances.Length != rayCount ||
            debugOrigins == null || debugOrigins.Length != rayCount ||
            debugHitPoints == null || debugHitPoints.Length != rayCount ||
            debugHits == null || debugHits.Length != rayCount)
        {
            InitializeArrays();
        }

        if (hitBuffer == null || hitBuffer.Length < 32)
        {
            hitBuffer = new RaycastHit[32];
        }
    }

    private int GetWeaponTagID(string tag)
    {
        switch (tag)
        {
            case "ShieldInactive": return 1;
            case "ShieldActive": return 2;

            case "SwordInactive": return 3;
            case "SwordActive": return 4;
            default: return 0;
        }
    }

    private bool IsPartOfSelf(Collider col)
    {
        return col.transform.IsChildOf(agentRoot);
    }

    public void UpdateSensor()
    {
        //Debug.Log("Weapon sensor update");

        EnsureArrays();

        Vector3 origin = transform.position;
        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            WeaponTagIDs[i] = 0;
            Distances[i] = 1f;

            float angle;
            if (rayCount == 1)
                angle = 0f;
            else
                angle = -90f + (i / (float)(rayCount - 1)) * 180f;
            Vector3 localDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 direction = transform.rotation * localDir;

            debugOrigins[i] = origin;
            debugHits[i] = false;
            debugHitPoints[i] = origin + direction * detectionDistance;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                sphereRadius,
                direction,
                hitBuffer,
                detectionDistance,
                weaponLayer
            );

            if (hitCount <= 0)
                continue;

            float bestDistance = float.MaxValue;
            int bestTagID = 0;
            Vector3 bestHitPoint = default;
            bool found = false;

            for (int j = 0; j < hitCount; j++)
            {
                RaycastHit hit = hitBuffer[j];

                if (IsPartOfSelf(hit.collider))
                    continue;

                TeamMember otherTeam = hit.collider.transform.root.GetComponent<TeamMember>();
                if (otherTeam == null || ownTeam == null || otherTeam.TeamId == ownTeam.TeamId)
                    continue;

                int tagID = GetWeaponTagID(hit.collider.tag);
                if (tagID == 0)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestTagID = tagID;
                    bestHitPoint = hit.point;
                    found = true;
                }
            }

            if (found)
            {
                WeaponTagIDs[i] = bestTagID;
                Distances[i] = Mathf.Clamp01(bestDistance / detectionDistance);
                debugHits[i] = true;
                debugHitPoints[i] = bestHitPoint;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        float angleStep = rayCount > 0 ? 360f / rayCount : 45f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle;
            if (rayCount == 1)
                angle = 0f;
            else
                angle = -90f + (i / (float)(rayCount - 1)) * 180f;
            Vector3 localDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 direction = transform.rotation * localDir;
            Vector3 endPoint = origin + direction * detectionDistance;

            bool hit = false;
            Vector3 hitPoint = endPoint;

            if (Application.isPlaying && debugHits != null && i < debugHits.Length)
            {
                hit = debugHits[i];
                hitPoint = debugHitPoints[i];
            }

            Gizmos.color = hit ? Color.green : Color.blue;
            Gizmos.DrawLine(origin, hit ? hitPoint : endPoint);
            Gizmos.DrawWireSphere(hit ? hitPoint : endPoint, sphereRadius * 0.2f);
        }
    }
}