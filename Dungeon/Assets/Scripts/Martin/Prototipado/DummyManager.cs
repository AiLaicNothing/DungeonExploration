using UnityEngine;

public class DummyManager : MonoBehaviour
{
    [SerializeField] private GameObject dummy;      
    [SerializeField] private Transform spawnPos;   

    private GameObject dummyGame;                 

    void Start()
    {
        Spawn(); // Spawn at start
    }

    void Update()
    {
        // If dummy was destroyed, respawn it
        if (dummyGame == null)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        if (dummy != null && spawnPos != null)
        {
            dummyGame = Instantiate(dummy, spawnPos.position, spawnPos.rotation);
        }
    }
}
