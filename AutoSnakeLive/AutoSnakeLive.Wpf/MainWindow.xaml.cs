using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AutoSnakeLive.Bot;
using AutoSnakeLive.Core;
using AutoSnakeLive.Shared;

namespace AutoSnakeLive.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly Brush MapGridBrush = new SolidColorBrush(Color.FromRgb(30, 42, 35));
    private static readonly Brush ObstacleBrush = new LinearGradientBrush(
        Color.FromRgb(120, 125, 121),
        Color.FromRgb(70, 74, 72),
        45);

    private static readonly TimeSpan DirectionButtonPressDuration = TimeSpan.FromMilliseconds(140);
    private static readonly BitmapImage FoodImage = LoadImage("FoodApple.png");
    private static readonly BitmapImage SnakeBodyImage = LoadImage("SnakeBody.png");
    private static readonly BitmapImage SnakeHeadRightImage = LoadImage("SnakeHeadRight.png");
    private static readonly BitmapImage KeyIdleWImage = LoadImage("KeyIdleW.png");
    private static readonly BitmapImage KeyIdleAImage = LoadImage("KeyIdleA.png");
    private static readonly BitmapImage KeyIdleSImage = LoadImage("KeyIdleS.png");
    private static readonly BitmapImage KeyIdleDImage = LoadImage("KeyIdleD.png");
    private static readonly BitmapImage KeyPressedWImage = LoadImage("KeyPressedW.png");
    private static readonly BitmapImage KeyPressedAImage = LoadImage("KeyPressedA.png");
    private static readonly BitmapImage KeyPressedSImage = LoadImage("KeyPressedS.png");
    private static readonly BitmapImage KeyPressedDImage = LoadImage("KeyPressedD.png");

    private GameManager _gameManager = null!;
    private GameConfig _config = null!;
    private readonly Stopwatch _stopwatch = new();
    private int _score;
    private Direction _activeDirection = Direction.Right;
    private Direction? _lastBotDirection;
    private Direction? _pressedDirection;
    private DateTime _pressedDirectionUntil = DateTime.MinValue;
    private List<(int Row, int Col)> _previousSnakeSegments = new();
    private List<(int Row, int Col)> _currentSnakeSegments = new();
    private TimeSpan _lastGameUpdateAt = TimeSpan.Zero;
    private TimeSpan _nextGameUpdateAt = TimeSpan.Zero;
    private TimeSpan _gameUpdateDuration = TimeSpan.FromMilliseconds(300);
    private TimeSpan _lastRenderedAt = TimeSpan.Zero;
    private TimeSpan _targetFrameInterval = TimeSpan.FromSeconds(1.0 / 90);
    private bool _isRendering;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        if (_gameManager is not null)
        {
            _gameManager.AppleEaten -= OnAppleEaten;
        }
    }

    private static void OnAppleEaten()
    {
        SystemSounds.Asterisk.Play();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Create game configuration and manager
        _config = new GameConfig();
        _targetFrameInterval = TimeSpan.FromSeconds(1.0 / _config.TargetFrameRate);
        var bot = new SnakeBot(new PathFinder(), new SafetyChecker());
        _gameManager = new GameManager(_config, state =>
        {
            var nextDirection = bot.GetNextDirection(state);
            if (_lastBotDirection != nextDirection)
            {
                _pressedDirection = nextDirection;
                _pressedDirectionUntil = DateTime.UtcNow + DirectionButtonPressDuration;
            }

            _lastBotDirection = nextDirection;
            _activeDirection = nextDirection;
            return _activeDirection;
        });
        _gameManager.AppleEaten += OnAppleEaten;
        _score = 0;
        _currentSnakeSegments = _gameManager.State.Snake.Segments.ToList();
        _previousSnakeSegments = _currentSnakeSegments.ToList();
        UpdateDirectionButtons();
        _stopwatch.Restart();
        _lastRenderedAt = TimeSpan.Zero;
        _lastGameUpdateAt = TimeSpan.Zero;
        _nextGameUpdateAt = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_isRendering)
        {
            return;
        }

        var elapsed = _stopwatch.Elapsed;
        if (elapsed - _lastRenderedAt < _targetFrameInterval)
        {
            return;
        }

        _isRendering = true;
        try
        {
            TickGameIfNeeded(elapsed);

            var currentLength = _gameManager.State.Snake.Segments.Count;
            _score = Math.Max(0, currentLength - 3);
            Render(GetMovementProgress(elapsed));
            UpdateDirectionButtons();
            RoundText.Text = $"Round {_gameManager.CurrentRound}";
            ScoreText.Text = _gameManager.HasWon
                ? $"You Win! Score: {_score}"
                : _gameManager.GameOver
                    ? $"Game Over! Score: {_score}"
                    : $"Score: {_score} / {_config.WinningScore}";
            _lastRenderedAt = elapsed;
        }
        finally
        {
            _isRendering = false;
        }
    }

    private void TickGameIfNeeded(TimeSpan elapsed)
    {
        if (_gameManager.GameOver || elapsed < _nextGameUpdateAt)
        {
            return;
        }

        _previousSnakeSegments = _gameManager.State.Snake.Segments.ToList();
        _lastGameUpdateAt = elapsed;

        var gameDelay = _gameManager.Update();
        _currentSnakeSegments = _gameManager.State.Snake.Segments.ToList();
        _gameUpdateDuration = TimeSpan.FromMilliseconds(gameDelay);
        _nextGameUpdateAt = _lastGameUpdateAt + _gameUpdateDuration;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        PauseRenderLoop();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        ResumeRenderLoop();
    }

    private void PauseRenderLoop()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }
    }

    private void ResumeRenderLoop()
    {
        if (!_stopwatch.IsRunning && _gameManager is not null)
        {
            _stopwatch.Start();
            _lastRenderedAt = TimeSpan.Zero;
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            PauseRenderLoop();
        }
        else
        {
            ResumeRenderLoop();
        }
    }

    private double GetMovementProgress(TimeSpan elapsed)
    {
        if (_gameUpdateDuration <= TimeSpan.Zero)
        {
            return 1;
        }

        var progress = (elapsed - _lastGameUpdateAt).TotalMilliseconds / _gameUpdateDuration.TotalMilliseconds;
        return Math.Clamp(progress, 0, 1);
    }

    private void UpdateDirectionButtons()
    {
        var activePress = DateTime.UtcNow < _pressedDirectionUntil ? _pressedDirection : null;
        if (activePress is null)
        {
            _pressedDirection = null;
        }

        SetKeyImage(KeyW, activePress == Direction.Up ? KeyPressedWImage : KeyIdleWImage);
        SetKeyImage(KeyA, activePress == Direction.Left ? KeyPressedAImage : KeyIdleAImage);
        SetKeyImage(KeyS, activePress == Direction.Down ? KeyPressedSImage : KeyIdleSImage);
        SetKeyImage(KeyD, activePress == Direction.Right ? KeyPressedDImage : KeyIdleDImage);
    }

    private static void SetKeyImage(Button key, ImageSource source)
    {
        if (key.Content is Image image)
        {
            if (!ReferenceEquals(image.Source, source))
            {
                image.Source = source;
            }

            return;
        }

        key.Content = new Image
        {
            Source = source,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true
        };
    }

    /// <summary>
    /// Renders the current game state onto the canvas.  
    /// Uses simple shapes to represent the snake, food and obstacles.
    /// </summary>
    private void Render(double movementProgress)
    {
        GameCanvas.Children.Clear();
        var state = _gameManager.State;
        var map = state.Map;
        // Determine the size of each cell based on the canvas dimensions
        var cellWidth = GameCanvas.Width / map.Width;
        var cellHeight = GameCanvas.Height / map.Height;
        DrawMapGrid(map.Width, map.Height, cellWidth, cellHeight);

        // Draw obstacles
        foreach (var obstacle in state.Obstacles)
        {
            var rect = new Rectangle
            {
                Width = cellWidth * 0.92,
                Height = cellHeight * 0.92,
                RadiusX = 3,
                RadiusY = 3,
                Fill = ObstacleBrush,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, obstacle.Col * cellWidth + cellWidth * 0.04);
            Canvas.SetTop(rect, obstacle.Row * cellHeight + cellHeight * 0.04);
            GameCanvas.Children.Add(rect);
        }

        // Draw food
        foreach (var foodItem in state.Foods)
        {
            var food = new Image
            {
                Source = FoodImage,
                Width = cellWidth * 1.12,
                Height = cellHeight * 1.18,
                Stretch = Stretch.Uniform
            };
            Canvas.SetLeft(food, foodItem.Col * cellWidth - cellWidth * 0.06);
            Canvas.SetTop(food, foodItem.Row * cellHeight - cellHeight * 0.12);
            GameCanvas.Children.Add(food);
        }

        // Draw snake
        var snakeSegments = GetInterpolatedSnakeSegments(movementProgress);
        for (var i = snakeSegments.Count - 1; i >= 0; i--)
        {
            var segment = snakeSegments[i];
            var isHead = i == 0;
            var image = new Image
            {
                Source = isHead ? SnakeHeadRightImage : SnakeBodyImage,
                Width = isHead ? cellWidth * 1.45 : cellWidth * 1.04,
                Height = isHead ? cellHeight * 1.1 : cellHeight,
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = isHead ? new RotateTransform(GetHeadRotation(_activeDirection)) : Transform.Identity
            };
            Canvas.SetLeft(image, segment.Col * cellWidth + (isHead ? -cellWidth * 0.22 : -cellWidth * 0.02));
            Canvas.SetTop(image, segment.Row * cellHeight + (isHead ? -cellHeight * 0.05 : 0));
            GameCanvas.Children.Add(image);
        }
    }

    private List<(double Row, double Col)> GetInterpolatedSnakeSegments(double progress)
    {
        if (_currentSnakeSegments.Count == 0)
        {
            return new List<(double Row, double Col)>();
        }

        var interpolated = new List<(double Row, double Col)>(_currentSnakeSegments.Count);
        for (var i = 0; i < _currentSnakeSegments.Count; i++)
        {
            var current = _currentSnakeSegments[i];
            var previous = i < _previousSnakeSegments.Count ? _previousSnakeSegments[i] : current;
            interpolated.Add((
                Lerp(previous.Row, current.Row, progress),
                Lerp(previous.Col, current.Col, progress)));
        }

        return interpolated;
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + (end - start) * progress;
    }

    private void DrawMapGrid(int width, int height, double cellWidth, double cellHeight)
    {
        for (var col = 0; col <= width; col += 5)
        {
            var x = col * cellWidth;
            GameCanvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = GameCanvas.Height,
                Stroke = MapGridBrush,
                StrokeThickness = 1,
                Opacity = 0.18
            });
        }

        for (var row = 0; row <= height; row += 5)
        {
            var y = row * cellHeight;
            GameCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = GameCanvas.Width,
                Y2 = y,
                Stroke = MapGridBrush,
                StrokeThickness = 1,
                Opacity = 0.18
            });
        }
    }

    private static double GetHeadRotation(Direction direction)
    {
        return direction switch
        {
            Direction.Up => -90,
            Direction.Down => 90,
            Direction.Left => 180,
            _ => 0
        };
    }

    private static BitmapImage LoadImage(string fileName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/UI/{fileName}", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
