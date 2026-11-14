// AI-GEN AI-013: tests for Christopher's GroupTable, written by Claude. Dev-only, never part of the game.
using Physics;

namespace PhysicsTests;

public class GroupTableTests
{
    static readonly string[] Names = { "Player", "Box", "Enemy", "Water" };

    [Fact]
    public void NamesGetNumbersInOrder()
    {
        var t = new GroupTable(Names);
        Assert.Equal(4, t.Count);
        for (int i = 0; i < Names.Length; i++)
            Assert.Equal(new CollisionGroup(i), t.GetGroup(Names[i]));
    }

    [Fact]
    public void EveryPairStartsAsIgnore()
    {
        var t = new GroupTable(Names);
        for (int a = 0; a < Names.Length; a++)
            for (int b = 0; b < Names.Length; b++)
                Assert.Equal(CollisionResponse.Ignore, t.Get(new CollisionGroup(a), new CollisionGroup(b)));
    }

    [Fact]
    public void SetFillsBothHalves()
    {
        var t = new GroupTable(Names);
        var player = t.GetGroup("Player");
        var water = t.GetGroup("Water");
        t.Set(player, water, CollisionResponse.Overlap);
        Assert.Equal(CollisionResponse.Overlap, t.Get(player, water));
        Assert.Equal(CollisionResponse.Overlap, t.Get(water, player));
    }

    [Fact]
    public void SetOnlyChangesThatPair()
    {
        var t = new GroupTable(Names);
        t.Set(t.GetGroup("Player"), t.GetGroup("Box"), CollisionResponse.Solid);
        int changed = 0;
        for (int a = 0; a < Names.Length; a++)
            for (int b = 0; b < Names.Length; b++)
                if (t.Get(new CollisionGroup(a), new CollisionGroup(b)) != CollisionResponse.Ignore) changed++;
        Assert.Equal(2, changed); // Player-Box and Box-Player
    }

    [Fact]
    public void GroupWithItself()
    {
        var t = new GroupTable(Names);
        var box = t.GetGroup("Box");
        t.Set(box, box, CollisionResponse.Solid);
        Assert.Equal(CollisionResponse.Solid, t.Get(box, box));
        Assert.Equal(CollisionResponse.Ignore, t.Get(box, t.GetGroup("Enemy")));
    }

    [Fact]
    public void SetAgainOverwrites()
    {
        var t = new GroupTable(Names);
        var enemy = t.GetGroup("Enemy");
        t.Set(enemy, enemy, CollisionResponse.Solid);
        t.Set(enemy, enemy, CollisionResponse.Soft);
        Assert.Equal(CollisionResponse.Soft, t.Get(enemy, enemy));
    }

    [Fact]
    public void AllFourResponsesStored()
    {
        var t = new GroupTable(Names);
        var g = new CollisionGroup[Names.Length];
        for (int i = 0; i < g.Length; i++) g[i] = t.GetGroup(Names[i]);
        t.Set(g[0], g[1], CollisionResponse.Solid);
        t.Set(g[0], g[2], CollisionResponse.Soft);
        t.Set(g[0], g[3], CollisionResponse.Overlap);
        t.Set(g[1], g[2], CollisionResponse.Ignore);
        Assert.Equal(CollisionResponse.Solid, t.Get(g[1], g[0]));
        Assert.Equal(CollisionResponse.Soft, t.Get(g[2], g[0]));
        Assert.Equal(CollisionResponse.Overlap, t.Get(g[3], g[0]));
        Assert.Equal(CollisionResponse.Ignore, t.Get(g[2], g[1]));
    }

    [Fact]
    public void DuplicateNameThrows()
    {
        Assert.Throws<ArgumentException>(() => new GroupTable(new[] { "Box", "Player", "Box" }));
    }

    [Fact]
    public void UnknownNameThrows()
    {
        var t = new GroupTable(Names);
        Assert.Throws<ArgumentException>(() => t.GetGroup("Enmey")); // typo on purpose
        Assert.Throws<ArgumentException>(() => t.GetGroup("player")); // names are case-sensitive
    }

    [Fact]
    public void EmptyAndSingleGroupTables()
    {
        Assert.Equal(0, new GroupTable(new string[0]).Count);
        var one = new GroupTable(new[] { "Only" });
        var g = one.GetGroup("Only");
        one.Set(g, g, CollisionResponse.Solid);
        Assert.Equal(CollisionResponse.Solid, one.Get(g, g));
    }
}
