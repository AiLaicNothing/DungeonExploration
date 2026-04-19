using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public PlayerStatsSaveData playerStats;
    public PlayerPositionSaveData playerPosition;
    public List<string> activatedCheckpoints = new List<string>();
    public string activeCheckpointName; // último checkpoint activo (spawn)
    public long savedAtTicks; // DateTime.UtcNow.Ticks para mostrar "guardado hace X"
}

[Serializable]
public class PlayerStatsSaveData
{
    public int upgradePoints;
    public int totalPointsEarned;
    public List<StatSaveEntry> stats = new List<StatSaveEntry>();
}

[Serializable]
public class StatSaveEntry
{
    public string id;
    public int pointsAssigned;
    public float currentValue;
}

[Serializable]
public class PlayerPositionSaveData
{
    public float posX, posY, posZ;
    public float rotY; // solo yaw, que es lo que importa para el jugador

    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(0, rotY, 0);

    public static PlayerPositionSaveData From(Transform t)
    {
        return new PlayerPositionSaveData
        {
            posX = t.position.x,
            posY = t.position.y,
            posZ = t.position.z,
            rotY = t.rotation.eulerAngles.y
        };
    }
}