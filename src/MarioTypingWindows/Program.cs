using System.Text;

namespace MarioTypingWindows;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (args.Length >= 2 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
        {
            return NativeSelfTest.Run(args[1]);
        }

        if (args.Length >= 2 && args[0].Equals("--render-test", StringComparison.OrdinalIgnoreCase))
        {
            return NativeSelfTest.RenderOriginalFrames(args[1]);
        }

        if (args.Length >= 2 && args[0].Equals("--render-mve-test", StringComparison.OrdinalIgnoreCase))
        {
            return NativeSelfTest.RenderMveFrames(args[1]);
        }

        if (args.Length >= 2 && args[0].Equals("--render-av-test", StringComparison.OrdinalIgnoreCase))
        {
            return NativeSelfTest.RenderAvContactSheets(args[1]);
        }

        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, eventArgs) => ShowFatalError(eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            ShowFatalError(eventArgs.ExceptionObject as Exception
                ?? new InvalidOperationException("An unknown error occurred."));

        Application.Run(new MainForm());
        return 0;
    }

    private static void ShowFatalError(Exception exception)
    {
        MessageBox.Show(
            $"Mario Teaches Typing Native encountered an error:\n\n{exception.Message}",
            "Mario Teaches Typing Native",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
