using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGun : MonoBehaviour
{
    [Header("Gun Stats")]
    public float damage;
    public float reloadTime;
    public float range;
    
    [Header("General Gun Information")]
    public Transform gunEnd;
    private TrailRenderer BulletTrail;
    private float nextFire;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // add to this the state flip to tell it. 
        // you should call this and do the timer within enemy. 
        if (Time.time > nextFire){
            // this should also be removed since it's a timer
            nextFire = reloadTime + Time.time;
            Shoot();
        }
    }

    /*
    Assumes you already handed me a raycast hit that connects a enemy to player
    I will also assume that you have LOS from this

    Major potential bug: 

    If raycast hit does not update its values over time, then a major bug will 
    be the fact that it will do a phantom hit even if you move out of the way. 

    If this is the case, there are 2 options, both of which do the same thing. 

    1) Calculate another raycast using the previous hit point of the other raycast. 
    This basically tries to check if the player is still there. However, this has
    issues with if the player moves out of the distance of the previous hit point, 
    so do this carefully. 

    2) Give the original parameters for the raycast that you determined the raycast with, 
    extract those, and pass into the shoot function and propagate down to do the recalculation 
    at the end. 

    TLDR: you might need to do a recalculation on the raycast depending on how it works. 
    */
    public void Shoot(RaycastHit hit)
    {
        // this is where it shoots from, you should change if you want diff logic. 
        Vector3 rayOrigin = transform.position;
        
        TrailRenderer trail = Instantiate(BulletTrail, transform.position, Quaternion.identity);

        StartCoroutine(WarnPlayer(trail, hit.point, hit.normal, true));
    }

    // This just shows a trail to warn the player, and then will
    // shoot the bullet. 
    private IEnumerator WarnPlayer(TrailRenderer Trail, Vector3 HitPoint, Vector3 HitNormal, bool MadeImpact)
    {
        Vector3 startPosition = Trail.transform.position;
        float distance = Vector3.Distance(Trail.transform.position, HitPoint);
        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, HitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= 200f * Time.deltaTime;

            yield return null;
        }
        Trail.transform.position = HitPoint;

        // trail.time for now, should make a new trail for this. 
        // should theoretically wait for the time it takes for it to destroy, if it doesn't
        // add a timer under that does 

        // this basically means that we will wait for the trail to disappear completely 
        // to shoot the player with damage. 
        yield return Destroy(Trail.gameObject, Trail.time);

        StartCoroutine(DealDamage(hit));
    }

    // deals damage after giving warning. 
    private IEnumerator DealDamage(RaycastHit hit)
    {
        PlayerInfo health = hit.collider.GetComponent<PlayerInfo>();
        
        // I think this holds the hit from the beginning. 
        // Consider recalculating the raycast at this moment, which means you need to pass
        // in values from the beginning of the shot. 
        if (health != null)
        {
            health.TakeDamage(gunDamage);
        }
    }
}