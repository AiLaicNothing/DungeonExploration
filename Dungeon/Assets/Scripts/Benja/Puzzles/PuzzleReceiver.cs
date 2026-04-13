using System.Collections.Generic;
using UnityEngine;

public class PuzzleReceiver : MonoBehaviour
{
    public enum LogicMode { AND, OR }  

    [Header("Lógica (Ambos o solo uno)")]
    public LogicMode logicMode = LogicMode.AND;

    [Header("Qué activa")]
    public List<MonoBehaviour> targets; 

    private List<IActivator> _activators = new();
    private bool _currentState = false;

    public void RegisterActivator(IActivator activator)
    {
        if (!_activators.Contains(activator))
            _activators.Add(activator);
    }

    public void Evaluate()
    {
        bool shouldBeActive = logicMode switch
        {
            LogicMode.AND => _activators.TrueForAll(a => a.IsActive),
            LogicMode.OR => _activators.Exists(a => a.IsActive),
            _ => false
        };

        if (shouldBeActive == _currentState) return; 
        _currentState = shouldBeActive;

        foreach (var target in targets)
        {
            if (target is IActivatable activatable)
            {
                if (_currentState) activatable.Activate();
                else activatable.Deactivate();
            }
        }
    }
}