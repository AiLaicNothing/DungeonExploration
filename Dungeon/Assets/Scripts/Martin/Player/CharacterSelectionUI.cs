using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private Image characterImage;

    [SerializeField] private TMP_Text characterNameText;

    [SerializeField] private TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button[] characterButtons;

    [Header("Start")]
    [SerializeField] private Button startButton;

    private int currentIndex;

    private void Start()
    {
        SetupButtons();

        if (PlayerSessionData.local == null) return;

        int selected = PlayerSessionData.local.SelectedCharacter.Value;

        // already selected before
        if (selected >= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        SelectCharacter(0);

        startButton.onClick.AddListener(StartGame);
    }

    //==================================================
    // BUTTONS
    //==================================================

    private void SetupButtons()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;

            characterButtons[i].onClick.AddListener(() =>{SelectCharacter(index);});
        }
    }

    //==================================================
    // SELECT
    //==================================================

    public void SelectCharacter(int index)
    {
        CharacterData data = CharacterSelectionManager.Instance.GetCharacter(index);

        if (data == null) return;

        currentIndex = index;

        characterImage.sprite = data.portrait;

        characterNameText.text = data.characterName;

        descriptionText.text = data.description;
    }

    //==================================================
    // START
    //==================================================

    private void StartGame()
    {
        if (PlayerSessionData.local == null)
        {
            Debug.LogWarning("Local PlayerSessionData missing");
            return;
        }

        PlayerSessionData.local.SubmitCharacterSelectionRpc(currentIndex);

        gameObject.SetActive(false);
    }
}
