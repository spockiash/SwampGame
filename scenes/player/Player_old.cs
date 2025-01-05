using Godot;
using SwampGame.src.enums;

public partial class Player_old : Area2D
{
    [Signal]
    public delegate void HitEventHandler();

    [Export]
    public int Speed { get; set; } = 120;

    [Export]
    public float GravAcceleration { get; set; } = 500f; // Gravity acceleration
    [Export]
    public float JumpStrength { get; set; } = 200f; // Jump force

    private Vector2 _screenSize;
    private AnimatedSprite2D _animatedSprite;
    private float _verticalVelocity = 0f;
    private CharacterState _state = CharacterState.Idle;
    
    private const string AnimatedSpriteNode = "AnimatedSprite2D";
    private const string WalkAnimation = "walk";
    private const string JumpAnimation = "jump";
    private const string IdleAnimation = "idle";
    private const float FlipOffset = 10f;
    public override void _Ready()
    {
        _screenSize = GetViewportRect().Size;
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpriteNode);
    }

    public override void _Process(double delta)
    {
        var velocity = GetInputVelocity();
        HandleGravity(delta);
        HandleJump();
        HandleMovement(velocity, delta);
        UpdateState(velocity);
        HandleAnimation();
    }

    private Vector2 GetInputVelocity()
    {
        Vector2 velocity = Vector2.Zero;

        // Move right
        if (Input.IsActionPressed("walk_right"))
        {
            velocity.X += 1;
        }

        // Move left
        if (Input.IsActionPressed("walk_left"))
        {
            velocity.X -= 1;
        }

        return velocity;
    }

    private void HandleGravity(double delta)
    {
        // Apply gravity
        _verticalVelocity += GravAcceleration * (float)delta;

        // Simple ground collision detection (replace with actual collision logic later)
        if (Position.Y >= _screenSize.Y - 50) // Assuming ground is near the bottom
        {
            _verticalVelocity = 0;
            Position = new Vector2(Position.X, _screenSize.Y - 50); // Snap to ground
        }
    }

    private void HandleJump()
    {
        if (Input.IsActionJustPressed("jump") && Position.Y >= _screenSize.Y - 50) // Replace ground check with proper collision
        {
            _verticalVelocity = -JumpStrength;
            _state = CharacterState.Jumping; // Jumping state is set on jump input
        }
    }

    private void HandleMovement(Vector2 velocity, double delta)
    {
        if (velocity.LengthSquared() > 0)
        {
            velocity = velocity.Normalized() * Speed;
        }

        velocity.Y += _verticalVelocity; // Add vertical velocity for gravity and jumping

        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, _screenSize.X),
            Mathf.Clamp(Position.Y, 0, _screenSize.Y)
        );
    }

    private void UpdateState(Vector2 velocity)
    {
        if (_state == CharacterState.Jumping && Position.Y >= _screenSize.Y - 50)
        {
            // If landing from a jump, transition to Idle
            _state = CharacterState.Idle;
        }
        else if (Input.IsActionPressed("walk_right") || Input.IsActionPressed("walk_left"))
        {
            // Transition to Walk on movement input
            _state = CharacterState.Walk;
        }
        else if (velocity == Vector2.Zero)
        {
            // If no input and no movement, set Idle state
            _state = CharacterState.Idle;
        }
    }

    private void HandleAnimation()
    {
        // Update animation based on state
        var targetAnimation = MapStateToAnimation(_state);

        if (_animatedSprite.Animation != targetAnimation)
        {
            _animatedSprite.Animation = targetAnimation;
            _animatedSprite.Play(); // Always play the animation when changing it
        }

        // Flip sprite based on velocity
        if (_state != CharacterState.Walk) return;
        
        var isFlippingLeft = Input.IsActionPressed("walk_left");
        if (_animatedSprite.FlipH == isFlippingLeft) return;
        _animatedSprite.FlipH = isFlippingLeft;

        // Adjust position for flipping
        var offset = isFlippingLeft ? -FlipOffset : FlipOffset;
        Position += new Vector2(offset, 0);
    }


    private static string MapStateToAnimation(CharacterState state) => state switch
    {
        CharacterState.Jumping => JumpAnimation,
        CharacterState.Walk => WalkAnimation,
        _ => IdleAnimation,
    };
}
