namespace SamElla.Screens;

internal enum LeaderboardContext
{
  Title,
  Victory,
  GameOver,
}

internal sealed class LeaderboardScreen(Leaderboard board)
{
  private const string InitialCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
  private int _typingIndex;
  private Game? _observedGame;
  private bool _outcomeHandled;

  public Leaderboard Board { get; } = board;

  public bool IsActive { get; private set; }

  public bool IsEnteringInitials { get; private set; }

  public string Initials { get; private set; } = string.Empty;

  public int SelectedInitialIndex => Math.Min(_typingIndex, 2);

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
    Initials = "AAA";
    _typingIndex = 0;
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

      if (code is 0x25 or 0x27)
      {
        _typingIndex = Math.Clamp(SelectedInitialIndex + (code == 0x25 ? -1 : 1), 0, 2);
      }
      else if (code is 0x26 or 0x28)
      {
        _typingIndex = SelectedInitialIndex;
        int index = InitialCharacters.IndexOf(Initials[_typingIndex]);
        int direction = code == 0x26 ? 1 : -1;
        SetInitial(InitialCharacters[(index + direction + InitialCharacters.Length) % InitialCharacters.Length]);
      }
      else if (code == 0x08)
      {
        // Backspace edits the previous typed slot; all three slots remain selectable.
        _typingIndex = Math.Max(0, _typingIndex - 1);
        SetInitial(' ');
      }
      else if (code == 0x0D)
      {
        int rank = Board.RankFor(Score);
        if (Board.TryAdd(Initials, Score, recordedAt))
        {
          SubmittedRank = rank;
          IsEnteringInitials = false;
          return true; // The saving Enter must not also close the board.
        }
      }
      else if (_typingIndex < 3)
      {
        char? character = code switch
        {
          >= 0x41 and <= 0x5A or >= 0x30 and <= 0x39 => (char)code,
          >= 0x60 and <= 0x69 => (char)('0' + code - 0x60),
          0x20 => ' ',
          _ => null,
        };

        if (character.HasValue)
        {
          SetInitial(character.Value);
          _typingIndex++;
        }
      }
    }

    return false;
  }

  private void SetInitial(char character)
  {
    char[] initials = Initials.ToCharArray();
    initials[SelectedInitialIndex] = character;
    Initials = new string(initials);
  }
}
