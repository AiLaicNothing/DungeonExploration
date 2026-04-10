using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class TeleporterPanelUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform content;
    public Transform player;

    void OnEnable()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        if (CheckpointManager.Instance == null)
        {
            Debug.LogError("CheckpointManager no existe en la escena");
            return;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab no asignado");
            return;
        }

        if (content == null)
        {
            Debug.LogError("Content no asignado");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (Checkpoint checkpoint in CheckpointManager.Instance.unlockedCheckpoints)
        {
            if (checkpoint == null)
                continue;

            GameObject button = Instantiate(buttonPrefab, content);

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
                text.text = checkpoint.checkpointName;

            Button btn = button.GetComponent<Button>();

            if (btn == null)
            {
                Debug.LogError("El prefab no tiene componente Button");
                return;
            }

            Checkpoint cp = checkpoint;

            btn.onClick.AddListener(() =>
            {
                Teleport(cp);
            });
        }
    }

    void Teleport(Checkpoint checkpoint)
    {
        player.position = checkpoint.spawnPoint.position;
        gameObject.SetActive(false);
    }
}