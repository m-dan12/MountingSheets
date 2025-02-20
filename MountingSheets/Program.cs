using System.Diagnostics;

namespace MountingSheets;

public class Meta
{
    public string Name { get; set; }
    public string VideoPath { get; set; }
    public string AudioPath { get; set; }
    public string SubtitlesPath { get; set; }
    public string FramesFolderPath { get; set; }
    public string CsvPath { get; set; }
    public string SrtPath { get; set; }
    public Meta(string path)
    {
        Name                = path.Split('\\').Last().Replace(".mp4", "");
        VideoPath           = $@"..\..\..\{path}";
        AudioPath           = $@"..\..\..\Audio\{Name}.wav";
        SubtitlesPath       = $@"..\..\..\Subtitles\{Name}.txt";
        FramesFolderPath    = $@"..\..\..\Frames\{Name}";
        CsvPath             = $@"..\..\..\Csv\{Name}.csv";
        SrtPath             = $@"..\..\..\Srt\{Name}.srt";
    }
}
public class CsvExport
{

}

internal class Program
{
    public static void StopwatchVoid(Action action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        Console.WriteLine($"Выполнение завершено, время: {sw.ElapsedMilliseconds} мс");
    }
    static void Main()
    {
        string excelPath = @"C:\Users\m-dan\Downloads\Как зоолог зоологу.xlsx";
        string srtPath = @"C:\Users\m-dan\Downloads\Telegram Desktop\SRT\Как зоолог зоологу.srt";
        string outputExcelPath = @"C:\Users\m-dan\Downloads\Out Как зоолог зоологу.xlsx";

        // Создаем экземпляр класса и вызываем метод
        var matcher = new SceneSubtitleMatcher();
        matcher.MatchScenesWithSubtitles(excelPath, srtPath, outputExcelPath);

        // Meta meta = new(@"C:\Users\m-dan\Downloads\Ау люди.mp4");

        // VideoAnalyzer.VideoToFrames(meta);


        // WhisperWrapper.Wrap(meta);

        // VideoAnalyzer.AlterVar(meta);

        //StopwatchVoid(() => VideoAnalyzer.ExtractFrames(meta));

        // You can set to -1 to disable logging messages
        // Vosk.Vosk.SetLogLevel(-1);

        // Извлечение аудио из видео
        // Subtittles.ExtractAudio(meta);
        // Subtittles.DemoSpeaker(meta);
    }
}
 