using UnityEngine;

public class InfoPanelManager : MonoBehaviour
{
    public GameObject infoPanel;

    void Start()
    {
        infoPanel.SetActive(false);
    }

    public void ShowPanel()
    {
        infoPanel.SetActive(true);
    }

    public void HidePanel()
    {
        infoPanel.SetActive(false);
    }
}
