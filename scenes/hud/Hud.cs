using Godot;
using System;
using SwampGame.managers;

public partial class Hud : CanvasLayer
{
	private Label _label;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_label = GetNode<Label>("HealthLabel");
		_label.Text = $"HP: {HealthManager.Instance.CurrentHealth}/{HealthManager.Instance.MaxHealth}";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Update labels
		_label.Text = $"HP: {HealthManager.Instance.CurrentHealth}/{HealthManager.Instance.MaxHealth}";
	}
}
