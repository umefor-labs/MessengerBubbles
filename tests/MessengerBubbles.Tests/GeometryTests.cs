using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class GeometryTests
{
    static readonly Box Screen = new(0, 0, 1920, 1040);
    static readonly Box SecondScreen = new(1920, 0, 1280, 720);

    [Theory]
    [InlineData(100, 0)]
    [InlineData(1500, 1864)]
    public void SnapX_GoesToNearestEdge(double x, double expected) =>
        Assert.Equal(expected, Geometry.SnapX(new Box(x, 300, 56, 56), Screen));

    [Fact]
    public void SnapX_UsesTheBubblesOwnScreen() =>
        Assert.Equal(1920, Geometry.SnapX(new Box(2000, 300, 56, 56), SecondScreen));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1864, 0)]
    [InlineData(0, 984)]
    [InlineData(1864, 984)]
    public void PlacePanel_StaysInsideWorkArea(double x, double y)
    {
        var p = Geometry.PlacePanel(new Box(x, y, 56, 56), 380, 560, Screen);
        Assert.InRange(p.X, 0, 1920 - 380);
        Assert.InRange(p.Y, 0, 1040 - 560);
    }

    [Fact]
    public void PlacePanel_ShrinksToFitSmallScreen()
    {
        var small = new Box(0, 0, 1024, 600);
        var p = Geometry.PlacePanel(new Box(968, 100, 56, 56), 460, 720, small);
        Assert.Equal(600, p.H);
        Assert.Equal(0, p.Y);
    }

    [Fact]
    public void DismissTarget_CentersOnMark()
    {
        var (x, y) = Geometry.DismissTarget(new Box(100, 100, 56, 56), new Box(0, 0, 1000, 1000));
        Assert.Equal(472, x);
        Assert.Equal(902, y);
    }

    [Fact]
    public void PlacePanel_DoesNotCoverBubble()
    {
        var p = Geometry.PlacePanel(new Box(1864, 300, 56, 56), 380, 560, Screen);
        Assert.True(p.X + p.W <= 1864);
    }

    [Fact]
    public void RestoreOnScreen_OnScreenSaved_Unchanged()
    {
        var saved = new Box(2000, 100, 56, 56);
        Assert.Equal(saved, Geometry.RestoreOnScreen(saved, [Screen, SecondScreen]));
    }

    [Fact]
    public void RestoreOnScreen_OffScreenSaved_MovesToFirstWorkArea()
    {
        var r = Geometry.RestoreOnScreen(new Box(2000, 100, 56, 56), [Screen]);
        Assert.InRange(r.X, 0, 1920 - 56);
        Assert.InRange(r.Y, 0, 1040 - 56);
    }

    [Fact]
    public void FindFreeSlot_StartFree_ReturnsStart()
    {
        var start = new Box(1864, 300, 56, 56);
        Assert.Equal(start, Geometry.FindFreeSlot(start, [], Screen, 8));
    }

    [Fact]
    public void FindFreeSlot_Taken_GoesBelow()
    {
        var start = new Box(1864, 300, 56, 56);
        Assert.Equal(start with { Y = 364 }, Geometry.FindFreeSlot(start, [start], Screen, 8));
    }

    [Fact]
    public void FindFreeSlot_RightEdgeNearBottom_WrapsToTopOfColumn()
    {
        var start = new Box(1864, 950, 56, 56);
        Assert.Equal(start with { Y = 0 }, Geometry.FindFreeSlot(start, [start], Screen, 8));
    }

    [Fact]
    public void FindFreeSlot_ColumnFull_MovesInward()
    {
        var start = new Box(1864, 950, 56, 56);
        var r = Geometry.FindFreeSlot(start, [new Box(1864, 0, 56, 1040)], Screen, 8);
        Assert.Equal(1800, r.X);
    }

    [Fact]
    public void FindFreeSlot_ScreenFull_FallsBackToStart()
    {
        var start = new Box(1864, 950, 56, 56);
        Assert.Equal(start, Geometry.FindFreeSlot(start, [Screen], Screen, 8));
    }

    [Theory]
    [InlineData(932, 950, true)]
    [InlineData(932, 300, false)]
    [InlineData(100, 950, false)]
    public void InDismissZone_OnlyNearBottomCentre(double x, double y, bool expected) =>
        Assert.Equal(expected, Geometry.InDismissZone(new Box(x, y, 56, 56), Screen));
}
