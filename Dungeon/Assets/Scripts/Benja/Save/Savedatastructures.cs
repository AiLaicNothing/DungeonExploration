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

    // Persistent player records.
    // IMPORTANT:
    // This list must keep entries even if players disconnect.
    public List<PlayerSaveEntry> players = new();
}

// ══════════════════════════════════════════════════════════════════════
// WORLD
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class WorldSaveData
{
    public List<string> discoveredCheckpoints = new();
    public int globalUpgradePointsGenerated;
    public List<string> defeatedBosses = new();
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

    public string[] equippedSkillIds;

    public string playerId;
    public string playerName;

    public int selectedCharacter = -1;

    // CHANGE:
    // These fields let the save remember the player even when offline.
    // Why:
    // The game must not depend on the player being connected at the exact
    // moment the host saves.
    public bool hasSpawnedAvatar;
    public bool isConnected;

    // CHANGE:
    // This is the last known gameplay position, updated while the player is online.
    public Vector3Serializable lastKnownPosition;

    // Kept for compatibility with your existing code.
    // Position used when restoring/spawning.
    public Vector3Serializable position;

    public string lastKnownScene;
    public long lastUpdatedTimestamp;

    public PlayerStatsSnapshot stats = new();
    public List<string> unlockedSkills = new();

    public string currentScene;
    public string activeCheckpoint;
    public List<string> personalCheckpoints = new();
}

[Serializable]
public class PlayerStatsSnapshot
{
    public float currentHealth;
    public float currentMana;
    public float currentStamina;

    public int upgradePoints;
    public int totalPointsEarned;

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