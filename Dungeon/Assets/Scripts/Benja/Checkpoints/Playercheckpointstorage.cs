using System;
using UnityEngine;

/// <summary>
/// Almacenamiento local (PlayerPrefs) de los datos de checkpoints por jugador.
/// La key se compone con el PlayerId para que cada cuenta tenga su propio progreso.
/// </summary>
public static class PlayerCheckpointStorage
{
    [Serializable]
    public class Data
    {
        public string playerId;
        public string lastUsedCheckpoint;
        public string[] discoveredCheckpoints;
    }

    private static string GetKey(string playerId) => $"checkpoint_data_{playerId}";

    /// <summary>Guarda los datos en PlayerPrefs como JSON.</summary>
    public static void Save(string playerId, Data data)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(GetKey(playerId), json);
        PlayerPrefs.Save();
    }

    /// <summary>Carga los datos guardados, o null si no hay.</summary>
    public static Data Load(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return null;
        string key = GetKey(playerId);
        if (!PlayerPrefs.HasKey(key)) return null;

        try
        {
            return JsonUtility.FromJson<Data>(PlayerPrefs.GetString(key));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerCheckpointStorage] Error al cargar: {e.Message}");
            return null;
        }
    }

    /// <summary>Borra los datos guardados de un jugador.</summary>
    public static void Clear(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        PlayerPrefs.DeleteKey(GetKey(playerId));
        PlayerPrefs.Save();
    }
}