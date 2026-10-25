namespace NetMan;

public readonly record struct BlockBreakEffect(int X, int Y, int AgeTicks = 0)
{
  public const int FrameDurationTicks = 15;

  public const int DurationTicks = FrameDurationTicks * 2;

  public int FrameIndex => AgeTicks / FrameDurationTicks;
}
