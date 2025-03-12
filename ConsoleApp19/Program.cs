using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;
using Vosk;
using Newtonsoft.Json.Linq;
using WindowsInput;
using WindowsInput.Native;
using NAudio.CoreAudioApi;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

class Program
{
    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern void SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    struct RECT
    {
        public int Left, Top, Right, Bottom;
    }


    private const byte VK_SPACE = 0x20;  // Space
    private const byte VK_F = 0x46;      // F
    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;

    static readonly string jsonFilePath = "applications.json";
    static Dictionary<int, string> ProggrammDirectory = new Dictionary<int, string>();
    static async Task Main(string[] args)
    {
        try
        {
            string appPath = @"C:\Users\gvyu3\AppData\Roaming\Telegram Desktop\Telegram.exe";
            string Proksi = @"C:\Program Files\Hiddify\Hiddify.exe";
            string Discord = @"C:\Users\gvyu3\AppData\Local\Discord\app-1.0.9180\Discord.exe";
            string taskmgr = @"C:\Windows\System32\taskmgr.exe";
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available Bytes");
            PerformanceCounter diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            PerformanceCounter processCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
            PerformanceCounter processMemoryCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
            PerformanceCounter totalRamCounter = new PerformanceCounter("Memory", "Committed Bytes");
            PerformanceCounter diskReadProcess = new PerformanceCounter("Process", "IO Read Bytes/sec", Process.GetCurrentProcess().ProcessName);
            PerformanceCounter diskWriteProcess = new PerformanceCounter("Process", "IO Write Bytes/sec", Process.GetCurrentProcess().ProcessName);
            PerformanceCounter processCountCounter = new PerformanceCounter("System", "Processes");
            PerformanceCounter threadsCounter = new PerformanceCounter("System", "Threads");
            PerformanceCounter contextSwitches = new PerformanceCounter("System", "Context Switches/sec");

            // Выбор языка перед запуском команд
            Console.WriteLine("Выберите язык: 1 - Русский, 2 - English");
            string langChoice = Console.ReadLine();
            string lang = langChoice == "2" ? "en" : "ru";

            // Выбор системы
            Console.WriteLine("Выберите системы: 1 - Голосовой, 2 - Текстовый");
            string SystemChoice = Console.ReadLine();

            // Словарь переводов
            Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
            {
                {"ru", new Dictionary<string, string>()
                    {
                        {"enter_command", "Введите команду:"},
                        {"help_for_error", "Команда не распознана. Введите 'Помощь' для списка доступных команд."},
                        {"help", "Доступные команды:"},
                        {"open_telegram", "Открой Телеграмм (или введите '1')"},
                        {"open_proxy", "Открой Прокси (или введите '2')"},
                        {"open_discord", "Открой Дискорд (или введите '3')"},
                        {"open_browser", "Открой Браузер (или введите '4')"},
                        {"brightness", "Открыть параметры яркости (или введите '5')"},
                        {"sound", "Открыть параметры звука (или введите '6')"},
                        {"task_manager", "Открыть Диспетчер задач (или введите '7')"},
                        {"show_time", "Покажи время (или введите '8')"},
                        {"show_weather", "Покажи погоду (или введите '9')"},
                        {"add_path", "Добавить путь к приложению (или введите '10')"},
                        {"show_apps", "Показать все приложения (или введите '11')"},
                        {"run_saved", "Запуск сохраненного приложения (или введите '12')"},
                        {"work_txt", "Работа с txt (или введите '13')"},
                        {"work_files", "Работа с файлами ПК (или введите '14')"},
                        {"password_gen", "Генератор паролей (или введите '15')"},
                        {"pc_info", "Вывод информации о ПК (или введите '16')"}
                    }
                },
                {"en", new Dictionary<string, string>()
                    {
                        {"enter_command", "Enter command:"},
                        {"help_for_error", "Command not recognized. Enter 'Help' for a list of available commands."},
                        {"help", "Available commands:"},
                        {"open_telegram", "Open Telegram (or enter '1')"},
                        {"open_proxy", "Open Proxy (or enter '2')"},
                        {"open_discord", "Open Discord (or enter '3')"},
                        {"open_browser", "Open Browser (or enter '4')"},
                        {"brightness", "Open brightness settings (or enter '5')"},
                        {"sound", "Open sound settings (or enter '6')"},
                        {"task_manager", "Open Task Manager (or enter '7')"},
                        {"show_time", "Show time (or enter '8')"},
                        {"show_weather", "Show weather (or enter '9')"},
                        {"add_path", "Add path to application (or enter '10')"},
                        {"show_apps", "Show all applications (or enter '11')"},
                        {"run_saved", "Run saved application (or enter '12')"},
                        {"work_txt", "Work with txt (or enter '13')"},
                        {"work_files", "Work with PC files (or enter '14')"},
                        {"password_gen", "Password generator (or enter '15')"},
                        {"pc_info", "Show PC information (or enter '16')"}
                    }
                }
            };

            string logFilePath = "created_files.log";
            string foldersJsonPath = "folders.json";

            // Чтение сохраненных папок
            List<string> savedFolders = File.Exists(foldersJsonPath)
                ? JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(foldersJsonPath)) ?? new List<string>()
                : new List<string>();

            // Добавляем стандартные пути
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            if (!savedFolders.Contains(desktopPath))
                savedFolders.Add(desktopPath);
            if (!savedFolders.Contains(downloadsPath))
                savedFolders.Add(downloadsPath);

            var sim = new InputSimulator();

            if (SystemChoice == "1")
            {
                //Vosk
                // Путь к модели для распознавания речи
                string modelPath = @"C:\Users\gvyu3\OneDrive\Рабочий стол\vosk-model-small-ru-0.22"; // Путь к модели для русского языка

                if (!System.IO.Directory.Exists(modelPath))
                {
                    Console.WriteLine("Ошибка: Папка модели не найдена!");
                    return;
                }

                var model = new Model(modelPath);
                var recognizer = new VoskRecognizer(model, 16000);

                var waveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(16000, 1)
                };

                // Слушаем и распознаем команды
                waveIn.DataAvailable += (sender, e) =>
                {
                    if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        var result = recognizer.Result();
                        var jsonResult = JObject.Parse(result);
                        string text = jsonResult["text"].ToString();

                        Console.WriteLine($"🎤 Распознанный текст: {text}");

                        // Вызов ExecuteCommand
                        ExecuteCommand(text, translations, lang);
                    }
                };

