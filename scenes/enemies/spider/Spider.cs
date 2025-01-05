using Godot;
using System;
using SwampGame.managers;
using SwampGame.src.enums;

public partial class Spider : CharacterBody2D
{
    [Export] public float DetectionRange { get; set; } = 180f;
    [Export] public float BiteRange      { get; set; } = 40f;
    [Export] public float Gravity        { get; set; } = 500f;
    [Export] public float JumpStrength   { get; set; } = 200f;
    [Export] public float MoveSpeed      { get; set; } = 60f;
    [Export] public int   BiteDamage     { get; set; } = 15;
    [Export] public float BiteCooldown   { get; set; } = 1.5f;
    [Export]
    public int Health { get; set; } = 50;
    // How long the spider will keep chasing after losing line of sight
    [Export] public float SightCooldown  { get; set; } = 1f;

    // Animation keys
    private const string ANIM_IDLE   = "idle";
    private const string ANIM_WALK   = "walk";
    private const string ANIM_JUMP   = "jump";
    private const string ANIM_ATTACK = "attack_bite";
    private const string ANIM_RIP = "rip";

    private AnimatedSprite2D _animatedSprite;
    private EnemyState _state = EnemyState.Idle;
    private Node2D _player;

    // Attack cooldown timer
    private float _attackTimer = 0.0f;
    // Sight cooldown timer
    private float _sightTimer = 0.0f;

    // Whether the spider has seen the player at least once recently
    private bool _hasSightedPlayer = false;
    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        
        _animationPlayer.Play(ANIM_IDLE);
        
        // If your Player node is literally named "Player" somewhere under root:
        _player = GetTree().Root.FindChild("Player", true, false) as Node2D;
        SpiderManager.Instance.RegisterEnemy(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        // 1) Decrement timers
        HandleAttackCooldownTimer(delta);
        HandleSightCooldownTimer(delta);

        // 2) Decide which AI state we're in (Idle, Walk, Attack)
        UpdateState();

        // 3) Flip sprite to face the player (by position, not velocity)
        FacePlayer();

        // 4) Apply gravity + horizontal movement
        Vector2 velocity = Velocity;

        // Gravity
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else if (velocity.Y > 0)
            velocity.Y = 0;

        switch (_state)
        {
            case EnemyState.Attack:
                // In Attack range, do not move horizontally by design
                PerformAttack();
                velocity.X = 0;
                break;

            case EnemyState.Walk:
                // Move horizontally toward the player's x-position
                float diffX = _player.GlobalPosition.X - GlobalPosition.X;
                float direction = MathF.Sign(diffX); 
                velocity.X = direction * MoveSpeed;
                break;

            default:
                // Idle or other -> no horizontal movement
                velocity.X = 0;
                break;
        }

        Velocity = velocity;
        MoveAndSlide();

        // 5) Play the correct animation
        HandleAnimation();
    }

    private void UpdateState()
    {
        if (_player == null)
        {
            _state = EnemyState.Idle;
            _hasSightedPlayer = false;
            return;
        }

        if (_state == EnemyState.Terminated)
        {
            return;
        }

        float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

        // If the player is farther than DetectionRange AND 
        // the spider hasn't sighted the player recently, just Idle.
        // (No line-of-sight checks if we've never seen them yet.)
        if (distance > DetectionRange && !_hasSightedPlayer)
        {
            _state = EnemyState.Idle;
            _hasSightedPlayer = false;
            return;
        }

        // If we get here, either the player is inside detection range
        // or we've sighted the player recently (and we haven't used up the sight timer).
        bool canSee = HasLineOfSight();

        // If spider *currently* sees the player, reset the sight timer
        if (canSee)
        {
            _hasSightedPlayer = true; 
            _sightTimer = SightCooldown; 
        }

        // 1) Attack if close enough AND we currently see the player
        if (distance <= BiteRange && canSee)
        {
            _state = EnemyState.Attack;
        }
        // 2) Walk if:
        //   - Player is within detection range and spider sees the player, OR
        //   - The spider has sighted the player recently and the sight timer hasn't expired
        else if ((distance <= DetectionRange && canSee) 
                 || (_hasSightedPlayer && _sightTimer > 0f))
        {
            _state = EnemyState.Walk;
        }
        else
        {
            // If the spider hasn't seen the player recently (sightTimer = 0),
            // or the player is now far out of range, idle
            if (_sightTimer <= 0f)
            {
                _state = EnemyState.Idle;
                _hasSightedPlayer = false;
            }
            else
            {
                // Even if we can't see the player at this exact moment,
                // we haven't run out the sight timer yet, so keep walking.
                _state = EnemyState.Walk;
            }
        }
    }

