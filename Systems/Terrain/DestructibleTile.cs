// DestructibleTile.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewDestructibleTile", menuName = "Tiles/Destructible Tile")]
public class DestructibleTile : Tile
{
    [Header("Destruction Settings")]
    public int maxHealth = 1;
    public Sprite[] damageSprites; // Sprites para diferentes estados de daño
}