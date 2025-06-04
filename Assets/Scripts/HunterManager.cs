using UnityEngine;
using System.Collections;

public class HunterManager : MonoBehaviour
{
    [Header("Hunter Settings")]
    public GameObject hunterPrefab;
    public float respawnDelay = 5f;

    [Header("Spawn Settings")]
    [Tooltip("Area spawn hunter (opsional, jika kosong akan gunakan posisi random)")]
    public Vector3[] spawnPoints;
    [Tooltip("Radius area spawn jika tidak ada spawn points")]
    public float spawnRadius = 50f;
    [Tooltip("Posisi tengah area spawn")]
    public Transform spawnCenter;

    [Header("Targeting System")]
    [Tooltip("Apakah hunter langsung mencari komodo atau ke target point dulu")]
    public bool directHuntingMode = true;
    [Tooltip("Target points (hanya digunakan jika directHuntingMode = false)")]
    public Vector3[] targetPoints;

    private GameObject currentHunter;
    private bool canSpawn = true;
    private bool isFirstSpawn = true;

    void Start()
    {
        // Validasi setup
        ValidateSetup();

        // Mulai spawn routine
        StartCoroutine(SpawnHunterRoutine());
    }

    void ValidateSetup()
    {
        if (hunterPrefab == null)
        {
            Debug.LogError("Hunter Prefab belum di-assign di HunterManager!");
            return;
        }

        // Cek apakah ada HunterBehaviour di prefab
        HunterBehaviour behaviour = hunterPrefab.GetComponent<HunterBehaviour>();
        if (behaviour == null)
        {
            Debug.LogError("Hunter Prefab tidak memiliki komponen HunterBehaviour!");
        }

        // Cek apakah ada komodo di scene
        KomodoPatrol[] komodos = FindObjectsOfType<KomodoPatrol>();
        if (komodos.Length == 0)
        {
            Debug.LogWarning("Tidak ada komodo yang ditemukan di scene! Pastikan ada objek dengan komponen KomodoPatrol.");
        }
        else
        {
            Debug.Log("Ditemukan " + komodos.Length + " komodo di scene.");
        }

        // Setup spawn center jika tidak ada
        if (spawnCenter == null)
        {
            spawnCenter = transform;
        }

        Debug.Log("HunterManager setup complete. Direct Hunting Mode: " + directHuntingMode);
    }

