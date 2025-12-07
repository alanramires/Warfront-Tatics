using UnityEngine;

public enum TrajectoryType { Straight, Parabolic } // Define como voa

[CreateAssetMenu(fileName = "NovaArma", menuName = "Warfront/Ficha de Arma")]
public class WeaponData : ScriptableObject
{
    [Header("Identidade Visual da Arma")]
    public string weaponName;       
    public Sprite icon;             // Ícone do HUD

    [Header("Visual do Projétil")]
    public Sprite projectileSprite; // <--- O desenho da bala/foguete (PNG)
    public TrajectoryType trajectory; // <--- Reta ou Parábola?
    public float projectileSpeed = 20f; // Velocidade do voo

    [Header("Estatísticas de Combate")]
    public int baseAttackPower;     
}