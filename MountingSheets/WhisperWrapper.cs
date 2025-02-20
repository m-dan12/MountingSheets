using System.Diagnostics;

namespace MountingSheets;

public class WhisperWrapper
{
    public static void Wrap(Meta meta)
    {
        string pythonPath = @"python.exe";  // Укажите путь к Python
        string scriptPath = @"..\..\..\whisper_process.py";  // Путь к вашему Python-скрипту
        string inputFile = meta.VideoPath;
        string outputFile = meta.SrtPath;

        ProcessStartInfo psi = new()
        {
            FileName = pythonPath,
            Arguments = $"{scriptPath} \"{inputFile}\" \"{outputFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(psi);
        process.WaitForExit();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(error))
            Console.WriteLine("Error: " + error);
        else
            Console.WriteLine("Subtitles created: " + outputFile);
    }
}