                waveIn.StartRecording();
                Console.WriteLine("🎤 Говорите команду...");

                // Ожидание завершения записи голосовой команды
                await Task.Delay(-1);  // Ожидаем, пока программа не будет завершена вручную

                waveIn.StopRecording();
                recognizer.Dispose();
                /////////////////////////////////
            }
                else if (SystemChoice == "2")
                {
                    while (true)
                    {
                        Console.WriteLine(translations[lang]["enter_command"]);
                        string Command = Console.ReadLine()?.Trim().ToLower() ?? "";
                        bool commandExecuted = false;

                        // Помощь
                        if (Command == "помощь" || Command == "help")
                    {
                        Console.WriteLine(translations[lang]["help"]);
                        Console.WriteLine($"1. {translations[lang]["open_telegram"]}");
                        Console.WriteLine($"2. {translations[lang]["open_proxy"]}");
                        Console.WriteLine($"3. {translations[lang]["open_discord"]}");
                        Console.WriteLine($"4. {translations[lang]["open_browser"]}");
                        Console.WriteLine($"5. {translations[lang]["brightness"]}");
                        Console.WriteLine($"6. {translations[lang]["sound"]}");
                        Console.WriteLine($"7. {translations[lang]["task_manager"]}");
                        Console.WriteLine($"8. {translations[lang]["show_time"]}");
                        Console.WriteLine($"9. {translations[lang]["show_weather"]}");
                        Console.WriteLine($"10. {translations[lang]["add_path"]}");
                        Console.WriteLine($"11. {translations[lang]["show_apps"]}");
                        Console.WriteLine($"12. {translations[lang]["run_saved"]}");
                        Console.WriteLine($"13. {translations[lang]["work_txt"]}");
                        Console.WriteLine($"14. {translations[lang]["work_files"]}");
                        Console.WriteLine($"15. {translations[lang]["password_gen"]}");
                        Console.WriteLine($"16. {translations[lang]["pc_info"]}");
                        commandExecuted = true;
                    }
                    // Telegram
                    else if (Command == "Открой Телеграмм" || Command == "1")
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = appPath,
                            UseShellExecute = true,
                        };
                        Process.Start(processInfo);
                        commandExecuted = true;
                    }
                    // Proksi
                    else if (Command == "Открой Прокси" || Command == "2")
                    {
                        var proksiZapusk = new ProcessStartInfo
                        {
                            FileName = Proksi,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(proksiZapusk);
                        commandExecuted = true;
                    }
                    // Discord
                    else if (Command == "Открой Дискорд" || Command == "3")
                    {
                        var DS = new ProcessStartInfo
                        {
                            FileName = Discord,
                            UseShellExecute = true,
                        };
                        Process.Start(DS);
                        commandExecuted = true;
                    }
                    // Браузер
                    else if (Command == "Открой Браузер" || Command == "4")
                    {
                        Console.Clear();
                        Console.WriteLine("Введите запрос для поиска в браузере:");
                        string query = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            string encodedQuery = Uri.EscapeDataString(query);
                            Console.WriteLine("Выберите, где будем искать: 1. YouTube 2. Google 3. Yandex");

                            string query1 = Console.ReadLine()?.Trim();

                            if (query1 == "1") // YouTube
                            {
                                string searchUrl = $"https://www.youtube.com/results?search_query={encodedQuery}";
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = searchUrl,
                                        UseShellExecute = true
                                    });
                                    commandExecuted = true;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка при открытии браузера: {ex.Message}");
                                }
                            }
                            else if (query1 == "2") // Google
                            {
                                string searchUrl = $"https://www.google.com/search?q={encodedQuery}";
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = searchUrl,
                                        UseShellExecute = true
                                    });
                                    commandExecuted = true;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка при открытии браузера: {ex.Message}");
                                }
                            }
                            else if (query1 == "3") // Yandex
                            {
                                string searchUrl = $"https://yandex.ru/search/?text={encodedQuery}";
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = searchUrl,
                                        UseShellExecute = true
                                    });
                                    commandExecuted = true;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка при открытии браузера: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ошибка: Неверный выбор поисковой системы.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ошибка: Пустой запрос.");
                        }
                    }
                    // Яркость
                    else if (Command == "Открыть параметры яркости" || Command == "5")
                    {
                        Console.Clear();
                        Console.WriteLine("Выбирите варинант процента ярксти 1.100% 2.50% 3.0%");
                        string light = Console.ReadLine();

                        if (light == "1")
                        {
                            //100%
                            sim.Mouse.MoveMouseTo(1700 * 65535 / 1920, 1050 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                            Thread.Sleep(500);
                            sim.Mouse.MoveMouseTo(1830 * 65535 / 1920, 845 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                        }

                        if (light == "2")
                        {
                            //50%
                            sim.Mouse.MoveMouseTo(1700 * 65535 / 1920, 1050 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                            Thread.Sleep(500);
                            sim.Mouse.MoveMouseTo(1685 * 65535 / 1920, 845 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                        }

                        if (light == "3")
                        {
                            //0%
                            sim.Mouse.MoveMouseTo(1700 * 65535 / 1920, 1050 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                            Thread.Sleep(500);
                            sim.Mouse.MoveMouseTo(1535 * 65535 / 1920, 845 * 65535 / 1080);
                            Thread.Sleep(500);
                            sim.Mouse.LeftButtonClick();
                        }
                    }
                    // Звук
                    else if (Command == "Открыть параметры звука" || Command == "6")
                    {
                        Console.Clear();
                        Console.WriteLine("Введите громкость (0 - 100):");
                        if (float.TryParse(Console.ReadLine(), out float volume))
                        {
                            SetVolume(volume);
                        }
                        else
                        {
                            Console.WriteLine("Некорректный ввод.");
                        }
                    }
                    // Диспечер задач
                    else if (Command == "Отрокрой Диспечер задач" || Command == "7")
                    {
                        var taskmgr1 = new ProcessStartInfo
                        {
                            FileName = taskmgr,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(taskmgr1);
                    }
                    // Время
                    else if (Command == "Покажи время" || Command == "8")
                    {
                        Console.Clear();
                        while (true)
                        {
                            DateTime currentTime = DateTime.Now;

                            Console.Clear(); // Очистка экрана для обновления времени
                            Console.WriteLine("Текущее время: " + currentTime.ToString("HH:mm:ss"));
                            Console.WriteLine("Полная дата и время: " + currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            Console.WriteLine("Нажмите 'q' для выхода...");

                            // Пауза между обновлениями времени (1 секунда)
                            Thread.Sleep(1000); // обновление каждый 1 секунду

                            // Проверка, не был ли введен 'q' для выхода из цикла
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(intercept: true); // Чтение ввода, но не вывод на экран
                                if (key.Key == ConsoleKey.Q)
                                {
                                    break; // Выход из цикла при нажатии 'q'
                                }
                            }
                        }

                        commandExecuted = true;
                    }
                    // Погода
                    else if (Command == "Покажи погоду" || Command == "9")
                    {
                        await MainPogoda();
                        commandExecuted = true;
                    }
                    // Добавить путь к приложению
                    else if (Command == "Добавить путь к приложению" || Command == "10")
                    {
                        Console.WriteLine("Введите путь к приложению:");
                        string addProgramm = Console.ReadLine();
                        int key = ProggrammDirectory.Count + 1; 
                        ProggrammDirectory.Add(key, addProgramm);
                        SaveApplications();
                        Console.WriteLine("Приложение добавлено и сохранено.");
                        //commandExecuted = true;
                    }
                    // Показать все приложения
                    else if (Command == "Показать все приложения" || Command == "11")
                    {
                        Console.WriteLine("Сохраненные пути к приложениям:");
                        foreach (var entry in ProggrammDirectory)
                        {
                            Console.WriteLine($"[{entry.Key}] - {entry.Value}");
                        }
                        //commandExecuted = true;
                    }
                    // Запуск сохраненного приложения
                    else if (Command == "Запуск сохраненного приложения" || Command == "12")
                    {
                        Console.WriteLine("Выберите номер приложения для запуска:");
                        foreach (var entry in ProggrammDirectory)
                        {
                            Console.WriteLine($"[{entry.Key}] - {entry.Value}");
                        }

                        if (int.TryParse(Console.ReadLine(), out int selectedKey))
                        {
                            if (ProggrammDirectory.TryGetValue(selectedKey, out string path))
                            {
                                if (File.Exists(path))
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = path,
                                        UseShellExecute = true
                                    });
                                    Console.WriteLine("Приложение запущено.");
                                }
                                else
                                {
                                    Console.WriteLine("Ошибка: Указанный путь не существует.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ошибка: Неверный выбор.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ошибка: Введите корректный номер.");
                        }
                        //commandExecuted = true;
                    }
                    //работа с txt файлами
                    else if (Command == "13" || Command == "Работа с txt")
                    {
                        while (true) // Цикл продолжится, пока не будет введен правильный путь или команда "выход"
                        {
                            Console.WriteLine("Укажите путь до txt файла (или введите 'выход' для отмены):");
                            string pathToFile = Console.ReadLine();

                            if (pathToFile.ToLower() == "выход")
                            {
                                Console.Clear();
                                break; // Прерываем цикл, если пользователь вводит "выход"
                            }

                            if (Path.GetExtension(pathToFile) == ".txt")
                            {
                                if (!File.Exists(pathToFile))
                                {
                                    Console.WriteLine("Ошибка: Файл не найден. Проверьте путь и попробуйте снова.");
                                    continue; // Продолжаем запрашивать путь
                                }

                                commandExecuted = false;

                                // Переходим к работе с файлом
                                while (true)
                                {
                                    Console.Clear(); // Очистка консоли перед отображением меню
                                    Console.WriteLine("\nДоступные команды:");
                                    Console.WriteLine("1. Добавление (или введите '1')");
                                    Console.WriteLine("2. Удаление (или введите '2')");
                                    Console.WriteLine("3. Просмотр файла (или введите '3')");
                                    Console.WriteLine("4. Копировать (или введите '4')");
                                    Console.WriteLine("5. Выход (или введите '5')");
                                    Console.Write("Введите команду: ");
                                    string textVarLower = Console.ReadLine()?.ToLower() ?? "";

                                    if (textVarLower == "добавление" || textVarLower == "1")
                                    {
                                        Console.Clear();
                                        Console.WriteLine("Выберите способ добавления: 1. На новую строку 2. В ту же строку");
                                        string addWrite = Console.ReadLine();
                                        if (addWrite == "1")
                                        {
                                            Console.Write("Введите текст для добавления: ");
                                            string inputText = Console.ReadLine();
                                            string content = File.ReadAllText(pathToFile);

                                            if (!string.IsNullOrEmpty(content) && !content.EndsWith(Environment.NewLine))
                                            {
                                                inputText = Environment.NewLine + inputText;
                                            }
                                            File.AppendAllText(pathToFile, inputText + Environment.NewLine);
                                        }
                                        else if (addWrite == "2")
                                        {
                                            string[] lines = File.ReadAllLines(pathToFile);
                                            PrintFileContentWithLineNumbers(lines);

                                            Console.WriteLine("Выберите номер строки для добавления:");
                                            if (int.TryParse(Console.ReadLine(), out int lineNumber) && lineNumber > 0 && lineNumber <= lines.Length)
                                            {
                                                Console.WriteLine("Введите текст для добавления:");
                                                string textToAdd = Console.ReadLine();
                                                lines[lineNumber - 1] += string.IsNullOrWhiteSpace(lines[lineNumber - 1]) ? textToAdd : " " + textToAdd;

                                                File.WriteAllLines(pathToFile, lines);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Ошибка: Неверный номер строки.");
                                            }
                                        }
                                    }
                                    else if (textVarLower == "удаление" || textVarLower == "2")
                                    {
                                        Console.Clear();
                                        Console.WriteLine("Выберите способ удаления: 1. Строку 2. Слово 3. Все");
                                        string deleteOption = Console.ReadLine();

                                        if (deleteOption == "1")
                                        {
                                            string[] lines = File.ReadAllLines(pathToFile);
                                            PrintFileContentWithLineNumbers(lines);

                                            Console.WriteLine("Введите номер строки для удаления:");
                                            if (int.TryParse(Console.ReadLine(), out int lineToRemove) && lineToRemove > 0 && lineToRemove <= lines.Length)
                                            {
                                                lines = lines.Where((line, index) => index != lineToRemove - 1).ToArray();
                                                File.WriteAllLines(pathToFile, lines);
                                                Console.WriteLine("Строка удалена.");
                                            }
                                            else
                                            {
                                                Console.WriteLine("Ошибка: Неверный номер строки.");
                                            }
                                        }
                                        else if (deleteOption == "2")
                                        {
                                            Console.WriteLine("Введите слово для удаления: ");
                                            string wordToRemove = Console.ReadLine();

                                            string[] lines = File.ReadAllLines(pathToFile);
                                            string[] updatedLines = lines.Select(line => line.Replace(wordToRemove, "")).ToArray();

                                            if (!updatedLines.SequenceEqual(lines))
                                            {
                                                File.WriteAllLines(pathToFile, updatedLines);
                                                Console.WriteLine("Слово удалено.");
                                            }
                                            else
                                            {
                                                Console.WriteLine("Ошибка: Слово не найдено.");
                                            }
                                        }
                                        else if (deleteOption == "3")
                                        {
                                            File.WriteAllText(pathToFile, string.Empty);
                                            Console.WriteLine("Файл очищен.");
                                        }
                                    }
                                    else if (textVarLower == "просмотр файла" || textVarLower == "3")
                                    {
                                        Console.Clear();
                                        PrintFileContent(pathToFile);
                                    }
                                    else if (textVarLower == "выход" || textVarLower == "5")
                                    {
                                        Console.Clear();
                                        break; // Выход из меню редактирования файла
                                    }
                                    else
                                    {
                                        Console.WriteLine("Ошибка: Неизвестная команда.");
                                    }
                                }

                                break; // Закрываем цикл ввода пути, если путь был корректным и мы начали работу с файлом
                            }
                            else
                            {
                                Console.WriteLine("Ошибка: Пожалуйста, выберите файл с расширением .txt");
                            }
                        }
                    }
                    //Работа с файлами Пк
                    else if (Command == "14" || Command == "Работа с файлами Пк")
                    {
                        while (true)
                        {
                            Console.WriteLine("\nДоступные команды:");
                            Console.WriteLine("1. Создать папку");
                            Console.WriteLine("2. Создать файл");
                            Console.WriteLine("3. Удалить файл");
                            Console.WriteLine("4. Выход");
                            Console.Write("Введите команду: ");
                            string command = Console.ReadLine();

                            if (command == "1") // Создание папки
                            {
                                Console.WriteLine("Выберите место создания папки:");
                                Console.WriteLine("1. Рабочий стол");
                                Console.WriteLine("2. Загрузки");
                                Console.WriteLine("3. Ввести вручную");
                                Console.Write("Ваш выбор: ");
                                string folderChoice = Console.ReadLine();

                                string folderPath = "";
                                if (folderChoice == "1")
                                {
                                    folderPath = desktopPath;
                                }
                                else if (folderChoice == "2")
                                {
                                    folderPath = downloadsPath;
                                }
                                else if (folderChoice == "3")
                                {
                                    Console.Write("Введите путь к новой папке: ");
                                    folderPath = Console.ReadLine();
                                }
                                else
                                {
                                    Console.WriteLine("Некорректный выбор.");
                                    continue;
                                }

                                Console.Write("Введите название папки: ");
                                string folderName = Console.ReadLine();
                                string fullPath = Path.Combine(folderPath, folderName);

                                if (!Directory.Exists(fullPath))
                                {
                                    Directory.CreateDirectory(fullPath);
                                    savedFolders.Add(fullPath);
                                    File.WriteAllText(foldersJsonPath, JsonConvert.SerializeObject(savedFolders));
                                    Console.WriteLine("Папка создана и сохранена.");
                                }
                                else
                                {
                                    Console.WriteLine("Папка уже существует.");
                                }
                            }
                            else if (command == "2") // Создание файла
                            {
                                Console.WriteLine("Выберите папку:");
                                for (int i = 0; i < savedFolders.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {savedFolders[i]}");
                                }
                                Console.Write("Введите номер папки: ");
                                if (!int.TryParse(Console.ReadLine(), out int folderIndex) || folderIndex < 1 || folderIndex > savedFolders.Count)
                                {
                                    Console.WriteLine("Некорректный выбор.");
                                    continue;
                                }
                                string selectedFolder = savedFolders[folderIndex - 1];

                                Console.Write("Введите название файла: ");
                                string fileName = Console.ReadLine();
                                Console.WriteLine("Выберите тип файла: 1. .txt  2. .bat  3. .rar  4. .zip  5. .docx  6. .xlsx");
                                string[] extensions = { ".txt", ".bat", ".rar", ".zip", ".docx", ".xlsx" };
                                if (!int.TryParse(Console.ReadLine(), out int formatChoice) || formatChoice < 1 || formatChoice > extensions.Length)
                                {
                                    Console.WriteLine("Некорректный формат.");
                                    continue;
                                }
                                string filePath = Path.Combine(selectedFolder, fileName + extensions[formatChoice - 1]);
                                if (File.Exists(filePath))
                                {
                                    Console.WriteLine("Файл уже существует.");
                                    continue;
                                }
                                File.Create(filePath).Close();
                                File.AppendAllText(logFilePath, filePath + Environment.NewLine);
                                Console.WriteLine("Файл создан.");
                            }
                            else if (command == "3") // Удаление файла
                            {
                                Console.WriteLine("1. Удалить любой файл  2. Удалить созданный программой");
                                string choice = Console.ReadLine();
                                if (choice == "1")
                                {
                                    // Логика для удаления любого файла
                                    Console.Write("Введите путь к файлу или папке: ");
                                    string path = Console.ReadLine();

                                    if (File.Exists(path))
                                    {
                                        File.Delete(path);
                                        Console.WriteLine("Файл удален.");
                                        continue;
                                    }
                                    else if (Directory.Exists(path))
                                    {
                                        string[] files = Directory.GetFiles(path);
                                        if (files.Length == 0)
                                        {
                                            Console.WriteLine("В папке нет файлов.");
                                            continue;
                                        }
                                        for (int i = 0; i < files.Length; i++)
                                        {
                                            Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
                                        }
                                        Console.Write("Введите номер файла: ");
                                        if (int.TryParse(Console.ReadLine(), out int fileNum) && fileNum > 0 && fileNum <= files.Length)
                                        {
                                            File.Delete(files[fileNum - 1]);
                                            Console.WriteLine("Файл удален.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Некорректный ввод.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Файл или папка не найдены.");
                                    }
                                }
                                else if (choice == "2") // Удаление созданных файлов
                                {
                                    // Чтение лог-файла с созданными файлами
                                    if (File.Exists(logFilePath))
                                    {
                                        var createdFiles = File.ReadAllLines(logFilePath).ToList();
                                        if (createdFiles.Count == 0)
                                        {
                                            Console.WriteLine("Нет файлов, созданных программой.");
                                            continue;
                                        }

                                        Console.WriteLine("Выберите файл для удаления:");
                                        for (int i = 0; i < createdFiles.Count; i++)
                                        {
                                            Console.WriteLine($"{i + 1}. {Path.GetFileName(createdFiles[i])}");
                                        }

                                        Console.Write("Введите номер файла: ");
                                        if (int.TryParse(Console.ReadLine(), out int fileNum) && fileNum > 0 && fileNum <= createdFiles.Count)
                                        {
                                            string fileToDelete = createdFiles[fileNum - 1];
                                            if (File.Exists(fileToDelete))
                                            {
                                                File.Delete(fileToDelete);
                                                // Удалить запись из лога
                                                createdFiles.RemoveAt(fileNum - 1);
                                                File.WriteAllLines(logFilePath, createdFiles);
                                                Console.WriteLine("Файл удален.");
                                            }
                                            else
                                            {
                                                Console.WriteLine("Файл не найден.");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Некорректный ввод.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Нет файлов, созданных программой.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Некорректный выбор.");
                                }
                            }
                            else if (command == "4")
                            {
                                Console.WriteLine("Выход.");
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Некорректная команда.");
                            }
                        }
                    }
                    //Генератор паролей
                    else if (Command == "15" || Command == "Генератор паролей")
                    {
                        while (true)
                        {
                            Console.WriteLine("Ввведите какой длинны должен быть пароль: ");
                            String Password = Console.ReadLine();
                            Console.WriteLine("Какой сложности должен быть пароль: ");
                            Console.WriteLine("1. Простой (только цифры)");
                            Console.WriteLine("2. Средний (цифры и буквы)");
                            Console.WriteLine("3. Сложный (цифры, буквы, символы)");
                            String ChosePass = Console.ReadLine();

                            if (!int.TryParse(Password, out int length) || length <= 0)
                            {
                                Console.WriteLine("Ошибка: Введите корректную длину пароля (положительное число).\n");
                                continue;
                            }

                            if (Password.ToLower() == "exit") // Возможность выхода
                                break;

                            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                            if (ChosePass == "1")
                                chars = "0123456789";
                            else if (ChosePass == "2")
                                chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                            else if (ChosePass == "3")
                                chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";

                            StringBuilder password = new StringBuilder();
                            Random rnd = new Random();

                            for (int i = 0; i < length; i++)
                            {
                                password.Append(chars[rnd.Next(chars.Length)]);
                            }

                            Console.WriteLine($"Сгенерированный пароль: {password}\n");
                        }
                    }
                    //Вывод информации о ПК
                    else if (Command == "16" || Command == "Вывод информации о ПК")
                    {
                        while (true)
                        {
                            Console.WriteLine("Введите команду:");
                            Console.WriteLine("\nДоступные команды:");
                            Console.WriteLine("1. Мониторинг ресурсов");
                            Console.WriteLine("2. Очистить кэш");
                            Console.WriteLine("3. Выход");
                            String MonitorRead = Console.ReadLine().ToLower();

                            if (MonitorRead == "1" || MonitorRead == "Мониторинг ресурсов")
                            {
                                while (true)
                                {
                                    float cpuUsage = cpuCounter.NextValue();
                                    float ramAvailable = ramCounter.NextValue() / (1024 * 1024);
                                    float diskUsage = diskCounter.NextValue();
                                    float processUsage = processCounter.NextValue();
                                    float processMemoryUsage = processMemoryCounter.NextValue() / (1024 * 1024);
                                    float totalRam = totalRamCounter.NextValue() / (1024 * 1024);
                                    float processDiskRead = diskReadProcess.NextValue() / (1024 * 1024);
                                    float processDiskWrite = diskWriteProcess.NextValue() / (1024 * 1024);
                                    float processCount = processCountCounter.NextValue();
                                    float threadsCount = threadsCounter.NextValue();
                                    float contextSwitchRate = contextSwitches.NextValue();

                                    Console.Clear();
                                    Console.WriteLine($"Загрузка CPU: {cpuUsage:F2}%");
                                    Console.WriteLine($"Свободная RAM: {ramAvailable:F2} MB / {totalRam:F2} MB");
                                    Console.WriteLine($"Загрузка диска: {diskUsage:F2}%");
                                    Console.WriteLine($"Использование CPU процессом: {processUsage:F2}%");
                                    Console.WriteLine($"Память процесса: {processMemoryUsage:F2} MB");
                                    Console.WriteLine($"Чтение процессом: {processDiskRead:F2} MB/s");
                                    Console.WriteLine($"Запись процессом: {processDiskWrite:F2} MB/s");
                                    Console.WriteLine($"Количество процессов: {processCount}");
                                    Console.WriteLine($"Количество потоков: {threadsCount}");
                                    Console.WriteLine($"Переключений контекста: {contextSwitchRate:F2} в секунду");
                                    Console.WriteLine("Нажмите 'q' для выхода...");

                                    Thread.Sleep(1000);

                                    if (Console.KeyAvailable)
                                    {
                                        var key = Console.ReadKey(intercept: true); // Чтение ввода, но не вывод на экран
                                        if (key.Key == ConsoleKey.Q)
                                        {
                                            break; // Выход из цикла при нажатии 'q'
                                        }
                                    }
                                }
                            }

                            else if (MonitorRead == "2" || MonitorRead == "Очистить кэш")
                            {
                                string tempPath = Path.GetTempPath();  // Локальный Temp текущего пользователя
                                string winTempPath = @"C:\Windows\Temp"; // Системный Temp

                                CleanTempFiles(tempPath);
                                CleanTempFiles(winTempPath);

                                Console.WriteLine("Очистка временных файлов завершена!");
                            }

                            else if (MonitorRead == "3" || MonitorRead == "Выход")
                            {
                                break;
                            }
                        }
                    }
                    //ютуб
                    else if (Command == "17" || Command == "Ютуб")
                    {
                        Console.Write("Введите запрос для поиска на YouTube: ");
                        string query = Console.ReadLine();

                        string videoUrl = SearchYouTube(query);
                        if (videoUrl != null)
                        {
                            Console.WriteLine($"Открываю видео: {videoUrl}");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = videoUrl,
                                UseShellExecute = true
                            });

                            Thread.Sleep(5000); // Ждём загрузку браузера

                            while (true)
                            {
                                Console.WriteLine("\nВыберите действие:");
                                Console.WriteLine("1 - Play/Pause");
                                Console.WriteLine("2 - Полный экран");
                                Console.WriteLine("0 - Выход");
                                Console.Write("Ваш выбор: ");
                                string input = Console.ReadLine();

                                IntPtr browserHandle = FindYouTubeWindow();
                                if (browserHandle != IntPtr.Zero)
                                {
                                    SetForegroundWindow(browserHandle);
                                    Thread.Sleep(500);

                                    ClickWindowCenter(browserHandle); // Кликаем в центр окна
                                    Thread.Sleep(200);

                                    switch (input)
                                    {
                                        case "1":
                                            SendKey(VK_SPACE);
                                            Console.WriteLine(" Воспроизведение / Пауза");
                                            break;
                                        case "2":
                                            SendKey(VK_F);
                                            Console.WriteLine("⛶ Полный экран");
                                            break;
                                        case "0":
                                            return;
                                        default:
                                            Console.WriteLine("Неверная команда");
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Не удалось найти окно браузера с YouTube.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Видео не найдено.");
                        }
                    }
                    // Если команда не распознана
                    if (!commandExecuted)
                    {
                        Console.WriteLine($"{translations[lang]["help_for_error"]}");
                    }
                    // Список команд, при которых не нужно очищать консоль
                    var noClearCommands = new HashSet<string>
                {
                    "помощь",
                    "help",
                    "Покажи время",
                    "8",
                    "11",
                    "12",
                    "13",
                    "16",
                    "Вывод информации о ПК",
                    "Показать все приложения",
                    "Запуск сохраненного приложения",
                    "Работа с txt"
                };

                    // Очищаем консоль, если команда не входит в список исключений
                    if (commandExecuted && !noClearCommands.Contains(Command))
                    {
                        Console.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}"); // Вывод подробной информации о месте ошибки
        }
    }
    static void ExecuteCommand(string text, Dictionary<string, Dictionary<string, string>> translations, string lang)
    {
        bool commandExecuted = false;

        // Помощь
        if (text.ToLower().Contains("") || text.ToLower().Contains(""))
        {
            Console.WriteLine(translations[lang]["help"]);
            Console.WriteLine($"1. {translations[lang]["open_telegram"]}");
            Console.WriteLine($"2. {translations[lang]["open_proxy"]}");
            Console.WriteLine($"3. {translations[lang]["open_discord"]}");
            Console.WriteLine($"4. {translations[lang]["open_browser"]}");
            Console.WriteLine($"5. {translations[lang]["brightness"]}");
            Console.WriteLine($"6. {translations[lang]["sound"]}");
            Console.WriteLine($"7. {translations[lang]["task_manager"]}");
            Console.WriteLine($"8. {translations[lang]["show_time"]}");
            Console.WriteLine($"9. {translations[lang]["show_weather"]}");
            Console.WriteLine($"10. {translations[lang]["add_path"]}");
            Console.WriteLine($"11. {translations[lang]["show_apps"]}");
            Console.WriteLine($"12. {translations[lang]["run_saved"]}");
            Console.WriteLine($"13. {translations[lang]["work_txt"]}");
            Console.WriteLine($"14. {translations[lang]["work_files"]}");
            Console.WriteLine($"15. {translations[lang]["password_gen"]}");
            Console.WriteLine($"16. {translations[lang]["pc_info"]}");
        }
    }
    //Используется для упрощения работы со звуком
    static void SetVolume(float volume)
    {
        if (volume < 0 || volume > 100)
        {
            Console.WriteLine("Значение должно быть от 0 до 100.");
            return;
        }

        var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        device.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100.0f;
        Console.WriteLine($"Громкость установлена на {volume}%");
    }

    // Загрузка приложений из JSON-файла
    static void LoadApplications()
    {
        if (File.Exists(jsonFilePath))
        {
            string json = File.ReadAllText(jsonFilePath);
            var loadedData = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
            if (loadedData != null)
            {
                ProggrammDirectory = loadedData;
                Console.WriteLine("Приложения успешно загружены из JSON.");
            }
        }
        else
        {
            Console.WriteLine("Файл с сохраненными приложениями не найден.");
        }
    }

    // Сохранение приложений в JSON-файл
    static void SaveApplications()
    {
        string json = JsonConvert.SerializeObject(ProggrammDirectory, Formatting.Indented);
        File.WriteAllText(jsonFilePath, json);
    }

    // Метод для вывода содержимого файла
    static void PrintFileContent(string path)
    {
        Console.WriteLine("\nСодержимое файла:");
        Console.WriteLine(File.ReadAllText(path));
    }

    // Метод для вывода содержимого файла с номерами строк
    static void PrintFileContentWithLineNumbers(string[] lines)
    {
        Console.WriteLine("\nСодержимое файла:");
        for (int i = 0; i < lines.Length; i++)
        {
            Console.WriteLine($"{i + 1}: {lines[i]}");
        }
    }

    private static readonly string apiKey = "f40069028698c6ea89bcade883ddd7b0";
    private static readonly string city = "Moscow";
    private static readonly string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

    static async Task MainPogoda()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Парсим JSON-ответ
                JObject weatherData = JObject.Parse(responseBody);
                double temperature = weatherData["main"]["temp"].Value<double>();
                string description = weatherData["weather"][0]["description"].ToString();

                Console.WriteLine($"Погода в {city}: {temperature}°C, {description}");
            }
            catch (HttpRequestException e)  
            {
                Console.WriteLine($"Ошибка запроса: {e.Message}");
            }
        }
    }

    static void CleanTempFiles(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static string SearchYouTube(string query)
    {
        var service = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = "AIzaSyClVXOAyR6Xhsw93_janoLNdmQwtuSiw6U",
            ApplicationName = "YouTubeSearchApp"
        });

        var request = service.Search.List("snippet");
        request.Q = query;
        request.MaxResults = 1;

        var response = request.Execute();
        foreach (var result in response.Items)
        {
            if (result.Id.Kind == "youtube#video")
            {
                return $"https://www.youtube.com/watch?v={result.Id.VideoId}";
            }
        }
        return null;
    }
    static IntPtr FindYouTubeWindow()
    {
        IntPtr chrome = FindWindow("Chrome_WidgetWin_1", null);
        IntPtr edge = FindWindow("Chrome_WidgetWin_1", null);
        IntPtr firefox = FindWindow("MozillaWindowClass", null);

        if (chrome != IntPtr.Zero) return chrome;
        if (edge != IntPtr.Zero) return edge;
        if (firefox != IntPtr.Zero) return firefox;

        return IntPtr.Zero;
    }

    static void SendKey(byte key)
    {
        keybd_event(key, 0, 0, 0);
        Thread.Sleep(50);
        keybd_event(key, 0, 2, 0);
    }

        static void ClickWindowCenter(IntPtr hWnd)
    {
        if (GetWindowRect(hWnd, out RECT rect))
        {
            int centerX = (rect.Left + rect.Right) / 2;
            int centerY = (rect.Top + rect.Bottom) / 2;

            SetCursorPos(centerX, centerY);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}