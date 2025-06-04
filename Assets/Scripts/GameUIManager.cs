using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Text komodoPopulationText;
    
    private Canvas gameCanvas;
    private GameObject uiPanel;
    private int currentKomodoPopulation = 0;
    
    void Start()
    {
        CreateGameUI();
        UpdateKomodoPopulation();
    }
    
    void CreateGameUI()
    {
        // Buat Canvas otomatis
        GameObject canvasObj = new GameObject("GameCanvas");
        gameCanvas = canvasObj.AddComponent<Canvas>();
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameCanvas.sortingOrder = 100;
        
        // Tambahkan CanvasScaler
        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        
        // Tambahkan GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Buat Panel untuk UI Info
        GameObject panelObj = new GameObject("InfoPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f); // Background hitam transparan
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f); // Anchor ke pojok kiri atas
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f); // Offset dari pojok
        panelRect.sizeDelta = new Vector2(300f, 80f); // Ukuran panel
        
        uiPanel = panelObj;
        
        // Buat Text untuk Populasi Komodo
        GameObject textObj = new GameObject("KomodoPopulationText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        komodoPopulationText = textObj.AddComponent<Text>();
        komodoPopulationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        komodoPopulationText.fontSize = 18;
        komodoPopulationText.color = Color.white;
        komodoPopulationText.alignment = TextAnchor.MiddleLeft;
        komodoPopulationText.text = "Populasi Komodo: 0";
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15f, 10f); // Padding
        textRect.offsetMax = new Vector2(-15f, -10f);
        
        Debug.Log("Game UI berhasil dibuat di pojok kiri atas");
    }
    
    public void UpdateKomodoPopulation()
    {
        // Hitung jumlah GameObject dengan tag "Komodo"
        GameObject[] komodos = GameObject.FindGameObjectsWithTag("Komodo");
        currentKomodoPopulation = komodos.Length;
        
        if (komodoPopulationText != null)
        {
            komodoPopulationText.text = "Populasi Komodo: " + currentKomodoPopulation;
        }
        
        // Cek Game Over
        if (currentKomodoPopulation <= 0)
        {
            Debug.Log("GAME OVER - Populasi Komodo habis!");
            // Di sini bisa ditambahkan logic game over lainnya
        }
    }
    
    public int GetKomodoPopulation()
    {
        return currentKomodoPopulation;
    }
    
    public void DecreaseKomodoPopulation()
    {
        if (currentKomodoPopulation > 0)
        {
            // Hancurkan satu komodo secara acak
            GameObject[] komodos = GameObject.FindGameObjectsWithTag("Komodo");
            if (komodos.Length > 0)
            {
                int randomIndex = Random.Range(0, komodos.Length);
                Destroy(komodos[randomIndex]);
                Debug.Log("Satu komodo telah ditangkap hunter dan dihilangkan");
            }
        }
        
        // Update display
        UpdateKomodoPopulation();
    }
}