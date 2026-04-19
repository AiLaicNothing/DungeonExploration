using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Player/Stats Data")]
public class PlayerStatsData : ScriptableObject
{
    [System.Serializable]
    public class StatConfig
    {
        [Tooltip("ID único. NO cambiar después, se usa para guardado y eventos.")]
        public string id;
        public string displayName;
        public float baseValue;
        public float minValue;
        public float maxValue;
        [Tooltip("Cuánto sube/baja el valor por cada punto invertido")]
        public float valuePerPoint = 10f;
        [Tooltip("Peso de tradeoff para SUBIR. Ej: Vida=2 → subir 1 punto de vida requiere 2 de balanza.")]
        public int upgradeCost = 1;
        [Tooltip("Peso de tradeoff que se obtiene al BAJAR. Ej: Daño=1 → bajar 1 punto de daño aporta 1 a la balanza.")]
        public int downgradeValue = 1;
    }

    [Header("Stats del jugador")]
    public List<StatConfig> stats = new List<StatConfig>();

    [Header("Sistema de Puntos")]
    [Tooltip("Puntos con los que el jugador empieza la partida (normalmente 0, se ganan en checkpoints)")]
    public int startingPoints = 0;

    /// <summary>Busca un StatConfig por su ID. Devuelve null si no existe.</summary>
    public StatConfig FindStat(string id) => stats.Find(s => s.id == id);
}