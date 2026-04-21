using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    Dictionary<EnemyStateType, EnemyStates> states;

    private void Awake()
    {
        states = new Dictionary<EnemyStateType, EnemyStates>();

        //states[EnemyStateType.Iddle] = new;
        //states[EnemyStateType.Hit] = new;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
