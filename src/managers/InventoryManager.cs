using Godot;

namespace SwampGame.managers;

public partial class InventoryManager : Node
{
    public static InventoryManager Instance { get; private set; }
    public int Coins { get; set; }
    
    public override void _Ready()
    {
        Instance = this;
        Coins = 0;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
    }
}