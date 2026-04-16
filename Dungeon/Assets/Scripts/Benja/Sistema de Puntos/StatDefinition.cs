using UnityEngine;

[CreateAssetMenu(fileName = "NewStat", menuName = "Stats/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string statId;       
    public string displayName;    
    public Sprite icon;

    [Header("Valores")]
    public float baseValue;  
    public float minValue; 
    public float maxValue; 

    [Header("Regeneración (0 si no regenera)")]
    public float regenPerSecond;
}