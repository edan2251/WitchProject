using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;

    public int Damage = 1;

    public Enemy enemy;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy hitEnemy = other.GetComponent<Enemy>();
            if (hitEnemy != null)
            {
                hitEnemy.TakeDamage(Damage);
                Destroy(gameObject);
            }
        }
    }
}