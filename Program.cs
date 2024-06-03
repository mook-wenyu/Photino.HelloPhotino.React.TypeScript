using Photino.NET;
using PhotinoNET.Server;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Photino.HelloPhotino.React.TypeScript;

class Program
{
#if DEBUG
    public static bool IsDebugMode = true;
#else
    public static bool IsDebugMode = false;
#endif

    private static readonly bool _logEvents = true;
    private static int _windowNumber = 1;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            InitMainWindow(args);
        }
        catch (Exception ex)
        {
            Log(null, ex.Message);
            Console.ReadKey();
        }
    }

    private static void InitMainWindow(string[] args)
    {
        int width = 1280;
        int height = 800;
        if (PhotinoWindow.IsWindowsPlatform)
        {
            // 设置进程的 DPI 感知上下文
            if (!CPPDll.SetProcessDpiAwarenessContext(CPPDll.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
            {
                Console.WriteLine("Failed to set DPI awareness context.");
                return;
            }

            (width, height) = Utils.SetWindowSize(width, height);
            Log(null, $"Adjusted window size: {width}x{height}");
        }

        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        // The appUrl is set to the local development server when in debug mode.
        // This helps with hot reloading and debugging.
        string appUrl = IsDebugMode ? "http://localhost:5173" : $"{baseUrl}/index.html";
        Console.WriteLine($"Serving React app at {appUrl}");

        var iconFile = PhotinoWindow.IsWindowsPlatform
                ? "wwwroot/photino-logo.ico"
                : "wwwroot/photino-logo.png";

        // Window title declared here for visibility
        string windowTitle = "Photino.React.TypeScript Demo App";

        string browserInit = string.Empty;
        if (PhotinoWindow.IsWindowsPlatform)
        {
            //Windows example for WebView2
            browserInit = "--disable-web-security --hide-scrollbars ";
        }
        else if (PhotinoWindow.IsMacOsPlatform)
        {
            //Mac example for Webkit on Cocoa
            browserInit = JsonSerializer.Serialize(new
            {
                setLegacyEncryptedMediaAPIEnabled = true
            });
        }
        else if (PhotinoWindow.IsLinuxPlatform)
        {
            //Linux example for Webkit2Gtk
            browserInit = JsonSerializer.Serialize(new
            {
                set_enable_encrypted_media = true,
                //set_default_font_size = 48,
                //set_enable_developer_extras = true,
                set_default_font_family = "monospace"
            });
        }

        // Creating a new PhotinoWindow instance with the fluent API
        var window = new PhotinoWindow()
            // .SetIconFile(iconFile)
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(width, height))
            // Resize to a percentage of the main monitor work area
            //.Resize(50, 50, "%")
            // Center window in the middle of the screen
            .Center()
            // Users can resize windows by default.
            // Let's make this one fixed instead.
            .SetResizable(true)
            .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/javascript";
                return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
            })
            // Most event handlers can be registered after the
            // PhotinoWindow was instantiated by calling a registration 
            // method like the following RegisterWebMessageReceivedHandler.
            // This could be added in the PhotinoWindowOptions if preferred.
            .RegisterWebMessageReceivedHandler((object? sender, string message) =>
            {
                if (sender is PhotinoWindow window)
                {
                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    string response = $"Received message: \"{message}\"";

                    // Send a message back to the JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    window.SendWebMessage(response);
                }
                else
                {
                    // Handle the case where sender is null or not of type PhotinoWindow
                    // This might involve logging an error or throwing an exception.
                    // For this example, we will log an error message.
                    Console.Error.WriteLine("Sender is not a PhotinoWindow or is null.");
                }
            })
            .Load(appUrl); // Can be used with relative path strings or "new URI()" instance to load a website.

        window.WaitForClose(); // Starts the application event loop
    }



    //These are the event handlers I'm hooking up
    private static Stream AppCustomSchemeUsed(object sender, string scheme, string url, out string contentType)
    {
        Log(sender, $"Custom scheme '{scheme}' was used.");
        var currentWindow = sender as PhotinoWindow;

        if (currentWindow == null)
        {
            contentType = "text/javascript";
            return new MemoryStream();
        }

        contentType = "text/javascript";

        var js =
@"
(() =>{
    window.setTimeout(() => {
        const title = document.getElementById('Title');
        const lineage = document.getElementById('Lineage');
        title.innerHTML = "

        + $"'{currentWindow.Title}';" + "\n"

        + $"        lineage.innerHTML = `PhotinoWindow Id: {currentWindow.Id} <br>`;" + "\n";

        //show lineage of this window
        var p = currentWindow.Parent;
        while (p != null)
        {
            js += $"        lineage.innerHTML += `Parent Id: {p.Id} <br>`;" + "\n";
            p = p.Parent;
        }

        js +=
@"        alert(`🎉 Dynamically inserted JavaScript.`);
    }, 1000);
})();
";

        return new MemoryStream(Encoding.UTF8.GetBytes(js));
    }

    private static void MessageReceivedFromWindow(object? sender, string message)
    {
        Log(sender, $"MessageReceivedFromWindow Callback Fired.");

        var currentWindow = sender as PhotinoWindow;
        if (currentWindow == null) return;
        if (string.Compare(message, "child-window", true) == 0)
        {
            var iconFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "wwwroot/photino-logo.ico"
                : "wwwroot/photino-logo.png";

            var x = new PhotinoWindow(currentWindow)
                .SetTitle($"Child Window {_windowNumber++}")
                //.SetIconFile(iconFile)
                .Load("wwwroot/main.html")

                .SetUseOsDefaultLocation(true)
                .SetHeight(600)
                .SetWidth(800)

                .SetGrantBrowserPermissions(false)

                .RegisterWindowCreatingHandler(WindowCreating)
                .RegisterWindowCreatedHandler(WindowCreated)
                .RegisterLocationChangedHandler(WindowLocationChanged)
                .RegisterSizeChangedHandler(WindowSizeChanged)
                .RegisterWebMessageReceivedHandler(MessageReceivedFromWindow)
                .RegisterWindowClosingHandler(WindowIsClosing)

                .RegisterCustomSchemeHandler("app", AppCustomSchemeUsed)

                .SetTemporaryFilesPath(currentWindow.TemporaryFilesPath)
                .SetLogVerbosity(_logEvents ? 2 : 0);

            x.WaitForClose();

            //x.Center();           //WaitForClose() is non-blocking for child windows
            //x.SetHeight(800);
            //x.Close();
        }
        else if (string.Compare(message, "zoom-in", true) == 0)
        {
            currentWindow.Zoom += 5;
            Log(sender, $"Zoom: {currentWindow.Zoom}");
        }
        else if (string.Compare(message, "zoom-out", true) == 0)
        {
            currentWindow.Zoom -= 5;
            Log(sender, $"Zoom: {currentWindow.Zoom}");
        }
        else if (string.Compare(message, "center", true) == 0)
        {
            currentWindow.Center();
        }
        else if (string.Compare(message, "close", true) == 0)
        {
            currentWindow.Close();
        }
        else if (string.Compare(message, "clearbrowserautofill", true) == 0)
        {
            currentWindow.ClearBrowserAutoFill();
        }
        else if (string.Compare(message, "minimize", true) == 0)
        {
            currentWindow.SetMinimized(!currentWindow.Minimized);
        }
        else if (string.Compare(message, "maximize", true) == 0)
        {
            currentWindow.SetMaximized(!currentWindow.Maximized);
        }
        else if (string.Compare(message, "setcontextmenuenabled", true) == 0)
        {
            currentWindow.SetContextMenuEnabled(!currentWindow.ContextMenuEnabled);
        }
        else if (string.Compare(message, "setdevtoolsenabled", true) == 0)
        {
            currentWindow.SetDevToolsEnabled(!currentWindow.DevToolsEnabled);
        }
        else if (string.Compare(message, "setgrantbrowserpermissions", true) == 0)
        {
            currentWindow.SetGrantBrowserPermissions(!currentWindow.GrantBrowserPermissions);
        }
        else if (string.Compare(message, "seticonfile", true) == 0)
        {
            var iconFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "wwwroot/photino-logo.ico"
                : "wwwroot/photino-logo.png";

            currentWindow.SetIconFile(iconFile);
        }
        else if (string.Compare(message, "setposition", true) == 0)
        {
            currentWindow.SetLeft(currentWindow.Left + 5);
            currentWindow.SetTop(currentWindow.Top + 5);
        }
        else if (string.Compare(message, "setresizable", true) == 0)
        {
            currentWindow.SetResizable(!currentWindow.Resizable);
        }
        else if (string.Compare(message, "setsize-up", true) == 0)
        {
            currentWindow.SetHeight(currentWindow.Height + 5);
            currentWindow.SetWidth(currentWindow.Width + 5);
        }
        else if (string.Compare(message, "setsize-down", true) == 0)
        {
            currentWindow.SetHeight(currentWindow.Height - 5);
            currentWindow.SetWidth(currentWindow.Width - 5);
        }
        else if (string.Compare(message, "settitle", true) == 0)
        {
            currentWindow.SetTitle(currentWindow.Title + "*");
        }
        else if (string.Compare(message, "settopmost", true) == 0)
        {
            currentWindow.SetTopMost(!currentWindow.Topmost);
        }
        else if (string.Compare(message, "setfullscreen", true) == 0)
        {
            currentWindow.SetFullScreen(!currentWindow.FullScreen);
        }
        else if (string.Compare(message, "showproperties", true) == 0)
        {
            var properties = GetPropertiesDisplay(currentWindow);
            currentWindow.ShowMessage("Settings", properties);
        }
        else if (string.Compare(message, "sendWebMessage", true) == 0)
        {
            currentWindow.SendWebMessage("web message");
        }
        else if (string.Compare(message, "setMinSize", true) == 0)
        {
            currentWindow.SetMinSize(320, 240);
        }
        else if (string.Compare(message, "setMaxSize", true) == 0)
        {
            currentWindow.SetMaxSize(640, 480);
        }
        else if (string.Compare(message, "toastNotification", true) == 0)
        {
            currentWindow.SendNotification("Toast Title", " Toast message!");
        }
        else if (string.Compare(message, "showOpenFile", true) == 0)
        {
            var results = currentWindow.ShowOpenFile(filters: new[]{
                    ("All files", new [] {"*.*"}),
                    ("Text files", new [] {"*.txt"}),
                    ("Image files", new [] {"*.png", "*.jpg", "*.jpeg"}),
                    ("PDF files", new [] {"*.pdf"}),
                    ("CSharp files", new [] { "*.cs" })
                });
            if (results.Length > 0)
                currentWindow.ShowMessage("Open File", string.Join(Environment.NewLine, results));
            else
                currentWindow.ShowMessage("Open File", "No file chosen", icon: PhotinoDialogIcon.Error);
        }
        else if (string.Compare(message, "showOpenFolder", true) == 0)
        {
            var results = currentWindow.ShowOpenFolder(multiSelect: true);
            if (results.Length > 0)
                currentWindow.ShowMessage("Open Folder", string.Join(Environment.NewLine, results));
            else
                currentWindow.ShowMessage("Open Folder", "No folder chosen", icon: PhotinoDialogIcon.Error);
        }
        else if (string.Compare(message, "showSaveFile", true) == 0)
        {
            var result = currentWindow.ShowSaveFile();
            if (result != null)
                currentWindow.ShowMessage("Save File", result);
            else
                currentWindow.ShowMessage("Save File", "File not saved", icon: PhotinoDialogIcon.Error);
        }
        else if (string.Compare(message, "showMessage", true) == 0)
        {
            var result = currentWindow.ShowMessage("Title", "Testing...");
        }
        else
            throw new Exception($"Unknown message '{message}'");
    }

    private static void WindowCreating(object? sender, EventArgs e)
    {
        Log(sender, "WindowCreating Callback Fired.");
    }

    private static void WindowCreated(object? sender, EventArgs e)
    {
        Log(sender, "WindowCreated Callback Fired.");
    }

    private static void WindowLocationChanged(object? sender, Point location)
    {
        Log(sender, $"WindowLocationChanged Callback Fired.  Left: {location.X}  Top: {location.Y}");
    }

    private static void WindowSizeChanged(object? sender, Size size)
    {
        Log(sender, $"WindowSizeChanged Callback Fired.  Height: {size.Height}  Width: {size.Width}");
    }

    private static void WindowMaximized(object sender, EventArgs e)
    {
        Log(sender, $"{nameof(WindowMaximized)} Callback Fired.");
    }

    private static void WindowRestored(object sender, EventArgs e)
    {
        Log(sender, $"{nameof(WindowRestored)} Callback Fired.");
    }

    private static void WindowMinimized(object sender, EventArgs e)
    {
        Log(sender, $"{nameof(WindowMinimized)} Callback Fired.");
    }

    private static bool WindowIsClosing(object sender, EventArgs e)
    {
        Log(sender, "WindowIsClosing Callback Fired.");
        return false;   //return true to block closing of the window
    }

    private static void WindowFocusIn(object sender, EventArgs e)
    {
        Log(sender, "WindowFocusIn Callback Fired.");
    }

    private static void WindowFocusOut(object sender, EventArgs e)
    {
        Log(sender, "WindowFocusOut Callback Fired.");
    }




    private static string GetPropertiesDisplay(PhotinoWindow currentWindow)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Title: {currentWindow.Title}");
        sb.AppendLine($"Zoom: {currentWindow.Zoom}");
        sb.AppendLine();
        sb.AppendLine($"ContextMenuEnabled: {currentWindow.ContextMenuEnabled}");
        sb.AppendLine($"DevToolsEnabled: {currentWindow.DevToolsEnabled}");
        sb.AppendLine($"GrantBrowserPermissions: {currentWindow.GrantBrowserPermissions}");
        sb.AppendLine();
        sb.AppendLine($"Top: {currentWindow.Top}");
        sb.AppendLine($"Left: {currentWindow.Left}");
        sb.AppendLine($"Height: {currentWindow.Height}");
        sb.AppendLine($"Width: {currentWindow.Width}");
        sb.AppendLine();
        sb.AppendLine($"Resizable: {currentWindow.Resizable}");
        sb.AppendLine($"Screen DPI: {currentWindow.ScreenDpi}");
        sb.AppendLine($"Topmost: {currentWindow.Topmost}");
        sb.AppendLine($"Maximized: {currentWindow.Maximized}");
        sb.AppendLine($"Minimized: {currentWindow.Minimized}");

        return sb.ToString();
    }


    private static void Log(object? sender, string message)
    {
        if (!_logEvents) return;
        var currentWindow = sender as PhotinoWindow;
        var windowTitle = currentWindow == null ? string.Empty : currentWindow.Title;
        Console.WriteLine($"-Client App: \"{windowTitle ?? "title?"}\" {message}");
    }
}
