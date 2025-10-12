using UnityEngine;
using System.Collections; 

public class Bomb : MonoBehaviour
{
    public int explosionDamage = 3;        
    public float detonationDelay = 0.3f;      // 땅에 닿은 후 터지기까지의 시간
    public float explosionRadius = 5f;      // 폭발의 범위
    public GameObject explosionPrefab;      

    
    private bool fuseStarted = false; // 퓨즈가 이미 시작되었는지

    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !fuseStarted)
        {
            fuseStarted = true;
            StartCoroutine(DetonateAfterDelay(detonationDelay));
        }

    }

    IEnumerator DetonateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Explode();
    }

    void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            Enemy hitEnemy = hit.GetComponent<Enemy>();

            if (hitEnemy != null)
            {
                hitEnemy.TakeDamage(explosionDamage);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}