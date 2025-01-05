using Godot;
using System;
using SwampGame.managers;

public partial class Hud : CanvasLayer
{
	private Label _healthLabel;
	private Label _coinsLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_healthLabel = GetNode<Label>("HealthLabel");
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_healthLabel.Text = $"HP: {HealthManager.Instance.CurrentHealth}/{HealthManager.Instance.MaxHealth}";
		_coinsLabel.Text = $"COINS: {InventoryManager.Instance.Coins}";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Update labels
		_healthLabel.Text = $"HP: {HealthManager.Instance.CurrentHealth}/{HealthManager.Instance.MaxHealth}";
		_coinsLabel.Text = $"COINS: {InventoryManager.Instance.Coins}";
	}
}
