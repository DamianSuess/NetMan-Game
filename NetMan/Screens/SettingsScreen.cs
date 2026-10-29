using NetMan.Audio;

namespace NetMan.Screens;

internal sealed class SettingsScreen(AudioSettings audio)
{
  public AudioSettings Audio { get; } = audio;
  public bool IsActive { get; private set; }
  public int SelectedIndex { get; private set; }

  public void Open()
  {
    SelectedIndex = 0;
    IsActive = true;
  }

  // The two disabled volume rows are deliberately excluded from navigation.
  public void HandleInput(IReadOnlyList<int> keyPresses, bool windowActive)
  {
    if (!IsActive || !windowActive)
      return;

    foreach (int code in keyPresses)
    {
      switch (code)
      {
        case 0x1B: // Escape
        case 0x70: // F1
        case 0x53: // S
          IsActive = false;
          return;

        case 0x26: // Up
          SelectedIndex = (SelectedIndex + 2) % 3;
          break;

        case 0x28: // Down
          SelectedIndex = (SelectedIndex + 1) % 3;
          break;

        case 0x25: // Left: off
        case 0x27: // Right: on
        case 0x0D: // Enter
        case 0x20: // Space
          if (SelectedIndex == 2)
          {
            if (code is 0x0D or 0x20)
            {
              IsActive = false;
              return;
            }

            break;
          }

          bool enabled = SelectedIndex == 0 ? Audio.MusicEnabled : Audio.SoundEffectsEnabled;
          enabled = code == 0x25 ? false : code == 0x27 ? true : !enabled;
          if (SelectedIndex == 0)
            Audio.MusicEnabled = enabled;
          else
            Audio.SoundEffectsEnabled = enabled;

          break;
      }
    }
  }
}
