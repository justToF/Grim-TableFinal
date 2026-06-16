using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollision : MonoBehaviour
{
    private List<ParticleCollisionEvent> m_CollisionEvents = new List<ParticleCollisionEvent>();
    private ParticleSystem m_ParticleSystem;

    private void Start()
    {
        m_ParticleSystem = GetComponent<ParticleSystem>();
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = m_ParticleSystem.GetCollisionEvents(other, m_CollisionEvents);
        for (int i = 0; i < numCollisionEvents; ++i)
        {
            var col = m_CollisionEvents[i].colliderComponent;

            var fire = col.GetComponent<ExtinguishableFire>();
            if (fire != null)
                fire.Extinguish();
        }
    }
}
