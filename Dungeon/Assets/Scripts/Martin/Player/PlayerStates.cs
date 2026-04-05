using UnityEngine;

public abstract class PlayerStates 
{
    protected PlayerController player;
    public PlayerStates(PlayerController player)
    {
        this.player = player;
    }

    public virtual void OnEnter() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
