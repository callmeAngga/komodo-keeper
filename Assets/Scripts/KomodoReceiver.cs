using UnityEngine;

public class KomodoReceiver : MonoBehaviour
{
    public float detectionRadius = 1.5f; // seberapa dekat daging harusnya
    public string foodTag = "Food";
    private bool hasEaten = false;

    void Update()
    {
        if (hasEaten) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag(foodTag))
            {
                Debug.Log("Komodo makan via OverlapSphere!");
                Destroy(hit.gameObject); // bisa ganti dengan animasi, dll.
                hasEaten = true;
                break;
            }
        }
    }

    // Opsional: Untuk bantu visual radius di editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
