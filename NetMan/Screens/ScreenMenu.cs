namespace NetMan.Screens;

internal enum MenuAction
{
  None,
  Primary,
  Settings,
}

internal sealed class ScreenMenu
{
  public int SelectedIndex { get; private set; }

  public void Reset() => SelectedIndex = 0;

  public MenuAction HandleInput(IReadOnlyList<int> keyPresses, bool windowActive)
  {
    if (!windowActive)
      return MenuAction.None;

    foreach (int code in keyPresses)
    {
      if (code is 0x26 or 0x28)
        SelectedIndex = 1 - SelectedIndex;
      else if (code == 0x0D)
        return SelectedIndex == 0 ? MenuAction.Primary : MenuAction.Settings;
    }

    return MenuAction.None;
  }
}
