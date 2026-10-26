namespace NetMan;

internal enum GameKey
{
  Left,
  Right,
  Jump,
  Pause,
  Restart,
  Escape,
  Start,
}

// Only this interface needs a new implementation to host the game on another OS.
internal interface IGameWindow : IDisposable
{
  bool Active { get; }

  bool AnyKeyPressed { get; }

  bool Closed { get; }

  bool Down(GameKey key);

  bool Pressed(GameKey key);

  void Present(PixelBuffer frame);

  void PumpEvents();
}
