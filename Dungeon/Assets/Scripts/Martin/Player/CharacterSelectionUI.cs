using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Header("Preview")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button[] characterButtons;

    [Header("Start")]
    [SerializeField] private Button startButton;

    private int currentIndex;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(StartSelection());
    }

    private IEnumerator StartSelection()
    {
        SetupButtons();

        while (PlayerSessionData.local == null)
        {
            Debug.Log("[CharacterSelectionUI] Waiting PlayerSessionData.local...");
            yield return null;
        }

        while (!PlayerSessionData.local.CharacterSelectionResolved.Value)
        {
            yield return null;
        }

        int selected = PlayerSessionData.local.SelectedCharacter.Value;

        Debug.Log($"[CharacterSelectionUI] SelectedCharacter={selected}");

        if (selected >= 0)
        {
            Debug.Log("[CharacterSelectionUI] Character already selected. Hiding UI.");
            gameObject.SetActive(false);
            yield break;
        }

        Debug.Log("[CharacterSelectionUI] No character selected yet.");

        if (panel != null)  panel.SetActive(true);

        SelectCharacter(0);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartGame);
    }

    private void SetupButtons()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }
    }

    public void SelectCharacter(int index)
    {
        CharacterData data = CharacterSelectionManager.Instance.GetCharacter(index);
        if (data == null) return;

        currentIndex = index;
        characterImage.sprite = data.portrait;
        characterNameText.text = data.characterName;
        descriptionText.text = data.description;
    }

    private void StartGame()
    {
        if (PlayerSessionData.local == null)
        {
            Debug.LogWarning("Local PlayerSessionData missing");
            return;
        }

        PlayerSessionData.local.SubmitCharacterSelectionRpc(currentIndex);

        if (panel != null)
            panel.SetActive(false);
    }
}