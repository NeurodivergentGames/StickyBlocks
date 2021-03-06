using Godot;
using System.Collections.Generic;

public class Level : Node2D
{
    protected PlayerBlock _playerBlock;
    // protected TextureRect _background;

    [Export(PropertyHint.Enum, "EASY,MEDIUM,HARD")]
    protected string _type = "EASY";
    public string Type { get { return _type; } }

    [Export]
    private string _name;

    protected RotationStickyBlock _currentStickyBlock;
    protected Godot.Collections.Array _allStickyBlocks;

    private Label _movesLabel;
    private int _movesCounter = 0;

    private float _auxAngle = 0f;


    [Export]
    protected int _movesRequired = 99;

    [Export]
    protected int _number;
    public int Number { get { return _number; } set { _number = value; } }

    private Line2D _line;

    private LevelCamera _cameraLevel;
    private Vector2 _areaSize;
    private Vector2 _centerArea;

    private Tween _tween;


    [Export]
    protected float _intialZoom = 0f;
    [Export]
    protected float _minimumZoom = 1f;

    [Signal]
    public delegate void GameCompleted(bool owned, int movesCounter);
    [Signal]
    public delegate void RotatingTweenCompleted();
    [Signal]
    public delegate void AnimationFinished();

    [Export]
    protected float _maxZoomConstraint = 0f;

    private float[] _levelData;

    private Label _label;

    [Export]
    protected bool _zoomable = true;

    protected AnimationPlayer _animationPlayerLast;
    protected string _gotoAnim = "goto";

    private Dictionary<string, int> _bounds = new Dictionary<string, int> { { "bottom", 0 }, { "top", 0 }, { "right", 0 }, { "left", 0 } };

    public override void _Process(float _)
    {
        if (_playerBlock.Debug)
        {
            _label.Text = $"Offset: {_cameraLevel.Offset} \n PlayerPos: {_playerBlock.GlobalPosition} \n CameraPos: {_cameraLevel.GlobalPosition} \n CenterArea; {_centerArea} \n CenterCamera: {_cameraLevel.GetCameraScreenCenter()} \n Direction: {_playerBlock.GlobalPosition.DirectionTo(_centerArea)}";
        }

    }
    public override void _Ready()
    {

        _playerBlock = GetNode<PlayerBlock>("PlayerBlock");
        // _background = GetNode<TextureRect>("Background/TextureRect");
        Node2D nodes = GetNode<Node2D>("RotationStickyBlocks");
        _allStickyBlocks = nodes.GetChildren();
        _tween = _playerBlock.GetNode<Tween>("Tween");

        _cameraLevel = GetNode<LevelCamera>("Position2D/Camera2DLevel");
        _cameraLevel.Current = true;

        _animationPlayerLast = GetNode<AnimationPlayer>("ImOnLastPlayer");


        foreach (RotationStickyBlock block in _allStickyBlocks)
        {
            block.Connect(nameof(RotationStickyBlock.ImRotating), this, nameof(_on_RotationStickyBlock_ImRotating));
        }

        SetInitialCurrentBlock();
        _playerBlock.UpdateState(_currentStickyBlock);
        _playerBlock.CurrentBlock = _currentStickyBlock;
        _currentStickyBlock.UpdateState();

        _movesLabel = GetNode<Label>("HUDLayer/HUDData/HBoxContainer/Moves");
        SetLabels();

        Area2D area = GetNode<Area2D>("Area2D");
        area.Connect("body_exited", this, nameof(_on_Area2D_body_exited));

        SetCameraLimits();

        if (_playerBlock.Debug)
        {
            CanvasLayer layer = (CanvasLayer)FindNode("HUDLayer");
            _label = new Label();
            _line = new Line2D();
            AddChild(_line);
            layer.AddChild(_label);
            _line.Width = 5;
            UpdateLine(_playerBlock.GlobalPosition, _playerBlock.DashDirection);
        }

        SetLevelData();

    }


