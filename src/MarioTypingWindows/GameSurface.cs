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

        var frame = Engine.CurrentFrame;
        var target = CalculateTargetRectangle(ClientRectangle, frame.Size);
        eventArgs.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        eventArgs.Graphics.CompositingMode = CompositingMode.SourceCopy;
        eventArgs.Graphics.DrawImage(
            frame,
            target,
            0,
            0,
            frame.Width,
            frame.Height,
            GraphicsUnit.Pixel);
    }

    public Point ClientToOriginal(Point point)
    {
        var frameSize = Engine?.CurrentFrame.Size ??
                        new Size(NativeGameEngine.ClassicFrameWidth, NativeGameEngine.FrameHeight);
        var target = CalculateTargetRectangle(ClientRectangle, frameSize);
        if (!target.Contains(point))
        {
            return new Point(-1, -1);
        }
        return new Point(
            Math.Clamp((point.X - target.X) * frameSize.Width / target.Width, 0, frameSize.Width - 1),
            Math.Clamp((point.Y - target.Y) * frameSize.Height / target.Height, 0, frameSize.Height - 1));
    }

    public Point OriginalToClient(Point point)
    {
        var frameSize = Engine?.CurrentFrame.Size ??
                        new Size(NativeGameEngine.ClassicFrameWidth, NativeGameEngine.FrameHeight);
        var target = CalculateTargetRectangle(ClientRectangle, frameSize);
        return new Point(
            target.X + point.X * target.Width / frameSize.Width,
            target.Y + point.Y * target.Height / frameSize.Height);
    }

    internal static Rectangle CalculateTargetRectangle(Rectangle bounds, Size frameSize)
    {
        // Classic Mode 13h was displayed on a 4:3 CRT. The extended logical
        // framebuffers retain that pixel aspect at 16:9 and 21:9.
        var aspectRatio = frameSize.Width switch
        {
            >= NativeGameEngine.UltraWidescreenFrameWidth => 21d / 9d,
            > NativeGameEngine.ClassicFrameWidth => 16d / 9d,
            _ => 4d / 3d,
        };
        var width = bounds.Width;
        var height = (int)Math.Round(width / aspectRatio);
        if (height > bounds.Height)
        {
            height = bounds.Height;
            width = (int)Math.Round(height * aspectRatio);
        }
        return new Rectangle(
            bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2,
            Math.Max(1, width),
            Math.Max(1, height));
    }
}
