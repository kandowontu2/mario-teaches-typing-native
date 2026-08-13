using System.Drawing.Drawing2D;

namespace MarioTypingWindows;

internal sealed class GameSurface : Control
{
    public GameSurface()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.Black;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    public NativeGameEngine? Engine { get; set; }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (Engine is null)
        {
            return;
        }

        var target = CalculateTargetRectangle(ClientRectangle);
        eventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        eventArgs.Graphics.CompositingMode = CompositingMode.SourceCopy;
        eventArgs.Graphics.DrawImage(Engine.CurrentFrame, target, 0, 0, 320, 200, GraphicsUnit.Pixel);
    }

    public Point ClientToOriginal(Point point)
    {
        var target = CalculateTargetRectangle(ClientRectangle);
        if (!target.Contains(point))
        {
            return new Point(-1, -1);
        }
        return new Point(
            Math.Clamp((point.X - target.X) * 320 / target.Width, 0, 319),
            Math.Clamp((point.Y - target.Y) * 200 / target.Height, 0, 199));
    }

    public Point OriginalToClient(Point point)
    {
        var target = CalculateTargetRectangle(ClientRectangle);
        return new Point(
            target.X + point.X * target.Width / 320,
            target.Y + point.Y * target.Height / 200);
    }

    private static Rectangle CalculateTargetRectangle(Rectangle bounds)
    {
        // Mode 13h was displayed on a 4:3 CRT; preserve that intended aspect ratio.
        var width = bounds.Width;
        var height = width * 3 / 4;
        if (height > bounds.Height)
        {
            height = bounds.Height;
            width = height * 4 / 3;
        }
        return new Rectangle(
            bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2,
            Math.Max(1, width),
            Math.Max(1, height));
    }
}
