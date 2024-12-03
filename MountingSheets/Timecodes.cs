using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenCvSharp;
using System.Linq;

namespace MountingSheets;

public class VideoAnalyzer
{
    // Метод для извлечения кадров с помощью FFmpeg
    public static void VideoToFrames(Meta meta)
    {
        if (!Directory.Exists(meta.FramesFolderPath))
        {
            Directory.CreateDirectory(meta.FramesFolderPath);
        }

        using VideoCapture videoCapture = new(meta.VideoPath);

        int frameCount = videoCapture.FrameCount;
        int numberOfFramesInDirectory = Directory.GetFiles(meta.FramesFolderPath, $"{meta.Name}_*.jpeg").Length;

        if (frameCount == numberOfFramesInDirectory)
            return;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = @"..\..\..\FFMpeg\bin\ffmpeg.exe",
                Arguments = $"-i {meta.VideoPath} -vf scale=352:288 {meta.FramesFolderPath}/{meta.Name}_%04d.jpeg",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }

    public static void AnalyzeFrames(Meta meta)
    {
        var frameFiles = Directory.GetFiles(meta.FramesFolderPath, $"{meta.Name}_*.jpeg")
                                  .OrderBy(f => f)
                                  .ToArray();

        Console.WriteLine(frameFiles.Length);

        if (frameFiles.Length < 2) return;

        List<ComparisonResults> diffs = [];
        Mat previousFrame = Cv2.ImRead(frameFiles[0]);

        // Сравниваем каждый кадр с предыдущим
        for (int i = 1; i < frameFiles.Length; i++)
        {
            Mat currentFrame = Cv2.ImRead(frameFiles[i]);

            var diff = new ComparisonResults(previousFrame, currentFrame);
            diffs.Add(diff);
            previousFrame = currentFrame.Clone();
            Console.WriteLine($"{i / 25 / 60:D2}:{i / 25 % 60:D2}:{i % 25:D2}");
        }
        for (int i = 1; i < diffs.Count - 1; i++)
        {
            Console.WriteLine($"{i / 25 / 60:D2}:{i / 25 % 60:D2}:{i % 25:D2}");
            PrintToCsv2(diffs[i - 1], diffs[i], diffs[i + 1], i, frameFiles[i]); // Запись в файл результатов сравнения
        }
    }

    // Структура для хранения результатов сравнения
    public struct ComparisonResults(Mat previousFrame, Mat currentFrame)
    {
        public double PixelDifference { get; set; }      = CompareByPixelDifference(previousFrame, currentFrame);
        public double MSE { get; set; }                  = CompareByMSE(previousFrame, currentFrame);
        public double SSIM { get; set; } = 0; // PrintTime(() => CompareBySSIM(previousFrame, currentFrame));
        public double HistogramCorrelation { get; set; } = CompareByHistogram(previousFrame, currentFrame);
        public double FeatureMatch { get; set; } = 0; // PrintTime(() => CompareByFeatureMatching(previousFrame, currentFrame));

        private static double PrintTime(Func<double> func)
        {
            var sw = Stopwatch.StartNew();
            double result = func();
            sw.Stop();
            Console.WriteLine($"Время выполнения: {sw.ElapsedMilliseconds} мс");
            return result;
        }

        private static double CompareByPixelDifference(Mat img1, Mat img2)
        {
            Mat diff = new();
            Cv2.Absdiff(img1, img2, diff);
            Scalar sumDiff = Cv2.Sum(diff);
            return sumDiff.Val0 + sumDiff.Val1 + sumDiff.Val2;
        }

        private static double CompareByMSE(Mat img1, Mat img2)
        {
            Mat diff = new();
            Cv2.Absdiff(img1, img2, diff);
            diff = diff.Mul(diff);
            Scalar mse = Cv2.Mean(diff);
            return mse.Val0;
        }

        private static double CompareBySSIM(Mat img1, Mat img2)
        {
            Mat img1Gray = new(), img2Gray = new();
            Cv2.CvtColor(img1, img1Gray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(img2, img2Gray, ColorConversionCodes.BGR2GRAY);
            return Cv2.CompareHist(img1Gray, img2Gray, HistCompMethods.Correl);
        }

        private static double CompareByHistogram(Mat img1, Mat img2)
        {
            Mat hist1 = new(), hist2 = new();
            Cv2.CalcHist(new[] { img1 }, new[] { 0 }, null, hist1, 1, new[] { 256 }, new Rangef[] { new(0, 256) });
            Cv2.CalcHist(new[] { img2 }, new[] { 0 }, null, hist2, 1, new[] { 256 }, new Rangef[] { new(0, 256) });
            return Cv2.CompareHist(hist1, hist2, HistCompMethods.Correl);
        }

        private static double CompareByFeatureMatching(Mat img1, Mat img2)
        {
            var orb = ORB.Create();
            Mat desc1 = new(), desc2 = new();
            orb.DetectAndCompute(img1, null, out KeyPoint[] _, desc1);
            orb.DetectAndCompute(img2, null, out KeyPoint[] _, desc2);
            var bf = new BFMatcher(NormTypes.Hamming, crossCheck: true);
            return bf.Match(desc1, desc2).Length;
        }
    }

