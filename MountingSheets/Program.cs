using System.Diagnostics;

namespace MountingSheets;

internal class Program
{
    static void Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        SceneChangeDetection.Hren(@"C:\\Users\\m-dan\\Downloads\\vd.mp4");
        sw.Stop();
        Console.WriteLine($"Выполнение завершено, время: {sw.ElapsedMilliseconds} мс");
    }
}
