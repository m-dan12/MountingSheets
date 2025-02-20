using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MountingSheets;

/*

// ПРИМЕНЕНИЕ ЭТОГО ГОВНА

List<string> inputFilePathes = [ @"C:\Users\m-dan\Downloads\Telegram Desktop\Я такой какой есть.csv",
                                         @"C:\Users\m-dan\Downloads\Telegram Desktop\На воре и шапка горит.csv",
                                         @"C:\Users\m-dan\Downloads\Telegram Desktop\Все к лучшему.csv",
                                         @"C:\Users\m-dan\Downloads\Telegram Desktop\Как зоолог зоологу.csv",
                                         @"C:\Users\m-dan\Downloads\Telegram Desktop\Призвание.csv",
                                         @"C:\Users\m-dan\Downloads\Telegram Desktop\Ау люди.csv"
           ];
List<string> outputFilePathes = [];
        foreach (string inputFile in inputFilePathes)
        {
            string name = inputFile.Split('\\').Last().Replace(".csv", "");
outputFilePathes.Add(inputFile.Replace(name, name + "_out"));

        }
        for (int i = 0; i < inputFilePathes.Count; i++)
{
    Timcode_1.TimecodeMinus1(inputFilePathes[i], outputFilePathes[i]);
}

*/


public class Timcode_1
{
    const int FramesPerSecond = 25;

    public static void TimecodeMinus1(string inputFilePath, string outputFilePath)
    {

        try
        {
            // Открываем файл для чтения и записи
            using (var reader = new StreamReader(inputFilePath))
            using (var writer = new StreamWriter(outputFilePath))
            {
                // Читаем и пишем заголовок
                string header = reader.ReadLine();
                writer.WriteLine(header);

                // Обрабатываем строки
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var columns = line.Split(';');
                    if (columns.Length < 3)
                    {
                        Console.WriteLine($"Строка имеет неверный формат: {line}");
                        continue;
                    }

                    // Считываем и изменяем таймкод конца монтажного кадра
                    string endTimecode = columns[2].Trim();
                    string newEndTimecode = SubtractOneFrame(endTimecode);
                    columns[2] = newEndTimecode;

                    // Записываем обновленную строку
                    writer.WriteLine(string.Join(";", columns));
                }
            }

            Console.WriteLine($"Обработка завершена. Результат записан в файл: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }

    static string SubtractOneFrame(string timecode)
    {
        // Парсим таймкод
        var parts = timecode.Split(':');
        if (parts.Length != 4) throw new FormatException($"Неверный формат таймкода: {timecode}");

        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        int seconds = int.Parse(parts[2]);
        int frames = int.Parse(parts[3]);

        // Вычитаем один кадр
        frames -= 1;
        if (frames < 0)
        {
            frames = FramesPerSecond - 1;
            seconds -= 1;

            if (seconds < 0)
            {
                seconds = 59;
                minutes -= 1;

                if (minutes < 0)
                {
                    minutes = 59;
                    hours -= 1;

                    if (hours < 0)
                        throw new InvalidOperationException("Таймкод не может быть меньше 00:00:00:00");
                }
            }
        }

        // Форматируем результат
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frames:D2}";
    }
}
