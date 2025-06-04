using UnityEngine;
using System.Collections;

public class HunterManager : MonoBehaviour
{
    [Header("Hunter Settings")]
    public GameObject hunterPrefab;
    public Vector3[] targetPoints;
    public float respawnDelay = 5f;
    public float hunterTimeout = 30f; // Waktu sebelum hunter menangkap komodo
    
    [Header("References")]
    public GameUIManager gameUIManager;

    private GameObject currentHunter;
    private bool canSpawn = true;
    private bool isFirstSpawn = true;
    private Coroutine hunterTimeoutCoroutine;

    void Start()
    {
        // Cari GameUIManager jika tidak diassign
        if (gameUIManager == null)
        {
            gameUIManager = FindObjectOfType<GameUIManager>();
            if (gameUIManager == null)
            {
                Debug.LogError("GameUIManager tidak ditemukan! Pastikan ada GameUIManager di scene.");
            }
        }
        
        // Gunakan coroutine untuk spawn hunter
        StartCoroutine(SpawnHunterRoutine());
    }

    IEnumerator SpawnHunterRoutine()
    {
        // Berikan jeda singkat di awal untuk memastikan scene sudah terinisialisasi dengan baik
        yield return new WaitForSeconds(1f);
        
        while (true)
        {
            if (canSpawn && currentHunter == null)
            {
                SpawnHunter();
            }
            yield return new WaitForSeconds(5f);
        }
    }

    void SpawnHunter()
    {
        if (currentHunter != null) return;

        if (hunterPrefab != null && targetPoints.Length > 0)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            int nearestTargetIndex = GetNearestTarget(spawnPos);
            Vector3 targetPos = targetPoints[nearestTargetIndex];

            // Buat hunter menghadap ke target
            Quaternion spawnRot = Quaternion.LookRotation((targetPos - spawnPos).normalized);
            
            // Spawn hunter
            currentHunter = Instantiate(hunterPrefab, spawnPos, spawnRot);
            
            // Beri nama unik untuk debugging
            currentHunter.name = isFirstSpawn ? "FirstHunter" : "Hunter_" + Time.time.ToString("F0");

            // Log untuk debug
            Debug.Log("Spawning " + currentHunter.name + " at " + spawnPos + ", targeting " + targetPos);

            // Atur behavior hunter
            var behaviour = currentHunter.GetComponent<HunterBehaviour>();
            if (behaviour != null)
            {
                // Berikan jeda singkat untuk memastikan komponen terinisialisasi
                StartCoroutine(DelayedStartWalking(behaviour, targetPos));
            }
            else
            {
                Debug.LogError("HunterBehaviour component missing on prefab!");
            }
            
            // Mulai timer untuk hunter timeout
            if (hunterTimeoutCoroutine != null)
            {
                StopCoroutine(hunterTimeoutCoroutine);
            }
            hunterTimeoutCoroutine = StartCoroutine(HunterTimeoutSequence());
            
            // Update status first spawn
            isFirstSpawn = false;
        }
        else
        {
            Debug.LogWarning("Prefab atau targetPoints belum diisi!");
        }
    }
    
    // Coroutine untuk memulai berjalan dengan sedikit jeda
    IEnumerator DelayedStartWalking(HunterBehaviour behaviour, Vector3 targetPos)
    {
        // Tunggu dua frame untuk memastikan semua komponen siap
        yield return null;
        yield return null;
        
        // Set hunter untuk berjalan ke target
        behaviour.StartWalking(targetPos);
        Debug.Log("Hunter started walking to " + targetPos);
    }
    
    // Coroutine untuk menangani timeout hunter
    IEnumerator HunterTimeoutSequence()
    {
        Debug.Log("Hunter timeout timer dimulai: " + hunterTimeout + " detik");
        yield return new WaitForSeconds(hunterTimeout);
        
        // Jika hunter masih ada setelah timeout (belum dilaporkan)
        if (currentHunter != null)
        {
            Debug.Log("Hunter timeout! Menangkap komodo dan menghilang.");
            
            // Kurangi populasi komodo
            if (gameUIManager != null)
            {
                gameUIManager.DecreaseKomodoPopulation();
            }
            
            // Hancurkan hunter tanpa animasi falling
            Destroy(currentHunter);
            currentHunter = null;
            
            // Mulai delay untuk spawn berikutnya
            StartCoroutine(DelayNextSpawn());
        }
    }

    public void ReportHunter()
    {
        if (currentHunter != null)
        {
            Debug.Log("Reporting hunter: " + currentHunter.name);
            
            // Stop timeout coroutine karena hunter sudah dilaporkan
            if (hunterTimeoutCoroutine != null)
            {
                StopCoroutine(hunterTimeoutCoroutine);
                hunterTimeoutCoroutine = null;
            }
            
            var behaviour = currentHunter.GetComponent<HunterBehaviour>();
            if (behaviour != null)
            {
                behaviour.StartFalling();
                Debug.Log("Hunter falling animation triggered");
            }

            currentHunter = null; // kosongkan hunter
            
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

    Vector3 GetRandomSpawnPosition()
    {
        int area = Random.Range(0, 4);

        switch (area)
        {
            case 0: return new Vector3(Random.Range(-74f, -27f), 0f, -24f);
            case 1: return new Vector3(-74f, 0f, Random.Range(-24f, 74f));
            case 2: return new Vector3(Random.Range(-74f, 24f), 0f, 74f);
            case 3: return new Vector3(24f, 0f, Random.Range(29f, 74f));
            default: return Vector3.zero;
        }
    }

    int GetNearestTarget(Vector3 pos)
    {
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
}