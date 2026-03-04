using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LanChatClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(@"C:\LanChat", "log", "crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers (żeby appka nie "znikała" bez śladu)
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            MessageBox.Show(
                "Wystąpił błąd aplikacji (UI). Szczegóły zapisane w:\r\n" + CrashLogPath +
                "\r\n\r\nTreść:\r\n" + e.Exception.Message,
                "LanChat - błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // ignore
        }

        // Nie zabijaj całego procesu - chcemy zebrać log i pozwolić pracować dalej
        e.Handled = true;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash("AppDomain.UnhandledException (IsTerminating=" + e.IsTerminating + ")", ex);
        }
        catch
        {
            // ignore
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
        }
        catch
        {
            // ignore
        }

        // żeby nie wywaliło procesu przy finalizerze
        e.SetObserved();
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);

            var sb = new StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.AppendLine("Source: " + source);

            if (ex is null)
            {
                sb.AppendLine("Exception: <null>");
            }
            else
            {
                sb.AppendLine("Exception: " + ex.GetType().FullName);
                sb.AppendLine("Message: " + ex.Message);
                sb.AppendLine("Stack: " + ex.StackTrace);

                if (ex.InnerException is not null)
                {
                    sb.AppendLine("--- Inner ---");
                    sb.AppendLine(ex.InnerException.GetType().FullName);
                    sb.AppendLine(ex.InnerException.Message);
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
            }

            File.AppendAllText(CrashLogPath, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // log nie może wysypać appki
        }
    }
}