    // Callback for animation_finished signal
    private void OnAnimationPlayerAnimationFinished(string animationName)
    {
        GD.Print($"Animation '{animationName}' finished.");
        
        // Add your custom logic here, e.g., transitioning states or playing another animation.
        if (animationName == ANIM_RIP)
        {
            QueueFree(); // Remove enemy from the scene after death animation.
        }
    }

    private void FacePlayer()
    {
        if (_player == null) 
            return;

        // Face right if spider.x < player.x, else face left
        bool shouldFaceRight = (GlobalPosition.X < _player.GlobalPosition.X);
        _animatedSprite.FlipH = shouldFaceRight;
    }

    private void HandleAnimation()
    {
        switch (_state)
        {
            case EnemyState.Attack:
                PlayAnimationIfNot(ANIM_ATTACK);
                break;
            case EnemyState.Walk:
                PlayAnimationIfNot(ANIM_WALK);
                break;
            case EnemyState.Terminated:
                PlayAnimationIfNot(ANIM_RIP);
                break;
            default:
                PlayAnimationIfNot(ANIM_IDLE);
                break;
        }
    }

    private void PlayAnimationIfNot(string anim)
    {
        if (_animationPlayer.CurrentAnimation != anim)
        {
            _animationPlayer.Play(anim);
        }
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

    private void HandleSightCooldownTimer(double delta)
    {
        // If we're currently not seeing the player, the timer counts down.
        // (We only reset it when 'canSee' is true in UpdateState.)
        if (_sightTimer > 0f)
        {
            _sightTimer -= (float)delta;
            if (_sightTimer < 0f)
                _sightTimer = 0f;
        }
    }

    public void PerformAttack()
    {
        if (_attackTimer <= 0f)
        {
            // Actually deal damage
            HealthManager.Instance.ApplyDamage(BiteDamage);
            GD.Print($"Spider attacked, dealing {BiteDamage} damage to the player.");

            // Reset the attack cooldown
            _attackTimer = BiteCooldown;
        }
    }

    /// <summary>
    /// Godot 4: Raycast from this spider's position to the player's position.
    /// If the first collider we hit is the player, line of sight is clear.
    /// </summary>
    private bool HasLineOfSight()
    {
        if (_player == null)
            return false;

        var spaceState = GetWorld2D().DirectSpaceState;

        // Construct ray parameters
        var query = PhysicsRayQueryParameters2D.Create(
            GlobalPosition,
            _player.GlobalPosition
        );

        // Optionally exclude this spider's own collision
        // query.Exclude = new Godot.Collections.Array { this };

        var result = spaceState.IntersectRay(query);

        // If no collision, direct line of sight
        if (result.Count == 0)
            return true;

        // If there's a collision, see if it's the player
        // In Godot 4 C#, "collider" can be an 'object' or instance ID, but often "collider" is an actual object
        // If it's "collider_id", you'd do an ID lookup.
        if (!result.ContainsKey("collider"))
        {
            // If there's no 'collider' key, check 'collider_id' or treat it as blocked
            return false;
        }

        var colliderVariant = result["collider"];
        var collider = colliderVariant.Obj as Node;

        return (collider == _player);
    }

    public void ApplyDamage(int damage)
    {
        Health -= damage;
        GD.Print($"Spider took {damage}, with {Health} HP left");
        // If health is below zero monster dies
        if (Health <= 0)
        {
            _state = EnemyState.Terminated;
            // Remove from physics layers
            CollisionLayer = 0;
            CollisionMask = 0;
            // After death
            SpiderManager.Instance.UnregisterEnemy(this);
        }
    }
}
