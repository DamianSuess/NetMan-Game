using System.Runtime.InteropServices;

namespace NetBro;

internal static partial class Keyboard
{
  public const int Left = 0x25, Up = 0x26, Right = 0x27, Escape = 0x1B, Space = 0x20,
      A = 0x41, D = 0x44, W = 0x57, R = 0x52, P = 0x50;

  [LibraryImport("user32.dll")]
  private static partial short GetAsyncKeyState(int key);

  public static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
}
