namespace SamElla.Screens;

internal enum LeaderboardContext
{
  Title,
  Victory,
  GameOver,
}

internal sealed class LeaderboardScreen(Leaderboard board)
{
  private Game? _observedGame;
  private bool _outcomeHandled;

  public Leaderboard Board { get; } = board;

  public bool IsActive { get; private set; }

  public bool IsEnteringInitials { get; private set; }

  public string Initials { get; private set; } = string.Empty;

  public int Score { get; private set; }

  public int SubmittedRank { get; private set; }

  public LeaderboardContext Context { get; private set; }

  public bool TryOpenFor(Game game)
  {
    if (IsActive)
      return false;

    if (!ReferenceEquals(_observedGame, game))
    {
      _observedGame = game;
      _outcomeHandled = false;
    }

    if (!game.Won && game.Lives > 0)
    {
      _outcomeHandled = false;
      return false;
    }

    if (_outcomeHandled || (!game.Won && !game.AwaitingRestart))
      return false;

    _outcomeHandled = true;
    if (!Board.Qualifies(game.Score))
      return false;

    Context = game.Won ? LeaderboardContext.Victory : LeaderboardContext.GameOver;
    Score = game.Score;
    Initials = string.Empty;
    SubmittedRank = 0;
    IsEnteringInitials = IsActive = true;

    return true;
  }

  public void OpenViewer()
  {
    Context = LeaderboardContext.Title;
    IsEnteringInitials = false;
    SubmittedRank = 0;
    IsActive = true;
  }

  // Return true only when a newly submitted entry needs to be saved.
  public bool HandleInput(IReadOnlyList<int> keyPresses, bool windowActive, DateTimeOffset recordedAt)
  {
    if (!IsActive || !windowActive)
      return false;

    foreach (int code in keyPresses)
    {
      if (!IsEnteringInitials)
      {
        if (code == 0x0D || (code == 0x1B && Context == LeaderboardContext.Title))
        {
          IsActive = false;
          return false;
        }

        continue;
      }

      if (code == 0x08 && Initials.Length > 0)
      {
        // Backspace
        Initials = Initials[..^1];
      }
      else if (code == 0x0D && Initials.Length == 3)
      {
        int rank = Board.RankFor(Score);
        if (Board.TryAdd(Initials, Score, recordedAt))
        {
          SubmittedRank = rank;
          IsEnteringInitials = false;
          return true; // The saving Enter must not also close the board.
        }
      }
      else if (Initials.Length < 3)
      {
        if (code is >= 0x41 and <= 0x5A or >= 0x30 and <= 0x39)
          Initials += (char)code;
        else if (code is >= 0x60 and <= 0x69) // Numeric keypad
          Initials += (char)('0' + code - 0x60);
      }
    }

    return false;
  }
}
