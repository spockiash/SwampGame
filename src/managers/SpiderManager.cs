using Godot;
using System.Collections.Generic;

public partial class SpiderManager : Node
{
    public static SpiderManager Instance { get; private set; }
    private List<Spider> enemies = new List<Spider>();

    public override void _Ready()
    {
        Instance = this;
    }

    public void RegisterEnemy(Spider enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
            GD.Print($"Enemy {enemy.Name} registered.");
        }
    }

    public void UnregisterEnemy(Spider enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            GD.Print($"Enemy {enemy.Name} unregistered.");
        }
    }

    public void ApplyDamageToEnemy(Spider enemy, int damage)
    {
        if (enemies.Contains(enemy))
        {
            enemy.ApplyDamage(damage);
        }
        else
        {
            GD.PrintErr("Attempted to damage an unregistered enemy.");
        }
    }
}