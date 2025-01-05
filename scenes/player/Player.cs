using Godot;
using SwampGame.managers;
using SwampGame.src.enums;

public partial class Player : CharacterBody2D
{
    [Signal]
    public delegate void HitEventHandler();

    [Export] public int Speed { get; set; } = 120;
    [Export] public float Gravity { get; set; } = 500f;
    [Export] public float JumpStrength { get; set; } = 200f;
    [Export] public float AttackCooldown { get; set; } = 0.6f;
    
    // Sprite references
    private AnimationPlayer _animationPlayer;
    private AnimatedSprite2D _animatedSprite;
    private AnimatedSprite2D _attackEffect;
    private CharacterState _state = CharacterState.Idle;
    private RayCast2D _rayCast;
    private double _attackDuration = 0.55f; // Adjust this based on your animation length
    private double _attackStateTimer = 0.0f;
    private int _lastDirection = 1; // 1 = right, -1 = left, default facing right

    // Animation constants
    private const string AnimationPlayer = "AnimationPlayer";
    private const string AnimatedSpriteNode = "AnimatedSprite2D";
    private const string AttackEffect = "AttackEffect";
    private const string RaycastNode = "RayCast2D";
    private const string WalkAnimation = "walk";
    private const string JumpAnimation = "jump";
    private const string IdleAnimation = "idle";
    private const string AttackOneAnimation = "attack_1";

    // Offsets
    private static readonly Vector2 RightOffset = new Vector2(7, -7);
    private static readonly Vector2 LeftOffset  = new Vector2(-7, -7);
    
    // Attack cooldown timer
    private float _attackTimer = 0.0f;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>(AnimationPlayer);
        _animatedSprite = GetNode<AnimatedSprite2D>(AnimatedSpriteNode);
        _attackEffect = GetNode<AnimatedSprite2D>(AttackEffect);
        _rayCast = GetNode<RayCast2D>(RaycastNode);
        HealthManager.Instance.ResetHealth();
        // Initialize the sprite’s default position/origin to the "right" offset
        // if your character is initially facing right
        _animatedSprite.Position = RightOffset;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        velocity.X = GetInputX() * Speed;
        velocity = GetInputActions(velocity);
        velocity = ApplyGravity(velocity, delta);
        
        // Handle cooldown timers
        HandleAttackCooldownTimer(delta);
        
        // Move
        Velocity = velocity;
        MoveAndSlide();

        // Update state
        UpdateState();

        // Handle animations/flipping
        HandleAnimation();
    }

    private Vector2 ApplyGravity(Vector2 velocity, double delta)
    {
        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }
        else
        {
            if (velocity.Y > 0) 
                velocity.Y = 0;
        }
        
        return velocity;
    }
    
    private int GetInputX()
    {
        int xInput = 0;
        if (Input.IsActionPressed("walk_right"))
            xInput += 1;
        if (Input.IsActionPressed("walk_left"))
            xInput -= 1;

        // Update _lastDirection only when there is movement input
        if (xInput != 0)
        {
            _lastDirection = xInput;
        }

        return xInput;
    }

    private Vector2 GetInputActions(Vector2 velocity)
    {
        // Attack 1
        if (Input.IsActionJustPressed("attack_1"))
        {
            _state = CharacterState.AttackOne;
            PerformAttack();
        }

        // Jump
        if (IsOnFloor() && Input.IsActionJustPressed("jump"))
        {
            velocity.Y = -JumpStrength;
            _state = CharacterState.Jumping;
        }
        
        return velocity;
    }

    private void UpdateState()
    {
        // If attack state is active, decrement its timer and prevent state overwriting
        if (_attackStateTimer > 0f)
        {
            _attackStateTimer -= GetProcessDeltaTime();
            if (_attackStateTimer <= 0f)
            {
                _attackStateTimer = 0f;
                _state = CharacterState.Idle; // Reset state after attack
            }
            return; // Prevent further state changes while attacking
        }

        // If we just landed, set to idle
        if (IsOnFloor() && _state == CharacterState.Jumping)
        {
            _state = CharacterState.Idle;
        }

        // Determine walking vs. idle vs. jumping
        int xInput = GetInputX();
        if (IsOnFloor() && xInput != 0)
        {
            _state = CharacterState.Walk;
        }
        else if (IsOnFloor() && xInput == 0)
        {
            _state = CharacterState.Idle;
        }
        else if (!IsOnFloor())
        {
            _state = CharacterState.Jumping;
        }
    }


    private void HandleAnimation()
    {
        // Which animation should play?
        var targetAnimation = MapStateToAnimation(_state);
        if (_animationPlayer.CurrentAnimation != targetAnimation)
        {
            _animationPlayer.Play(targetAnimation);
        }

        FlipAttackEffect();
        FlipPlayerSprite();
    }

    private void FlipAttackEffect()
    {
        // Get the current local position of the attack effect
        var fxPosition = _attackEffect.Position;

        // Flip vertically by inverting the Y position relative to a baseline
        if (_lastDirection > 0) // Facing right
        {
            _attackEffect.Position = new Vector2(Mathf.Abs(fxPosition.X), fxPosition.Y); // Normal orientation
        }
        else // Facing left
        {
            _attackEffect.Position = new Vector2(-Mathf.Abs(fxPosition.X), fxPosition.Y); // Flipped vertically
        }
    }

    private void FlipPlayerSprite()
    {
        // Determine direction based on _lastDirection
        bool isMovingLeft = (_lastDirection < 0);
        
        // Flip sprite and rotate RayCast2D only if the direction changes
        if (_animatedSprite.FlipH != isMovingLeft)
        {
            _animatedSprite.FlipH = isMovingLeft;

            // Set the sprite’s local offset
            if (isMovingLeft)
            {
                _animatedSprite.Position = LeftOffset;
                _rayCast.RotationDegrees = 180; // Rotate the RayCast to point left
            }
            else
            {
                _animatedSprite.Position = RightOffset;
                _rayCast.RotationDegrees = 0; // Rotate the RayCast to point right
            }
        }
    }

    private static string MapStateToAnimation(CharacterState state)
    {
        return state switch
        {
            CharacterState.Jumping => JumpAnimation,
            CharacterState.Walk    => WalkAnimation,
            CharacterState.AttackOne => AttackOneAnimation,
            _                       => IdleAnimation,
        };
    }
    
    private void HandleAttackCooldownTimer(double delta)
    {
        if (_attackTimer > 0f)
        {
            _attackTimer -= (float)delta;
            if (_attackTimer < 0f)
                _attackTimer = 0f;
        }
    }

    private void PerformAttack()
    {
        if (_attackTimer <= 0f && _attackStateTimer <= 0f)
        {
            _state = CharacterState.AttackOne;
            _attackStateTimer = _attackDuration; // Set attack state duration

            // Reset the attack cooldown
            _attackTimer = AttackCooldown;

            // Schedule the raycast collision and damage application after the cooldown
            var timer = GetTree().CreateTimer((float)_attackDuration);
            timer.Connect("timeout", Callable.From(ApplyDamage)); // Use Callable.From to wrap the method reference
        }
    }

    private void ApplyDamage()
    {
        // Resolve raycast collision after the attack duration
        if (_rayCast.IsColliding())
        {
            var collider = _rayCast.GetCollider();
            if (collider is Spider spider)
            {
                GD.Print($"Enemy {spider.Name} detected by RayCast2D.");
                SpiderManager.Instance.ApplyDamageToEnemy(spider, 75);
            }
        }
    }
}
