using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsTab_System : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button restartSceneButton;
    [SerializeField] private Button deleteSaveButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button resetSettingsButton;

    [Header("Confirmación borrar progreso")]
    [SerializeField] private GameObject confirmDeletePanel;
    [SerializeField] private Button confirmDeleteYesButton;
    [SerializeField] private Button confirmDeleteNoButton;

    void OnEnable()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);

        if (restartSceneButton != null) restartSceneButton.onClick.AddListener(OnRestartScene);
        if (deleteSaveButton != null) deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);
        if (resetSettingsButton != null) resetSettingsButton.onClick.AddListener(OnResetSettings);

        if (confirmDeleteYesButton != null) confirmDeleteYesButton.onClick.AddListener(OnConfirmDeleteYes);
        if (confirmDeleteNoButton != null) confirmDeleteNoButton.onClick.AddListener(OnConfirmDeleteNo);
    }

    void OnDisable()
    {
        if (restartSceneButton != null) restartSceneButton.onClick.RemoveListener(OnRestartScene);
        if (deleteSaveButton != null) deleteSaveButton.onClick.RemoveListener(OnDeleteSaveClicked);
        if (quitGameButton != null) quitGameButton.onClick.RemoveListener(OnQuitGame);
        if (resetSettingsButton != null) resetSettingsButton.onClick.RemoveListener(OnResetSettings);
        if (confirmDeleteYesButton != null) confirmDeleteYesButton.onClick.RemoveListener(OnConfirmDeleteYes);
        if (confirmDeleteNoButton != null) confirmDeleteNoButton.onClick.RemoveListener(OnConfirmDeleteNo);
    }

    private void OnRestartScene()
    {
        Time.timeScale = 1f;
        SettingsManager.Instance?.Flush();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDeleteSaveClicked()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(true);
        else DoDeleteSave();
    }

    private void OnConfirmDeleteYes()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        DoDeleteSave();
    }

    private void OnConfirmDeleteNo()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
    }

    private void DoDeleteSave()
    {
        Time.timeScale = 1f;
        if (Savesystem.Instance != null) Savesystem.Instance.ResetAndReloadScene();
        else Debug.LogWarning("[SettingsTab_System] No hay Savesystem en la escena.");
    }

    private void OnQuitGame()
    {
        SettingsManager.Instance?.Flush();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnResetSettings()
    {
        SettingsManager.Instance?.ResetToDefaults();
    }
}