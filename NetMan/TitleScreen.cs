namespace NetMan;

internal sealed class TitleScreen
{
  public bool IsActive { get; private set; } = true;
  public double Elapsed { get; private set; }

  public void Update(double elapsed, bool startPressed, bool windowActive)
  {
    if (!IsActive || !windowActive)
      return;

    Elapsed += elapsed;
    if (startPressed)
      IsActive = false;
  }
}
