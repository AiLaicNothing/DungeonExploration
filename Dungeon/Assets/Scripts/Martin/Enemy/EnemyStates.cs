using UnityEngine;

public abstract class EnemyStates 
{
    protected EnemyController enemy;
    public EnemyStates(EnemyController enemy)
    {
        this.enemy = enemy;
    }

    public virtual void OnEnter() { }
    public virtual void OnFixedUpdate() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
}
