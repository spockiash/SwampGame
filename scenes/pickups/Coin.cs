using Godot;
using System;
using SwampGame.managers;

public partial class Coin : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player)
		{
			GD.Print("Player collected the coin!");
			InventoryManager.Instance.AddCoins(1);
			QueueFree();
		}
		
	}
}
