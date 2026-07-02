using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            Debug.Log("Player telat parry! Kena Damage brutal!");

            PlayerStatus health = other.GetComponent<PlayerStatus>();
            if (health != null)
            {

                health.TakeDamage(10);
            }

        }
    }
}