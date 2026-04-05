using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public PlayerStates currentState { get; private set; }

    public void Initialize(PlayerStates InitialState)
    {
        currentState = InitialState;
        currentState.OnEnter();
    }

    public void ChangeState(PlayerStates nextState)
    {
        currentState.OnExit();
        currentState = nextState;
        currentState.OnEnter(); 
    }

    public void Update()
    {
        currentState?.OnUpdate();
    }

    public void FixedUpdate()
    {
        currentState?.OnFixedUpdate();
    }
}
