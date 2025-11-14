using UnityEngine;

public class AutoColliders : MonoBehaviour {
    void Start() {
        foreach (Transform child in GetComponentsInChildren<Transform>()) {
            var mf = child.GetComponent<MeshFilter>();
            var mr = child.GetComponent<MeshRenderer>();

            if (mf && mr && !child.GetComponent<Collider>()) {
                child.gameObject.AddComponent<MeshCollider>();
            }
        }
    }
}
