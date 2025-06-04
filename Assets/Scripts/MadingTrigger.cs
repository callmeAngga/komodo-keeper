using UnityEngine;

public class MadingTrigger : MonoBehaviour
{
    public GameObject infoPanel;

    void Start()
    {
        infoPanel.SetActive(false); // sembunyikan panel saat awal
    }

    void OnMouseDown()
    {
        infoPanel.SetActive(true);
    }
}
