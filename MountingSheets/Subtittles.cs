using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vosk;

namespace MountingSheets;

public class Subtittles
{
    public static Model model = new(@"..\..\..\Vosk\small");
    public static SpkModel spkModel = new(@"..\..\..\Vosk\spk");
    public static void ExtractAudio(Meta meta)
    {
        if (!File.Exists(meta.AudioPath))
            return;
        // Создаем процесс для запуска FFmpeg
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"..\..\..\FFMpeg\bin\ffmpeg.exe", // или полный путь к ffmpeg, если он не добавлен в PATH
                Arguments = $"-i \"{meta.VideoPath}\" -ar 16000 -ac 1 \"{meta.AudioPath}\"", // Установка частоты 16000 Гц и моно-канала
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    public static void DemoSpeaker(Meta meta)
    {

        using StreamWriter writer = new(meta.SubtitlesPath);
        VoskRecognizer rec = new(model, 16000.0f, spkModel);

        using (Stream source = File.OpenRead(meta.AudioPath))
        {
            byte[] buffer = new byte[1280];
            int bytesRead;
            int i = 0;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (rec.AcceptWaveform(buffer, bytesRead))
                {
                    writer.WriteLine(rec.Result());
                }
                Console.WriteLine($"{i / 25 / 60 : d2}:{i / 25 % 60 : d2}:{i % 25 : d2}");
                i++;
            }
        }
        Console.WriteLine(rec.FinalResult());
    }
}