using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Parkour System/New Parkour Action")]
public class ParkourAction : ScriptableObject
{
    [SerializeField] string animName;
    [SerializeField] string obstacleTag; // Penambahan untuk aksi spesifik seperti Vault
    [SerializeField] float minHeight;
    [SerializeField] float maxHeight;

    [Header("Rotation Setup")]
    [SerializeField] bool rotateToObstacle;
    [SerializeField] float postActionDelay; // Jeda untuk animasi kombo (seperti Climb Up)

    [Header("Target Matching Setup")]
    [SerializeField] bool enableTargetMatching;
    [SerializeField] AvatarTarget matchBodyPart;
    [SerializeField] float matchStartTime;
    [SerializeField] float matchTargetTime;
    [SerializeField] Vector3 matchPositionWeight = new Vector3(0, 1, 0); // Default ke Y (Tinggi)

    // Properties (Getters)
    public string AnimName => animName;
    public string ObstacleTag => obstacleTag;
    public bool RotateToObstacle => rotateToObstacle;
    public float PostActionDelay => postActionDelay;
    public bool EnableTargetMatching => enableTargetMatching;
    public AvatarTarget MatchBodyPart => matchBodyPart;
    public float MatchStartTime => matchStartTime;
    public float MatchTargetTime => matchTargetTime;
    public Vector3 MatchPositionWeight => matchPositionWeight;

    public bool CheckIfPossible(ObstacleHitData hitData, Transform player)
    {
        // Cek Tag: Jika aksi ini butuh tag khusus, pastikan tag rintangan cocok
        if (!string.IsNullOrEmpty(obstacleTag) && hitData.forwardHit.transform.tag != obstacleTag)
        {
            return false;
        }

        float height = hitData.heightHit.point.y - player.position.y;

        if (height < minHeight || height > maxHeight)
        {
            return false;
        }

        return true;
    }
}