    private void SetLevelData()
    {
        _levelData = new float[_allStickyBlocks.Count];

        int i = 0;
        foreach (RotationStickyBlock block in _allStickyBlocks)
        {
            _levelData[i] = block.GlobalRotation;
            block.UpdateState();
            i++;
        }
    }
    private void BackOneMove()
    {
        _playerBlock.BackOneMove();
        _currentStickyBlock.IsCurrentBlock = false;
        RotationStickyBlock lastBlock = (RotationStickyBlock)_playerBlock.LastState["Block"];
        lastBlock.IsCurrentBlock = true;
        _currentStickyBlock = lastBlock;
        _playerBlock.CurrentBlock = lastBlock;

        _currentStickyBlock.BackOneMove();
    }
    protected void ResetLevel()
    {
        _movesCounter = 0;
        _movesLabel.Text = $"MOVES: 0";

        _currentStickyBlock.IsCurrentBlock = false;
        RotationStickyBlock initialBlock = GetNode<RotationStickyBlock>("RotationStickyBlocks/InitialBlock");
        initialBlock.IsCurrentBlock = true;
        _currentStickyBlock = initialBlock;

        _playerBlock.Reset();
        _playerBlock.CurrentBlock = _currentStickyBlock;
        _playerBlock.UpdateBlock(_currentStickyBlock);
        // _camera.Reset();
        _cameraLevel.Reset();

        int i = 0;
        foreach (RotationStickyBlock block in _allStickyBlocks)
        {
            block.IsRotated = false;
            block.GlobalRotation = _levelData[i];
            i++;
        }
    }
    private void SetLabels()
    {
        Label levelLabel = GetNode<Label>("HUDLayer/HUDData/HBoxContainer/Level");
        levelLabel.Text = $"LEVEL: {_number}";
        _movesLabel.Text = $"MOVES: 0";

        if (_name != null)
        {
            Label nameLabel = GetNode<Label>("HUDLayer/Name");
            nameLabel.Text = _name;
        }

    }
    private void SetCameraLimits()
    {
        CollisionShape2D shape = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
        RectangleShape2D rect = (RectangleShape2D)shape.Shape;

        Vector2 center = shape.Position;
        _centerArea = center;

        _bounds["top"] = (int)(center[1] - rect.Extents[1]); ;
        _bounds["bottom"] = (int)(center[1] + rect.Extents[1]);
        _bounds["left"] = (int)(center[0] - rect.Extents[0]); ;
        _bounds["right"] = (int)(center[0] + rect.Extents[0]);

        Vector2 size = new Vector2(1280,720);//GetViewport().Size;
        _areaSize = rect.Extents * 2;

        float maxLevelZoomValue = _maxZoomConstraint;

        if (_maxZoomConstraint == 0)
        {
            maxLevelZoomValue = Mathf.Max(_areaSize.y / size.y, _areaSize.x / size.x) + 0.3f;
        }

        _cameraLevel.Init(maxLevelZoomValue, _intialZoom, _zoomable, _minimumZoom);

    }

    protected void SetInitialCurrentBlock()
    {
        RotationStickyBlock initialBlock = GetNode<RotationStickyBlock>("RotationStickyBlocks/InitialBlock");
        initialBlock.IsCurrentBlock = true;
        _currentStickyBlock = initialBlock;
    }

    public void CircularMotion(float angle)
    {
        float newAngle = angle - _auxAngle;
        Vector2 pivot = _currentStickyBlock.GlobalPosition;
        _playerBlock.Rotate(newAngle);
        _playerBlock.GlobalPosition = pivot + (_playerBlock.GlobalPosition - pivot).Rotated(newAngle);
        _auxAngle = angle;

    }
    public void RotateAround(float angle)
    {
        Tween tween = _playerBlock.GetNode<Tween>("Tween");

        tween.InterpolateMethod(this, "CircularMotion", 0, angle, 0.3f);
        tween.Start();
    }
    protected void UpdatePlayerPosition()
    {
        _auxAngle = 0f;
        float angle = _currentStickyBlock.RotationAngle;

        if (_currentStickyBlock.IsRotated)
        {
            angle = -angle;
        }

        RotateAround(angle);

        _playerBlock.DashDirection = _playerBlock.DashDirection.Rotated(angle);
    }
    public virtual void ScaleModulate(bool down = true)
    {
        if (down)
        {
            _animationPlayerLast.Play("paused");
            return;
        }

        _animationPlayerLast.Play("pausedReset");

    }

