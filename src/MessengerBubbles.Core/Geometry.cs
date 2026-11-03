namespace MessengerBubbles.Core;

public record Box(double X, double Y, double W, double H);

public static class Geometry
{
    const double Gap = 8;
    const double DismissWidth = 240;
    const double DismissHeight = 140;

    public static double SnapX(Box bubble, Box workArea) =>
        bubble.X + bubble.W / 2 < workArea.X + workArea.W / 2
            ? workArea.X
            : workArea.X + workArea.W - bubble.W;

    public static (double W, double H) PanelDips(PanelSize size) => size switch
    {
        PanelSize.Small => (340, 480),
        PanelSize.Large => (460, 720),
        _ => (380, 560),
    };

    // Where a dismissed bubble ends up: centered on the mark in the dismiss zone.
    public static (double X, double Y) DismissTarget(Box bubble, Box workArea) =>
        (workArea.X + (workArea.W - bubble.W) / 2, workArea.Y + workArea.H - (DismissHeight + bubble.H) / 2);

    public static Box PlacePanel(Box bubble, double w, double h, Box workArea)
    {
        w = Math.Min(w, workArea.W);
        h = Math.Min(h, workArea.H);
        var onLeft = bubble.X + bubble.W / 2 < workArea.X + workArea.W / 2;
        var x = onLeft ? bubble.X + bubble.W + Gap : bubble.X - w - Gap;
        return new Box(Clamp(x, workArea.X, workArea.X + workArea.W - w),
                       Clamp(bubble.Y, workArea.Y, workArea.Y + workArea.H - h), w, h);
    }

    // Beside the bubble, moved above or below the open chat panel rather than covering it.
    public static Box PlacePreview(Box bubble, double w, double h, Box workArea, Box? panel)
    {
        var box = PlacePanel(bubble, w, h, workArea);
        if (panel == null || !Overlaps(box, panel)) return box;
        if (panel.Y - Gap - h >= workArea.Y) return box with { Y = panel.Y - Gap - h };
        if (panel.Y + panel.H + Gap + h <= workArea.Y + workArea.H) return box with { Y = panel.Y + panel.H + Gap };
        return box;
    }

    static bool Overlaps(Box a, Box b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;

    public static Box RestoreOnScreen(Box saved, IReadOnlyList<Box> workAreas)
    {
        var cx = saved.X + saved.W / 2;
        var cy = saved.Y + saved.H / 2;
        if (workAreas.Any(a => cx >= a.X && cx < a.X + a.W && cy >= a.Y && cy < a.Y + a.H))
            return saved;
        var first = workAreas[0];
        return saved with
        {
            X = first.X + first.W - saved.W,
            Y = Clamp(saved.Y, first.Y, first.Y + first.H - saved.H),
        };
    }

    public static bool InDismissZone(Box bubble, Box workArea)
    {
        var cx = bubble.X + bubble.W / 2;
        var cy = bubble.Y + bubble.H / 2;
        return Math.Abs(cx - (workArea.X + workArea.W / 2)) <= DismissWidth / 2
            && cy >= workArea.Y + workArea.H - DismissHeight;
    }

    // A new bubble goes above the column's stack, then below start.Y when the top is full; columns move away from the bubble's edge.
    public static Box FindFreeSlot(Box start, IReadOnlyList<Box> taken, Box area, double spacing)
    {
        var inward = start.X + start.W / 2 < area.X + area.W / 2 ? 1 : -1;
        var stepX = (start.W + spacing) * inward;
        var stepY = start.H + spacing;
        for (var x = start.X; x >= area.X && x + start.W <= area.X + area.W; x += stepX)
        {
            var column = taken.Where(o => x < o.X + o.W && o.X < x + start.W).ToList();
            var top = column.Count > 0 ? Math.Min(start.Y, column.Min(o => o.Y)) : start.Y;
            for (var y = top; y >= area.Y; y -= stepY)
                if (IsFree(x, y)) return start with { X = x, Y = y };
            for (var y = start.Y; y + start.H <= area.Y + area.H; y += stepY)
                if (IsFree(x, y)) return start with { X = x, Y = y };
        }
        return start;

        bool IsFree(double x, double y) => !taken.Any(o =>
            x < o.X + o.W && o.X < x + start.W && y < o.Y + o.H && o.Y < y + start.H);
    }

    static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(v, max));
}
