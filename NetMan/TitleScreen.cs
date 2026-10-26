namespace NetMan;

internal sealed class TitleScreen
{
  public bool IsActive { get; private set; } = true;
  public double Elapsed { get; private set; }
  public PlayerCharacter SelectedCharacter { get; private set; } = PlayerCharacter.Sam;

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