    // Метод для записи результатов в файл
    private static void PrintToCsv(ComparisonResults diff, int frameIndex, string framePath)
    {
        string logPath = "comparison_results.csv";
        string timecodesPath = "timecodes.csv";

        using StreamWriter logWriter = new(logPath, true);
        using StreamWriter timecodeWriter = new(timecodesPath, true);
        
        if (frameIndex < 1)
            logWriter.WriteLine($"Индекс,Путь,Разность пиксилей,MSE,SSIM,Корреляция гистограм,Ключевые точки");

        logWriter.WriteLine($"{frameIndex},{framePath},{diff.PixelDifference},{diff.MSE},{diff.SSIM},{diff.HistogramCorrelation},{diff.FeatureMatch}");

    }
    // Метод для записи результатов в файл
    private static void PrintToCsv2(ComparisonResults previousDiff, ComparisonResults currentDiff, ComparisonResults nextDiff, int frameIndex, string framePath)
    {
        string timecodesPath = "timecodes.csv";

        using StreamWriter timecodeWriter = new(timecodesPath, true);

        double App_PixelDifference = 0;
        double App_MSE = 0;
        double App_SSIM = 0;
        double App_HistogramCorrelation = 0;
        double App_FeatureMatch = 0;


        if (frameIndex == 1)
            timecodeWriter.WriteLine($"Индекс;" +                               // 0
                                     $"Путь;" +                                 // 1
                                     $"Разность пиксилей;" +                    // 2
                                     $"App_Разность пиксилей;" +                // 3
                                     $"MSE;" +                                  // 4
                                     $"App_MSE;" +                              // 5
                                     $"SSIM;" +                                 // 6
                                     $"App_SSIM;" +                             // 7
                                     $"Корреляция гистограм;" +                 // 8
                                     $"App_Корреляция гистограм;" +             // 9
                                     $"Ключевые точки;" +                       // 10
                                     $"App_Ключевые точки");                    // 11

        if (frameIndex > 0)
        {
            App_PixelDifference      = (previousDiff.PixelDifference + currentDiff.PixelDifference + nextDiff.PixelDifference) / 3;
            App_MSE                  = (previousDiff.MSE + currentDiff.MSE + nextDiff.MSE) / 3; ;
            App_SSIM                 = (previousDiff.SSIM + currentDiff.SSIM + nextDiff.SSIM) / 3;    ;
            App_HistogramCorrelation = (previousDiff.HistogramCorrelation + currentDiff.HistogramCorrelation + nextDiff.HistogramCorrelation) / 3;    ;
            App_FeatureMatch         = (previousDiff.FeatureMatch + currentDiff.FeatureMatch + nextDiff.FeatureMatch) / 3;    ;
        }

        timecodeWriter.WriteLine($"{frameIndex};" +                             // 0
                                 $"{framePath};" +                              // 1
                                 $"{currentDiff.PixelDifference};" +            // 2
                                 $"{App_PixelDifference};" +                    // 3
                                 $"{currentDiff.MSE};" +                        // 4
                                 $"{App_MSE};" +                                // 5
                                 $"{currentDiff.SSIM};" +                       // 6
                                 $"{App_SSIM};" +                               // 7
                                 $"{currentDiff.HistogramCorrelation};" +       // 8
                                 $"{App_HistogramCorrelation};" +               // 9
                                 $"{currentDiff.FeatureMatch};" +               // 10
                                 $"{App_FeatureMatch}");                        // 11

    }

    public static void AlterVar(Meta meta)
    {
        double CompareByPixelDifference(Mat img1, Mat img2)
        {
            Mat diff = new();
            Cv2.Absdiff(img1, img2, diff);
            Cv2.CvtColor(diff, diff, ColorConversionCodes.BGR2GRAY);
            Scalar mean = Cv2.Mean(diff);
            return mean.Val0;
        }
        void PrintToCsv(double previousDiff, double currentDiff, double nextDiff, int frameIndex, int fps)
        {
            string timecodesPath = "timecodes.csv";

            using StreamWriter timecodeWriter = new(timecodesPath, true);

            if (frameIndex == 1)
            {
                timecodeWriter.WriteLine($"Индекс;" +
                                         $"Таймкод;" +
                                         $"Разность пиксилей;" +
                                         $"App_Разность пиксилей;" +
                                         $"Разность;");
                timecodeWriter.WriteLine($"1" +
                                         $"00:00:00:00;" +
                                         $"0;" +
                                         $"0;" +
                                         $"0;");
                return;
            }

            double app = (previousDiff + currentDiff + nextDiff) / 3;
            double diffDiff = currentDiff / app;
            string timecode = $"00:{frameIndex / fps / 60:d2}:{frameIndex / fps % 60:d2}:{frameIndex % fps:d2}";


            timecodeWriter.WriteLine($"{frameIndex};" +
                                     $"{timecode};" +
                                     $"{currentDiff};" +
                                     $"{app};" +
                                     $"{diffDiff};");

        }

        using VideoCapture videoCapture = new(meta.VideoPath);
        int fps = (int)videoCapture.Fps;
        int frameCount = videoCapture.FrameCount;

        Mat prevFrame = new();
        Mat currentFrame = new();

        videoCapture.Read(currentFrame);
        prevFrame = currentFrame.Clone();

        List<double> diffs = [ 0 ];

        while (videoCapture.Read(currentFrame))
        {
            if (currentFrame.Empty())
                break;

            diffs.Add(CompareByPixelDifference(prevFrame, currentFrame));

            prevFrame = currentFrame.Clone();
        }

        for (int i = 1; i < frameCount - 1; i++)
            PrintToCsv(diffs[i - 1], diffs[i], diffs[i + 1], i, fps);
    }
}