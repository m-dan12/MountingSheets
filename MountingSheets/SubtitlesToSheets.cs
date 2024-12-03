using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using System.Linq;
using System.Runtime.Intrinsics.Wasm;

namespace MountingSheets;

public class SceneSubtitleMatcher
{
    private int frameRate = 25;

    // Конструктор для задания частоты кадров (по умолчанию 25)
    public SceneSubtitleMatcher(int frameRate = 25)
    {
        this.frameRate = frameRate;
    }

    // Метод для преобразования таймкода (чч:мм:сс:кк) в миллисекунды
    private int ConvertTimecodeToMilliseconds(string timecode)
    {
        var parts = timecode.Split(':');
        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(parts[2]);
        int frames = int.Parse(parts[3]);

        int totalMilliseconds = (hours * 3600 + minutes * 60 + seconds) * 1000 + (frames * 1000) / frameRate;
        return totalMilliseconds;
    }

    // Метод для преобразования таймкода (чч:мм:сс,мс) в миллисекунды
    private int ConvertSRTTimecodeToMilliseconds(string timecode)
    {
        var parts = timecode.Split(new[] { ':', ',' });
        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(parts[2]);
        int milliseconds = int.Parse(parts[3]);

        return (hours * 3600 + minutes * 60 + seconds) * 1000 + milliseconds;
    }

    // Основной метод для сопоставления сцен и субтитров
    public void MatchScenesWithSubtitles(string excelPath, string srtPath, string outputExcelPath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        // Читаем Excel-файл
        var scenes = new List<Scene>();
        using (var package = new ExcelPackage(new FileInfo(excelPath)))
        {
            var worksheet = package.Workbook.Worksheets.First();
            int rows = worksheet.Dimension.End.Row;

            for (int i = 3; i <= rows; i++) // Предполагается, что 1-я строка - заголовок
            {
                var scene = new Scene
                {
                    Number = int.Parse(worksheet.Cells[i, 1].Text),
                    StartMilliseconds = ConvertTimecodeToMilliseconds(worksheet.Cells[i, 2].Text),
                    EndMilliseconds = ConvertTimecodeToMilliseconds(worksheet.Cells[i, 3].Text),
                    Plan = worksheet.Cells[i, 4].Text,
                    Description = worksheet.Cells[i, 5].Text,
                    Dialogues = ""
                };
                scenes.Add(scene);
            }
        }

        // Читаем SRT-файл
        var subtitles = new List<Subtitle>();
        var srtLines = File.ReadAllLines(srtPath);
        for (int i = 0; i < srtLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(srtLines[i]))
                continue;

            var subtitle = new Subtitle
            {
                Number = int.Parse(srtLines[i]),
                StartMilliseconds = ConvertSRTTimecodeToMilliseconds(srtLines[i + 1].Split(" --> ")[0]),
                EndMilliseconds = ConvertSRTTimecodeToMilliseconds(srtLines[i + 1].Split(" --> ")[1]),
                Text = srtLines[i + 2]
            };
            subtitles.Add(subtitle);
            i += 2; // Пропустить следующую строку
        }

        // Сопоставляем субтитры сценам
        foreach (var subtitle in subtitles)
        {
            Scene bestMatch = null;
            int maxOverlap = 0;

            foreach (var scene in scenes)
            {
                int overlap = Math.Min(scene.EndMilliseconds, subtitle.EndMilliseconds) -
                              Math.Max(scene.StartMilliseconds, subtitle.StartMilliseconds);

                if (overlap > maxOverlap)
                {
                    maxOverlap = overlap;
                    bestMatch = scene;
                }
            }

            if (bestMatch != null)
            {
                bestMatch.Dialogues += subtitle.Text + Environment.NewLine;
            }
        }

        // Сохраняем результат в новый Excel-файл
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Scenes");
            worksheet.Cells[1, 1].Value = "Номер сцены";
            worksheet.Cells[1, 2].Value = "Таймкод начала";
            worksheet.Cells[1, 3].Value = "Таймкод конца";
            worksheet.Cells[1, 4].Value = "План";
            worksheet.Cells[1, 5].Value = "Описание";
            worksheet.Cells[1, 6].Value = "Диалоги";

            int row = 2;
            foreach (var scene in scenes)
            {
                worksheet.Cells[row, 1].Value = scene.Number;
                worksheet.Cells[row, 2].Value = scene.StartMilliseconds;
                worksheet.Cells[row, 3].Value = scene.EndMilliseconds;
                worksheet.Cells[row, 4].Value = scene.Plan;
                worksheet.Cells[row, 5].Value = scene.Description;
                worksheet.Cells[row, 6].Value = scene.Dialogues.Replace("<b>", "\n").Replace("</b>","").Trim();
                row++;
            }

            package.SaveAs(new FileInfo(outputExcelPath));
        }

        Console.WriteLine("Готово! Файл сохранен: " + outputExcelPath);
    }

    // Классы для хранения данных о сценах и субтитрах
    private class Scene
    {
        public int Number { get; set; }
        public int StartMilliseconds { get; set; }
        public int EndMilliseconds { get; set; }
        public string Plan { get; set; }
        public string Description { get; set; }
        public string Dialogues { get; set; }
    }

    private class Subtitle
    {
        public int Number { get; set; }
        public int StartMilliseconds { get; set; }
        public int EndMilliseconds { get; set; }
        public string Text { get; set; }
    }
}