using UnityEngine;

public class KomodoSpawner : MonoBehaviour
{
    [Header("Komodo Settings")]
    public GameObject komodoPrefab;
    public int initialKomodoCount = 5;
    public Vector3[] spawnAreas;
    
    [Header("References")]
    public GameUIManager gameUIManager;
    
    void Start()
    {
        // Cari GameUIManager jika tidak diassign
        if (gameUIManager == null)
        {
            gameUIManager = FindObjectOfType<GameUIManager>();
        }
        
        SpawnInitialKomodos();
        
        // Update UI setelah spawn
        if (gameUIManager != null)
        {
            // Tunggu sebentar untuk memastikan semua komodo sudah ter-spawn
            Invoke("UpdateUI", 0.1f);
        }
    }
    
    void SpawnInitialKomodos()
    {
        if (komodoPrefab == null)
        {
            Debug.LogWarning("Komodo Prefab belum diassign!");
            return;
        }
        
        for (int i = 0; i < initialKomodoCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject komodo = Instantiate(komodoPrefab, spawnPos, Quaternion.identity);
            komodo.name = "Komodo_" + (i + 1);
            
            // Pastikan tag "Komodo" ada
            if (komodo.tag != "Komodo")
            {
                komodo.tag = "Komodo";
            }
            
            Debug.Log("Spawned " + komodo.name + " at position " + spawnPos);
        }
        
        Debug.Log("Total " + initialKomodoCount + " komodo berhasil di-spawn");
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        // Jika ada area spawn yang ditentukan, gunakan itu
        if (spawnAreas != null && spawnAreas.Length > 0)
        {
            Vector3 basePos = spawnAreas[Random.Range(0, spawnAreas.Length)];
            // Tambahkan variasi posisi dalam radius 10 unit
            Vector3 randomOffset = new Vector3(
                Random.Range(-10f, 10f),
                0f,
                Random.Range(-10f, 10f)
            );
            return basePos + randomOffset;
        }
        else
        {
            // Gunakan area spawn default (dalam area yang aman dari hunter)
            return new Vector3(
                Random.Range(-20f, 20f),
                0f,
                Random.Range(-20f, 20f)
            );
        }
    }
    
    void UpdateUI()
    {
        if (gameUIManager != null)
        {
            gameUIManager.UpdateKomodoPopulation();
        }
    }
}   