using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TimedPlatformChallenge : NetworkBehaviour
{
    public enum ChallengeState
    {
        Idle,
        Running,
        Completed
    }

    [Header("Challenge")]
    [SerializeField] private float challengeDuration = 30f;
    [Header("Platform Hide")]
    [SerializeField]
    private float platformHideDelay = 12f;
    [Header("Platforms")]
    [SerializeField]
    private List<ChallengePlatform> platforms =
        new();

    public float RemainingTime => _remainingTime.Value;
    public NetworkVariable<float> RemainingTimeNetwork =>
    _remainingTime;

    [SerializeField] private float platformSpawnDelay = 0.15f;

    private NetworkVariable<float> _remainingTime =
        new(0f);

    private NetworkVariable<ChallengeState> _state =
        new(ChallengeState.Idle);

    private Coroutine _timerRoutine;

    public bool IsRunning =>
        _state.Value == ChallengeState.Running;


    private IEnumerator HidePlatformsRoutine()
{
    yield return new WaitForSeconds(platformHideDelay);

    foreach (var platform in platforms)
    {
        if (platform == null)
            continue;

        platform.Hide();
    }
}
    public void StartChallenge()
    {
        if (!IsServer)
            return;

        if (_state.Value != ChallengeState.Idle)
            return;

        _state.Value = ChallengeState.Running;
        ShowUIClientRpc();

        StartCoroutine(StartChallengeRoutine());
    }
    [ClientRpc]
    private void ShowUIClientRpc()
    {
        ChallengeUI.Instance?.Show(
            "DESAFÍO DE LA FUENTE",
            this);
    }
    [ClientRpc]
    private void HideUIClientRpc()
    {
        ChallengeUI.Instance?.Hide();
    }
    private IEnumerator StartChallengeRoutine()
    {
        foreach (var platform in platforms)
        {
            if (platform == null)
                continue;

            platform.Show();

            yield return new WaitForSeconds(
                platformSpawnDelay);
        }

        _remainingTime.Value = challengeDuration;

        _timerRoutine =
            StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (_remainingTime.Value > 0f)
        {
            _remainingTime.Value -= Time.deltaTime;

            yield return null;
        }

        Timeout();
    }

    private void Timeout()
    {
        if (!IsServer)
            return;

        Debug.Log("[Challenge] Timeout");

        _state.Value = ChallengeState.Idle;

        HideUIClientRpc();

        StartCoroutine(HidePlatformsRoutine());
    }

    public void CompleteChallenge()
    {
        if (!IsServer)
            return;

        if (_state.Value != ChallengeState.Running)
            return;

        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);

        _state.Value = ChallengeState.Completed;

        HideUIClientRpc();

        StartCoroutine(HidePlatformsRoutine());

        Debug.Log("[Challenge] Completed");
    }
}