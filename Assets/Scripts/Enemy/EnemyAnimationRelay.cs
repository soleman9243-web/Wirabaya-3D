using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    private EnemyRanged enemyRanged;

    private void Awake()
    {
        // Mencari script EnemyRanged di Parent
        enemyRanged = GetComponentInParent<EnemyRanged>();
    }

    // Fungsi ini ditangkap oleh Animation Event di Animator (EnemyObject)
    // lalu diteruskan ke script EnemyRanged di Parent
    public void GrabArrowEvent()
    {
        if (enemyRanged != null)
        {
            enemyRanged.GrabArrowEvent();
        }
    }
}
