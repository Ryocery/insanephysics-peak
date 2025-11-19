using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace InsanePhysics.Features.HostileItems;

public class HostileItems : MonoBehaviour, IOnEventCallback {
    public static SFX_Instance[]? CachedBonkSounds;

    public const byte BonkEventCode = 42;
    public const byte WakeUpEventCode = 43;

    private float _timer;
    private const float CheckInterval = 0.3f;
    private const float Chance = 0.01f;
    private const float DetectionRadius = 50.0f;
    private int _layerMask;

    private void Start() {
        _layerMask = Physics.AllLayers;
        StartCoroutine(LoadBonkSounds());
    }

    private void OnEnable() {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private IEnumerator LoadBonkSounds() {
        yield return null;
        Item? coconut = Resources.FindObjectsOfTypeAll<Item>().FirstOrDefault(x => x.name.Contains("Coconut"));
        if (coconut is null) yield break;

        Bonkable? bonk = coconut.GetComponent<Bonkable>();
        if (bonk is null || bonk.bonk == null) yield break;

        CachedBonkSounds = bonk.bonk;
        Debug.Log($"[InsanePhysics] Stole audio from {coconut.name}");
    }

    public static void SendBonkEvent(Vector3 position) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.All };
        object[] content = [position];
        PhotonNetwork.RaiseEvent(BonkEventCode, content, options, SendOptions.SendReliable);
    }

    public static void SendWakeUpEvent(int viewID) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
        object[] content = [viewID];
        PhotonNetwork.RaiseEvent(WakeUpEventCode, content, options, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == BonkEventCode) {
            object[] data = (object[])photonEvent.CustomData;
            Vector3 pos = (Vector3)data[0];

            if (CachedBonkSounds != null && CachedBonkSounds.Length > 0) {
                foreach (SFX_Instance sfx in CachedBonkSounds) {
                    if (sfx != null) sfx.Play(pos);
                }
            }
        }

        else if (photonEvent.Code == WakeUpEventCode) {
            object[] data = (object[])photonEvent.CustomData;
            int viewID = (int)data[0];

            PhotonView view = PhotonView.Find(viewID);
            if (view != null && view.GetComponent<Rigidbody>() != null) {
                WakeUpItem(view.GetComponent<Rigidbody>());
            }
        }
    }

    private void Update() {
        if (!PhotonNetwork.IsMasterClient) return;
        _timer += Time.deltaTime;
        if (_timer < CheckInterval) return;
        _timer = 0f;

        List<Character> validTargets = Character.AllCharacters
            .Where(c => c is not null && !c.data.dead && !c.data.fullyPassedOut)
            .ToList();

        if (validTargets.Count > 0) {
            Character victim = validTargets[Random.Range(0, validTargets.Count)];
            AttemptHostilePhysics(victim);
        }
    }

    private void AttemptHostilePhysics(Character player) {
        Collider[] nearbyColliders = Physics.OverlapSphere(player.Center, DetectionRadius);

        foreach (Collider col in nearbyColliders) {
            if (Random.value > Chance) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb?.GetComponentInParent<Item>() is null) continue;
            if (rb.GetComponentInParent<Character>() is not null) continue;
            if (rb.GetComponent<HostileProjectile>() is not null) continue;

            Vector3 directionToPlayer = player.Center - rb.transform.position;
            float distance = directionToPlayer.magnitude;

            if (Physics.Raycast(rb.transform.position, directionToPlayer.normalized, out RaycastHit hit, distance, _layerMask)) {
                Character hitCharacter = hit.collider.GetComponentInParent<Character>();
                if (hitCharacter != player) {
                    if (hit.collider.transform.root != rb.transform.root) continue;
                }
            }

            LaunchObjectAtPlayer(rb, player);
            break;
        }
    }

    private void WakeUpItem(Rigidbody rb) {
        rb.transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;

        if (rb.GetComponent<Collider>() is not null) {
            rb.GetComponent<Collider>().enabled = true;
        }
    }

    private void LaunchObjectAtPlayer(Rigidbody rb, Character player) {
        Debug.Log($"[InsanePhysics] {rb.name} is attacking {player.characterName}!");
        
        WakeUpItem(rb);
        
        PhotonView view = rb.GetComponent<PhotonView>();
        if (view is not null) {
            SendWakeUpEvent(view.ViewID);
            if (!view.IsMine) {
                view.RequestOwnership();
            }
        }
        
        rb.gameObject.AddComponent<HostileProjectile>();

        Vector3 attackDir = (player.Center - rb.transform.position).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float launchForce = Plugin.HostileObjectPower.Value;
        rb.AddForce(attackDir * launchForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }
}