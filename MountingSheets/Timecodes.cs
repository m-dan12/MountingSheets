using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MountingSheets;

internal class SceneChangeDetection
{
    // Структура для хранения результатов метода и времени выполнения
    public struct ComparisonResults(Mat previousFrame, Mat currentFrame)
    {
        public double? CompareByPixelDifference { get; set; } = MeasureComparison("Разница по пикселям", () => CompareByPixelDifference(previousFrame, currentFrame));
        public double? CompareByMSE { get; set; } = MeasureComparison("Среднеквадратичное отклонение (MSE)", () => CompareByMSE(previousFrame, currentFrame));
        public double? CompareBySSIM { get; set; } //= MeasureComparison("Структурное сходство (SSIM)", () => CompareBySSIM(previousFrame, currentFrame));
        public double? CompareByHistogram { get; set; } = MeasureComparison("Гистограммное сравнение", () => CompareByHistogram(previousFrame, currentFrame));
        public double? CompareByFeatureMatching { get; set; } //= MeasureComparison("Ключевые точки и дескрипторы", () => CompareByFeatureMatching(previousFrame, currentFrame));
    }
    public static void Hren(string videoPath)
    {
        using VideoCapture videoCapture = new(videoPath);

        Mat previousFrame = new(), currentFrame = new();

        videoCapture.Read(currentFrame);
        currentFrame.CopyTo(previousFrame);

        List<ComparisonResults> diffs = [];

        int i = 1;

        while (videoCapture.Read(currentFrame))
        {
            if (currentFrame.Empty())
                break;

            var diff = new ComparisonResults(previousFrame, currentFrame);
            diffs.Add(diff);

            Console.WriteLine("-------------");
            Console.WriteLine($"Вывод: {diff.CompareByPixelDifference:f6} {diff.CompareByMSE:f6} {diff.CompareByHistogram:f6}");
            Console.WriteLine("-------------");

            currentFrame.CopyTo(previousFrame);

            i++;
        }
    }
    // Вспомогательный метод для измерения времени и вывода результата сравнения
    public static double MeasureComparison(string methodName, Func<double> comparisonMethod)
    {
        Stopwatch sw = Stopwatch.StartNew();
        double result = comparisonMethod();
        sw.Stop();
        Console.WriteLine($"{methodName}: результат = {result}, время: {sw.ElapsedMilliseconds} мс");
        return result;
    }

    // 1. Абсолютное отклонение (разность пикселей)
    public static double CompareByPixelDifference(Mat img1, Mat img2)
    {
        Mat diff = new();
        Cv2.Absdiff(img1, img2, diff);
        Scalar sumDiff = Cv2.Sum(diff);
        return sumDiff.Val0 + sumDiff.Val1 + sumDiff.Val2;
    }

    // 2. Среднеквадратичное отклонение (MSE)
    public static double CompareByMSE(Mat img1, Mat img2)
    {
        Mat diff = new();
        Cv2.Absdiff(img1, img2, diff);
        diff = diff.Mul(diff);
        Scalar mse = Cv2.Mean(diff);
        return mse.Val0;
    }

    // 3. Индекс структурного сходства (SSIM)
    public static double CompareBySSIM(Mat img1, Mat img2)
    {
        Mat img1Gray = new(), img2Gray = new();
        Cv2.CvtColor(img1, img1Gray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(img2, img2Gray, ColorConversionCodes.BGR2GRAY);
        return Cv2.CompareHist(img1Gray, img2Gray, HistCompMethods.Correl); // ИСКЛЮЧЕНИЕ
    }

    // 4. Гистограммное сравнение
    public static double CompareByHistogram(Mat img1, Mat img2)
    {
        Mat hist1 = new(), hist2 = new();
        Cv2.CalcHist([img1], [0], null, hist1, 1, [256], new Rangef[] { new(0, 256) });
        Cv2.CalcHist([img2], [0], null, hist2, 1, [256], new Rangef[] { new(0, 256) });
        return Cv2.CompareHist(hist1, hist2, HistCompMethods.Correl); // Коэффициент корреляции гистограмм
    }

    // 5. Сравнение ключевых точек
    public static double CompareByFeatureMatching(Mat img1, Mat img2)
    {   
        var orb = ORB.Create();
        Mat desc1 = new(), desc2 = new();
        orb.DetectAndCompute(img1, null, out KeyPoint[] _, desc1);
        orb.DetectAndCompute(img2, null, out KeyPoint[] _, desc2);
        var bf = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        return bf.Match(desc1, desc2).Length; // ИНОГДА ИСКЛЮЧЕНИЕ
    }

}