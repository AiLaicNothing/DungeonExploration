using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructuras serializables para Save System multiplayer.
/// </summary>

// ══════════════════════════════════════════════════════════════════════
// SAVE SLOT
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class SaveSlotData
{
    public string saveId;
    public string saveName;

    public long createdTimestamp;
    public long lastPlayedTimestamp;

    public float totalPlayTimeSeconds;

    public WorldSaveData worldData = new();

    public List<PlayerSaveEntry> players = new();
}

// ══════════════════════════════════════════════════════════════════════
// WORLD
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class WorldSaveData
{
    // Checkpoints desbloqueados globalmente
    public List<string> discoveredCheckpoints = new();

    // Puntos generados globalmente
    public int globalUpgradePointsGenerated;

    // Bosses derrotados
    public List<string> defeatedBosses = new();

    // Estados de puzzles
    public List<PuzzleStateEntry> puzzleStates = new();
}

[Serializable]
public class PuzzleStateEntry
{
    public string puzzleId;

    public bool isSolved;
}

// ══════════════════════════════════════════════════════════════════════
// PLAYER
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class PlayerSaveEntry
{
    // Unity Authentication PlayerId
    public string playerId;

    public string playerName;

    public int selectedCharacter = -1;

    // Stats
    public PlayerStatsSnapshot stats = new();

    // Skills desbloqueadas
    public List<string> unlockedSkills = new();

    // Posición exacta de logout/reconexión
    public Vector3Serializable position;

    // Escena actual
    public string currentScene;

    // Checkpoint activo para respawn
    public string activeCheckpoint;

    // Checkpoints descubiertos personalmente
    public List<string> personalCheckpoints = new();
}

[Serializable]
public class PlayerStatsSnapshot
{
    // ════════════════════════════════════════════════════
    // CURRENT VALUES
    // ════════════════════════════════════════════════════

    public float currentHealth;
    public float currentMana;
    public float currentStamina;

    // ════════════════════════════════════════════════════
    // AVAILABLE POINTS
    // ════════════════════════════════════════════════════

    public int upgradePoints;

    // Total histórico ganado
    // IMPORTANTE para evitar duplicaciones
    public int totalPointsEarned;

    // ════════════════════════════════════════════════════
    // ASSIGNED POINTS
    // ════════════════════════════════════════════════════

    public int healthPoints;
    public int manaPoints;
    public int staminaPoints;

    public int physicalDamagePoints;
    public int magicalDamagePoints;

    public int healthRegenPoints;
    public int manaRegenPoints;
    public int staminaRegenPoints;
}

// ══════════════════════════════════════════════════════════════════════
// VECTOR3 SERIALIZABLE
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public struct Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static implicit operator Vector3(Vector3Serializable v)
    {
        return v.ToVector3();
    }

    public static implicit operator Vector3Serializable(Vector3 v)
    {
        return new Vector3Serializable(v);
    }
}