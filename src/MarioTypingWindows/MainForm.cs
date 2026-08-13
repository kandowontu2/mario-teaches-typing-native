namespace MarioTypingWindows;

internal sealed class MainForm : Form
{
    private readonly NativeGameEngine engine;
    private readonly GameSurface surface = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer frameTimer = new() { Interval = 16 };
    private FormBorderStyle previousBorderStyle;
    private Rectangle previousBounds;
    private bool fullScreen;

    public MainForm()
    {
        Text = "Mario Teaches Typing";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 720);
        MinimumSize = new Size(640, 480);
        BackColor = Color.Black;
        KeyPreview = true;

        engine = new NativeGameEngine();
        engine.ExitRequested += (_, _) => Close();
        engine.CertificatePrintRequested += PrintCertificate;
        engine.StudentInformationCursorRequested += point =>
        {
            if (surface.IsHandleCreated)
            {
                Cursor.Position = surface.PointToScreen(surface.OriginalToClient(point));
            }
        };
        engine.LessonTimesCursorRequested += point =>
        {
            if (surface.IsHandleCreated)
            {
                Cursor.Position = surface.PointToScreen(surface.OriginalToClient(point));
            }
        };
        engine.CertificateCursorRequested += point =>
        {
            if (surface.IsHandleCreated)
            {
                Cursor.Position = surface.PointToScreen(surface.OriginalToClient(point));
            }
        };
        surface.Engine = engine;
        surface.MouseDown += (_, eventArgs) =>
        {
            var point = surface.ClientToOriginal(eventArgs.Location);
            if (point.X >= 0)
            {
                engine.HandleClick(point.X, point.Y);
            }
            surface.Focus();
        };
        surface.MouseWheel += (_, eventArgs) => engine.HandleMouseWheel(eventArgs.Delta);
        engine.FrameChanged += (_, _) => surface.Invalidate();
        Controls.Add(surface);

        KeyDown += HandleKeyDown;
        KeyPress += (_, eventArgs) =>
        {
            engine.HandleCharacter(eventArgs.KeyChar);
            eventArgs.Handled = true;
        };
        frameTimer.Tick += (_, _) => engine.Tick();
        frameTimer.Start();
        Shown += (_, _) => surface.Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            frameTimer.Dispose();
            engine.Dispose();
        }
        base.Dispose(disposing);
    }

    private void HandleKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode == Keys.F11 || (eventArgs.Alt && eventArgs.KeyCode == Keys.Enter))
        {
            ToggleFullScreen();
            eventArgs.SuppressKeyPress = true;
            return;
        }
        engine.HandleKeyDown(eventArgs.KeyCode);
    }

    private void ToggleFullScreen()
    {
        if (!fullScreen)
        {
            previousBorderStyle = FormBorderStyle;
            previousBounds = Bounds;
            FormBorderStyle = FormBorderStyle.None;
            Bounds = Screen.FromControl(this).Bounds;
            TopMost = true;
        }
        else
        {
            TopMost = false;
            FormBorderStyle = previousBorderStyle;
            Bounds = previousBounds;
        }
        fullScreen = !fullScreen;
    }

    private void PrintCertificate(CertificatePrintRequest request)
    {
        using var document = new System.Drawing.Printing.PrintDocument
        {
            DocumentName = $"Mario Teaches Typing - {request.Student}",
        };
        document.PrintPage += (_, eventArgs) =>
        {
            if (eventArgs.Graphics is null)
            {
                return;
            }
            using var titleFont = new Font("Times New Roman", 28, FontStyle.Bold);
            using var bodyFont = new Font("Times New Roman", 17, FontStyle.Regular);
            using var nameFont = new Font("Times New Roman", 22, FontStyle.Bold | FontStyle.Italic);
            using var centered = new StringFormat { Alignment = StringAlignment.Center };
            var centerX = eventArgs.MarginBounds.Left + eventArgs.MarginBounds.Width / 2f;
            eventArgs.Graphics.DrawString(
                "MARIO TEACHES TYPING",
                titleFont,
                Brushes.Black,
                centerX,
                eventArgs.MarginBounds.Top + 40,
                centered);
            eventArgs.Graphics.DrawString(
                "CERTIFICATE OF COMPLETION\n\nThis is to certify that",
                bodyFont,
                Brushes.Black,
                centerX,
                eventArgs.MarginBounds.Top + 110,
                centered);
            eventArgs.Graphics.DrawString(
                request.Student,
                nameFont,
                Brushes.Black,
                centerX,
                eventArgs.MarginBounds.Top + 245,
                centered);
            eventArgs.Graphics.DrawString(
                $"has successfully completed the\n{request.Level} Level of\nMario Teaches Typing\n\n{DateTime.Now:d}",
                bodyFont,
                Brushes.Black,
                centerX,
                eventArgs.MarginBounds.Top + 310,
                centered);
        };

        using var dialog = new PrintDialog
        {
            Document = document,
            UseEXDialog = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            document.Print();
        }
    }

}