    public void _on_PlayerBlock_ChangeStickyBlock(RotationStickyBlock newStickyBlock)
    {
        foreach (RotationStickyBlock block in _allStickyBlocks)
        {
            block.IsCurrentBlock = false;
        }

        newStickyBlock.IsCurrentBlock = true;
        _currentStickyBlock = newStickyBlock;
        RotationStickyBlock.CanRotate = true;

    }
    public async void _on_RotationStickyBlock_ImRotating()
    {
        if (!_playerBlock.Moving && !_playerBlock.Rotating)
        {
            UpdatePlayerPosition();
            if (_playerBlock.Debug)
            {
                await ToSignal(_tween, "tween_completed");
                UpdateLine(_playerBlock.GlobalPosition, _playerBlock.DashDirection);
            }
        }
    }
    public async void _on_PlayerBlock_ImOnLast(int movesCounter)
    {

        bool owned = false;
        if (movesCounter <= _movesRequired)
        {
            owned = true;
        }


        _animationPlayerLast.Play(_gotoAnim);
        await ToSignal(this, nameof(AnimationFinished));

        EmitSignal(nameof(GameCompleted), owned, movesCounter);
    }

    public void _on_PlayerBlock_MoveMade()
    {
        _currentStickyBlock.UpdateState();
        RotationStickyBlock.CanRotate = false;
        _currentStickyBlock.IsCurrentBlock = false;
        _movesCounter += 1;
        _movesLabel.Text = $"MOVES: {_movesCounter}";

        if (_movesCounter > 99)
        {
            _movesLabel.Text = $"MOVES: DUMB";
        }
    }
    public async void _on_HUDbuttons_PausePressed()
    {
        ScaleModulate(true);
        await ToSignal(this, nameof(AnimationFinished));
        GetTree().Paused = true;
        GetNode<Control>("ButtonsLayer/PauseMenu").Show();
    }
    public void _on_HUDbuttons_ResetPressed()
    {
        if (!_playerBlock.Moving && !_playerBlock.Blocked)
        {
            ResetLevel();
        }
    }
    public void _on_HUDbuttons_UndoPressed()
    {
        if (!_playerBlock.Moving && !_playerBlock.Blocked)
        {
            BackOneMove();
        }
    }
    public void _on_Area2D_body_exited(Node body)
    {
        if (!_currentStickyBlock.IsLast)
        {

            _playerBlock.BackToLastPosition();

            _currentStickyBlock.IsCurrentBlock = true;
            RotationStickyBlock.CanRotate = true;
        }
    }
    public void _on_PlayerBlock_AddLine(Vector2 pivot, Vector2 normal)
    {
        UpdateLine(pivot, normal);
    }

    public void _on_Camera2DLevel_OffsetZoom(Vector2 newZoom, Vector2 oldZoom)
    {
        Vector2 centerCamera = _cameraLevel.GetCameraScreenCenter();
        Vector2 direction = centerCamera.DirectionTo(_playerBlock.GlobalPosition);
        float distance = _playerBlock.GlobalPosition.DistanceTo(centerCamera);

        Vector2 oldOffset = _cameraLevel.Offset;
        Vector2 newOffset = oldOffset - direction * (newZoom - oldZoom) * distance;

        if (newZoom.x - oldZoom.x > 0)
        {
            newOffset = newOffset * (_cameraLevel.ZoomLimits.y / newZoom.x - 1);
        }

        Tween tweenCamera = _cameraLevel.TweenCamera;
        float duration = _cameraLevel.ZoomAnimationDuration;


        tweenCamera.InterpolateProperty(_cameraLevel, "offset", oldOffset, newOffset, duration,
            Tween.TransitionType.Sine, Tween.EaseType.Out);
        tweenCamera.Start();
    }

    public void UpdateLine(Vector2 pivot, Vector2 normal)
    {
        _line.ClearPoints();
        _line.AddPoint(pivot);
        _line.AddPoint(pivot + normal * 2000);
    }
    public void _on_Tween_tween_completed(object _, NodePath __)
    {
        EmitSignal(nameof(RotatingTweenCompleted));
    }
    public void _on_ImOnLastPlayer_animation_finished(string _)
    {
        EmitSignal(nameof(AnimationFinished));
    }

}






















