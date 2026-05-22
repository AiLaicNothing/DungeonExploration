using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructuras de datos para el sistema de guardado persistente.
/// Todas son [Serializable] para poder guardarse en JSON.
/// </summary>

// ══════════════════════════════════════════════════════════════════════
// SLOT DE GUARDADO (archivo principal)
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class SaveSlotData
{
    public string saveId;               // GUID único de esta partida
    public string saveName;             // "Partida 1", nombre personalizado
    public long createdTimestamp;       // Unix timestamp de creación
    public long lastPlayedTimestamp;    // última vez que se jugó
    public float totalPlayTimeSeconds;  // tiempo total jugado

    public WorldSaveData worldData;     // estado del mundo compartido
    public List<PlayerSaveEntry> players = new(); // datos de cada jugador que participó
}

// ══════════════════════════════════════════════════════════════════════
// MUNDO (estado global)
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class WorldSaveData
{
    public List<string> discoveredCheckpoints = new();
    public int globalUpgradePointsGenerated;
    public List<string> defeatedBosses = new();
    public List<PuzzleStateEntry> puzzleStates = new();

    // Aquí puedes añadir más adelante:
    // - puertas abiertas
    // - cofres abiertos
    // - NPCs muertos permanentemente
}

[Serializable]
public class PuzzleStateEntry
{
    public string puzzleId;
    public bool isSolved;
}

// ══════════════════════════════════════════════════════════════════════
// JUGADOR (datos individuales)
// ══════════════════════════════════════════════════════════════════════

[Serializable]
public class PlayerSaveEntry
{
    public string playerId;             // Unity Authentication PlayerId
    public string playerName;

    public PlayerStatsSnapshot stats;
    public List<string> unlockedSkills = new();

    public Vector3Serializable position;
    public string currentScene;         // "04_Gameplay"

    public string activeCheckpoint;     // checkpoint marcado como respawn
    public List<string> personalCheckpoints = new(); // checkpoints descubiertos personalmente
}
[Serializable]
public class PlayerStatsSnapshot
{
    // ── Valores actuales ─────────────────────────────
    public float currentHealth;
    public float currentMana;
    public float currentStamina;

    // ── Puntos disponibles ──────────────────────────
    public int upgradePoints;

    // ── Puntos invertidos por stat ──────────────────
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
// UTILIDADES
// ══════════════════════════════════════════════════════════════════════

/// <summary>Vector3 serializable para JSON (Unity's Vector3 no es serializable por defecto).</summary>
[Serializable]
public struct Vector3Serializable
{
    public float x, y, z;

    public Vector3Serializable(Vector3 v)
    {
        x = v.x; y = v.y; z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);

    public static implicit operator Vector3(Vector3Serializable v) => v.ToVector3();
    public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable(v);
}