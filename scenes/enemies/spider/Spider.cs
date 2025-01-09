using Godot;
using System;
using SwampGame.managers;
using SwampGame.src.enums;

public partial class Spider : CharacterBody2D
{
    [Export] public NodePath PlatformLayerPath;

    private PlatformLayer _platformLayer;
    private Vector2[] _currentPath;
    private int _pathIndex = 0;
    [Export] public float DetectionRange { get; set; } = 180f;
    [Export] public float BiteRange { get; set; } = 40f;
    [Export] public float Gravity { get; set; } = 500f;
    [Export] public float JumpStrength { get; set; } = 200f;
    [Export] public float MoveSpeed { get; set; } = 60f;
    [Export] public int BiteDamage { get; set; } = 15;
    [Export] public float BiteCooldown { get; set; } = 1.5f;
    [Export] public int Health { get; set; } = 50;
    [Export] public float SightCooldown { get; set; } = 1f;

    // Animation keys
    private const string ANIM_IDLE = "idle";
    private const string ANIM_WALK = "walk";
    private const string ANIM_JUMP = "jump";
    private const string ANIM_ATTACK = "attack_bite";
    private const string ANIM_RIP = "rip";

    private AnimatedSprite2D _animatedSprite;
    private EnemyState _state = EnemyState.Idle;
    private Node2D _player;

    private float _attackTimer = 0.0f;
    private float _sightTimer = 0.0f;

    private bool _hasSightedPlayer = false;
    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _platformLayer = GetNode<PlatformLayer>(PlatformLayerPath);
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        _animationPlayer.Play(ANIM_IDLE);

        _player = GetTree().Root.FindChild("Player", true, false) as Node2D;
        SpiderManager.Instance.RegisterEnemy(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleAttackCooldownTimer(delta);
        HandleSightCooldownTimer(delta);

        UpdateState();
        FacePlayer();

        if (_state == EnemyState.Walk)
        {
            UpdatePathToPlayer();
            Navigate((float)delta);
        }
        else if (_state == EnemyState.Attack)
        {
            PerformAttack();
            Velocity = new Vector2(0, Velocity.Y);
            MoveAndSlide();
        }
        else
        {
            Velocity = new Vector2(0, Velocity.Y);
            MoveAndSlide();
        }

        HandleAnimation();
    }

    private void Navigate(float delta)
    {
        if (_currentPath == null || _pathIndex >= _currentPath.Length)
            return;

        Vector2 target = _currentPath[_pathIndex];
        Vector2 direction = (target - GlobalPosition).Normalized();

        GD.Print($"Current Position: {GlobalPosition}, Target: {target}, Direction: {direction}");

        if ((direction.X < 0 && Velocity.X > 0) || (direction.X > 0 && Velocity.X < 0))
        {
            GD.Print("Spider moving in the wrong direction!");
        }

        if (target.Y < GlobalPosition.Y - 10 && IsOnFloor())
        {
            Velocity = new Vector2(direction.X * MoveSpeed, -JumpStrength);
        }
        else
        {
            Velocity = new Vector2(direction.X * MoveSpeed, Velocity.Y);
        }

        if (GlobalPosition.DistanceTo(target) < 10f)
        {
            _pathIndex++;
        }

        if (!IsOnFloor())
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * delta);
        }
        else if (Velocity.Y > 0)
        {
            Velocity = new Vector2(Velocity.X, 0);
        }

        MoveAndSlide();

        for (int i = 0; i < _currentPath.Length - 1; i++)
        {
            DebugDrawLine(_currentPath[i], _currentPath[i + 1], new Color(1, 0, 0));
        }
    }

    private void DebugDrawLine(Vector2 from, Vector2 to, Color color)
    {
        DrawLine(from, to, color, 2);
    }

    private void UpdatePathToPlayer()
    {
        if (_player == null || _platformLayer == null)
            return;

        if (_currentPath == null || _pathIndex >= _currentPath.Length - 1 || ShouldRecalculatePath())
        {
            _currentPath = _platformLayer.GetPath(GlobalPosition, _player.GlobalPosition);
            _pathIndex = 0;
        }
    }

    private bool ShouldRecalculatePath()
    {
        float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
        return distanceToPlayer > DetectionRange / 2 || _pathIndex >= _currentPath.Length;
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

        if (distance > DetectionRange && !_hasSightedPlayer)
        {
            _state = EnemyState.Idle;
            _hasSightedPlayer = false;
            return;
        }

        bool canSee = HasLineOfSight();

        if (canSee)
        {
            _hasSightedPlayer = true;
            _sightTimer = SightCooldown;
        }

        if (distance <= BiteRange && canSee)
        {
            _state = EnemyState.Attack;
        }
        else if ((distance <= DetectionRange && canSee) || (_hasSightedPlayer && _sightTimer > 0f))
        {
            _state = EnemyState.Walk;
        }
        else
        {
            if (_sightTimer <= 0f)
            {
                _state = EnemyState.Idle;
                _hasSightedPlayer = false;
            }
            else
            {
                _state = EnemyState.Walk;
            }
        }
    }

    private void OnAnimationPlayerAnimationFinished(string animationName)
    {
        if (animationName == ANIM_RIP)
        {
            QueueFree();
        }
    }

    private void FacePlayer()
    {
        if (_player == null)
            return;

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
            HealthManager.Instance.ApplyDamage(BiteDamage);
            GD.Print($"Spider attacked, dealing {BiteDamage} damage to the player.");
            _attackTimer = BiteCooldown;
        }
    }

    private bool HasLineOfSight()
    {
        if (_player == null)
            return false;

        var spaceState = GetWorld2D().DirectSpaceState;

        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, _player.GlobalPosition);
        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
            return true;

        if (!result.ContainsKey("collider"))
        {
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
        if (Health <= 0)
        {
            _state = EnemyState.Terminated;
            CollisionLayer = 0;
            CollisionMask = 0;
            SpiderManager.Instance.UnregisterEnemy(this);
        }
    }
}
