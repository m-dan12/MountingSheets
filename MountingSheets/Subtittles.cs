using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vosk;

namespace MountingSheets
{
    internal class Subtittles
    {
        public static void ExtractAudio(string inputVideoPath, string outputAudioPath)
        {
            // Создаем процесс для запуска FFmpeg
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"D:\Subtittles\ffmpeg-7.1-essentials_build\bin\ffmpeg.exe", // или полный путь к ffmpeg, если он не добавлен в PATH
                    Arguments = $"-i \"{inputVideoPath}\" -ar 16000 -ac 1 \"{outputAudioPath}\"", // Установка частоты 16000 Гц и моно-канала
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }

        public static void DemoSpeaker(Model model, string audioFilePath)
        {
            string txtPath = @"D:\Subtittles\text.txt";
            using StreamWriter writer = new StreamWriter(txtPath);
            // Output speakers
            SpkModel spkModel = new SpkModel(@"D:\Subtittles\vosk-model-spk-0.4");
            VoskRecognizer rec = new VoskRecognizer(model, 16000.0f);
            rec.SetSpkModel(spkModel);

            using (Stream source = File.OpenRead(audioFilePath))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (rec.AcceptWaveform(buffer, bytesRead))
                    {
                        writer.WriteLine(rec.Result());
                    }
                    else
                    {
                        //Console.WriteLine(rec.PartialResult());
                    }
                }
            }
            Console.WriteLine(rec.FinalResult());
        }

        /*public static void Main()
        {
            // You can set to -1 to disable logging messages
            Vosk.Vosk.SetLogLevel(0);
            string videoFilePath = @"D:\Video\dv2.mp4"; //путь к исходнику
            string audioFilePath = @"D:\Audio\output.wav"; //путь к папке куда сохраняем

            // Извлечение аудио из видео
            //ExtractAudio(videoFilePath, audioFilePath);
            Console.WriteLine("Пошел я нахуй");
            Model model = new Model(@"D:\Subtittles\vosk-model-small-ru-0.22");
            DemoBytes(model);
            DemoFloats(model);
            DemoSpeaker(model, audioFilePath);
        }*/
    }
}
