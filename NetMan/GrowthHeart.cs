namespace NetMan;

public sealed class GrowthHeart(double x, double y, int direction)
{
  public const double Speed = 4;
  public const int BeatCycleTicks = 36;

  public double X { get; internal set; } = x;
  public double Y { get; internal set; } = y;
  public double VelocityY { get; internal set; } = -6;
  public int Direction { get; internal set; } = direction;
  public int AgeTicks { get; internal set; }

  // A quick double beat followed by a longer rest.
  public int FrameIndex => AgeTicks % BeatCycleTicks is < 6 or >= 12 and < 18 ? 1 : 0;
}
