using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class LevelTests
{
  [TestMethod]
  public void MissingSpawnIsRejected()
  {
    Assert.ThrowsExactly<InvalidDataException>(() => Level.Parse("test", ["..........", ".........G", "##########"]));
  }

  [TestMethod]
  public void UnknownTileIsRejected()
  {
    Assert.ThrowsExactly<InvalidDataException>(() => Level.Parse("test", ["S.........", "........ZG", "##########"]));
  }

  [TestMethod]
  public void DuplicateSpawnIsRejected()
  {
    Assert.ThrowsExactly<InvalidDataException>(() => Level.Parse("test", ["SS........", ".........G", "##########"]));
  }

  [TestMethod]
  public void ShortRowsArePadded()
  {
    Assert.AreEqual('.', Level.Parse("ragged", ["S.........G", ".", "###########"]).Tile(5, 1));
  }

  [TestMethod]
  public void BundledLevelsAreValid()
  {
    var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Levels"), "*.txt");
    Assert.IsNotEmpty(files);
    foreach (var file in files)
    {
      Assert.IsGreaterThan(0, Level.Load(file).Width, Path.GetFileName(file));
    }
  }
}