    IEnumerator SpawnHunterRoutine()
    {
        // Berikan jeda singkat di awal
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (canSpawn && currentHunter == null)
            {
                SpawnHunter();
            }
            yield return new WaitForSeconds(2f); // Cek setiap 2 detik
        }
    }

    void SpawnHunter()
    {
        if (currentHunter != null) return;

        if (hunterPrefab != null)
        {
            Vector3 spawnPos = GetSpawnPosition();
            Quaternion spawnRot = GetSpawnRotation(spawnPos);

            // Spawn hunter
            currentHunter = Instantiate(hunterPrefab, spawnPos, spawnRot);

            // Beri nama unik untuk debugging
            currentHunter.name = isFirstSpawn ? "FirstHunter" : "Hunter_" + Time.time.ToString("F0");

            Debug.Log("Spawning " + currentHunter.name + " at " + spawnPos);

            // Atur behavior hunter
            var behaviour = currentHunter.GetComponent<HunterBehaviour>();
            if (behaviour != null)
            {
                StartCoroutine(DelayedStartHunting(behaviour));
            }
            else
            {
                Debug.LogError("HunterBehaviour component missing on prefab!");
            }

            isFirstSpawn = false;
        }
        else
        {
            Debug.LogWarning("Hunter Prefab belum diisi!");
        }
    }

    Vector3 GetSpawnPosition()
    {
        // Jika ada spawn points yang sudah ditentukan
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex];
        }

        // Jika tidak ada spawn points, gunakan area random di sekitar spawn center
        Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
        randomDirection.y = 0; // Keep on ground level

        Vector3 spawnPosition = spawnCenter.position + randomDirection;

        // Pastikan Y position berada di ground level
        spawnPosition.y = 0f; // Atau gunakan raycast untuk detect ground

        return spawnPosition;
    }

    Quaternion GetSpawnRotation(Vector3 spawnPos)
    {
        if (directHuntingMode)
        {
            // Jika direct hunting mode, hadapkan ke arah komodo terdekat
            KomodoPatrol[] komodos = FindObjectsOfType<KomodoPatrol>();
            if (komodos.Length > 0)
            {
                Transform nearestKomodo = null;
                float nearestDistance = float.MaxValue;

                foreach (KomodoPatrol komodo in komodos)
                {
                    float distance = Vector3.Distance(spawnPos, komodo.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestKomodo = komodo.transform;
                    }
                }

                if (nearestKomodo != null)
                {
                    Vector3 direction = (nearestKomodo.position - spawnPos).normalized;
                    return Quaternion.LookRotation(direction);
                }
            }
        }
        else if (targetPoints != null && targetPoints.Length > 0)
        {
            // Mode lama: hadapkan ke target point terdekat
            int nearestTargetIndex = GetNearestTarget(spawnPos);
            Vector3 targetPos = targetPoints[nearestTargetIndex];
            Vector3 direction = (targetPos - spawnPos).normalized;
            return Quaternion.LookRotation(direction);
        }

        // Default rotation
        return Quaternion.identity;
    }

    IEnumerator DelayedStartHunting(HunterBehaviour behaviour)
    {
        // Tunggu beberapa frame untuk memastikan semua komponen siap
        yield return null;
        yield return null;

        if (directHuntingMode)
        {
            // Mode baru: langsung berburu komodo
            behaviour.StartHuntingKomodo();
            Debug.Log("Hunter started hunting komodo directly");
        }
        else if (targetPoints != null && targetPoints.Length > 0)
        {
            // Mode lama: pergi ke target point
            int nearestTargetIndex = GetNearestTarget(currentHunter.transform.position);
            Vector3 targetPos = targetPoints[nearestTargetIndex];
            behaviour.StartWalking(targetPos);
            Debug.Log("Hunter started walking to target point: " + targetPos);
        }
        else
        {
            // Fallback: berburu komodo
            behaviour.StartHuntingKomodo();
            Debug.Log("Hunter started hunting komodo (fallback mode)");
        }
    }

    public void ReportHunter()
    {
        if (currentHunter != null)
        {
            Debug.Log("Reporting hunter: " + currentHunter.name);

            var behaviour = currentHunter.GetComponent<HunterBehaviour>();
            if (behaviour != null)
            {
                behaviour.StartFalling();
                Debug.Log("Hunter falling animation triggered");
            }

            currentHunter = null;

            // Tambahkan jeda sebelum hunter berikutnya muncul
            StartCoroutine(DelayNextSpawn());
        }
        else
        {
            Debug.LogWarning("Tidak ada hunter yang sedang aktif.");
        }
    }

    IEnumerator DelayNextSpawn()
    {
        canSpawn = false;
        Debug.Log("Menunggu " + respawnDelay + " detik sebelum spawn hunter berikutnya");
        yield return new WaitForSeconds(respawnDelay);
        canSpawn = true;
        Debug.Log("Hunter baru dapat muncul sekarang");
    }

    int GetNearestTarget(Vector3 pos)
    {
        if (targetPoints == null || targetPoints.Length == 0) return 0;

        int nearest = 0;
        float minDist = Vector3.Distance(pos, targetPoints[0]);

        for (int i = 1; i < targetPoints.Length; i++)
        {
            float dist = Vector3.Distance(pos, targetPoints[i]);
            if (dist < minDist)
            {
                nearest = i;
                minDist = dist;
            }
        }

        return nearest;
    }

    // Method untuk manual spawn hunter (untuk testing)
    [ContextMenu("Spawn Hunter Now")]
    public void ManualSpawnHunter()
    {
        if (currentHunter == null)
        {
            SpawnHunter();
        }
        else
        {
            Debug.Log("Hunter sudah ada: " + currentHunter.name);
        }
    }

    // Method untuk manual report hunter (untuk testing)
    [ContextMenu("Report Current Hunter")]
    public void ManualReportHunter()
    {
        ReportHunter();
    }

    // Gizmos untuk visualisasi area spawn
    void OnDrawGizmosSelected()
    {
        if (spawnCenter == null) return;

        // Gambar area spawn
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius);

        // Gambar spawn points jika ada
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Gizmos.DrawWireSphere(spawnPoints[i], 1f);
                // Gambar nomor spawn point
                UnityEditor.Handles.Label(spawnPoints[i] + Vector3.up * 2f, "Spawn " + i);
            }
        }

        // Gambar target points jika ada dan tidak dalam direct hunting mode
        if (!directHuntingMode && targetPoints != null && targetPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < targetPoints.Length; i++)
            {
                Gizmos.DrawWireSphere(targetPoints[i], 0.5f);
                UnityEditor.Handles.Label(targetPoints[i] + Vector3.up * 2f, "Target " + i);
            }
        }
    }
}