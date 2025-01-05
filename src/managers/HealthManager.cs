using Godot;

namespace SwampGame.managers
{
    public partial class HealthManager : Node
    {
        public static HealthManager Instance { get; private set; }

        [Export]
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            CurrentHealth = MaxHealth;
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }
        public void ApplyDamage(int damage)
        {
            CurrentHealth -= damage;
            GD.Print($"Player took {damage} damage, remaining health: {CurrentHealth}");

            if (CurrentHealth <= 0)
            {
                GD.Print("Player has been defeated.");
                // Implement additional logic for player defeat, such as triggering animations or ending the game.
                // Reload the current scene:
                CallDeferred(nameof(ReloadLevel));
            }
        }
        private void ReloadLevel()
        {
            GetTree().ReloadCurrentScene();
        }
    }
}