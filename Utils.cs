using System.Runtime.InteropServices;

namespace Photino.HelloPhotino.React.TypeScript;

public partial class Utils
{

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForSystem();

    // DPI 缩放比例常量
    private const uint DPI_DEFAULT = 96;

    /// <summary>
    /// 根据系统 DPI 设置窗口尺寸
    /// </summary>
    /// <param name="width">原始宽度</param>
    /// <param name="height">原始高度</param>
    /// <returns>调整后的窗口尺寸</returns>
    public static (int, int) SetWindowSize(int width, int height)
    {
        // 获取当前 DPI 值
        uint dpi = GetDpiForSystem();

        // 计算 DPI 缩放比例
        double scale = dpi / DPI_DEFAULT;

        // 调整窗口尺寸
        int adjustedWidth = (int)Math.Ceiling(width * scale);
        int adjustedHeight = (int)Math.Ceiling(height * scale);

        return (adjustedWidth, adjustedHeight);
    }

}