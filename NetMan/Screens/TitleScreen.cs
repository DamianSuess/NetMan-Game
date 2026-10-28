using NetMan.Actors;

namespace NetMan;

internal sealed class TitleScreen
{
  private static readonly int[] KonamiCode = [0x26, 0x26, 0x28, 0x28, 0x25, 0x27, 0x25, 0x27, 0x42, 0x41];
  private readonly Queue<int> _recentKeys = new();

  public bool IsActive { get; private set; } = true;
  public bool KonamiCodeEnabled { get; private set; }
  public double Elapsed { get; private set; }
  public PlayerCharacter SelectedCharacter { get; private set; } = PlayerCharacter.Sam;

  public void HandleKeyPress(int code)
  {
    if (!IsActive)
      return;

    _recentKeys.Enqueue(code);
    if (_recentKeys.Count > KonamiCode.Length)
      _recentKeys.Dequeue();
    if (_recentKeys.SequenceEqual(KonamiCode))
      KonamiCodeEnabled = true;
  }

  public void HandleInput(double elapsed, IReadOnlyList<int> keyPresses, bool windowActive)
  {
    if (windowActive)
    {
      foreach (int code in keyPresses)
        HandleKeyPress(code);
    }

    int selectionDirection = (keyPresses.Any(code => code is 0x27 or 0x44) ? 1 : 0)
      - (keyPresses.Any(code => code is 0x25 or 0x41) ? 1 : 0);

    bool startPressed = keyPresses.Any(code => code is 0x0D or 0x20);
    Update(elapsed, startPressed, windowActive, selectionDirection);
  }

  public void Update(double elapsed, bool startPressed, bool windowActive, int selectionDirection = 0)
  {
    if (!IsActive || !windowActive)
      return;

    Elapsed += elapsed;
    if (selectionDirection < 0)
      SelectedCharacter = PlayerCharacter.Sam;
    else if (selectionDirection > 0)
      SelectedCharacter = PlayerCharacter.Ella;

    if (startPressed)
      IsActive = false;
  }
}
