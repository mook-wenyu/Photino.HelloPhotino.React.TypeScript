using System.Runtime.InteropServices;

namespace Photino.HelloPhotino.React.TypeScript;

public partial class CPPDll
{
    // DPI_AWARENESS_CONTEXT 常量
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    // 声明 SetProcessDpiAwarenessContext 函数
    // 使用 LibraryImportAttribute 标记外部函数引入，并使用 MarshalAs 特性指示返回类型为布尔值
    // 此函数用于设置 DPI 能力上下文
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

}