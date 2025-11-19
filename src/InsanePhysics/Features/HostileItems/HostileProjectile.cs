using UnityEngine;

namespace InsanePhysics.Features.HostileItems;

public class HostileProjectile : MonoBehaviour {
    private bool _hasHit;
    private float _creationTime;

    private const float RagdollTime = 1f;
    private const float BonkForce = 40f;
    private const float BonkRange = 6f;
    private const float MinBonkVelocity = 5f;
    private const float ArmingDelay = 0.05f;

    private void Start() {
        _creationTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision) {
        if (_hasHit) return;
        if (Time.time - _creationTime < ArmingDelay) return;

        if (collision.relativeVelocity.magnitude < MinBonkVelocity) {
            Destroy(this);
            return;
        }

        Character victim = collision.gameObject.GetComponentInParent<Character>();

        if (victim is not null) {
            if (victim.data.dead || victim.data.fullyPassedOut) return;

            Debug.Log($"[InsanePhysics] BONK! {name} knocked out {victim.characterName}");
            victim.Fall(RagdollTime);
            ContactPoint contact = collision.contacts[0];
            victim.AddForceAtPosition(-collision.relativeVelocity.normalized * BonkForce, contact.point, BonkRange);

            HostileItems.SendBonkEvent(transform.position);

            _hasHit = true;
            Destroy(this);
        } else {
            if (collision.relativeVelocity.magnitude > 10f) {
                Destroy(this);
            }
        }
    }
}