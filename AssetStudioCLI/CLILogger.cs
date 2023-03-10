﻿using AssetStudio;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AssetStudioCLI.Options;

namespace AssetStudioCLI
{
    internal enum LogOutputMode
    {
        Console,
        File,
        Both,
    }

    internal class CLILogger : ILogger
    {
        private readonly LogOutputMode logOutput;
        private readonly LoggerEvent logMinLevel;
        public string LogName;
        public string LogPath;

        public CLILogger(CLIOptions options)
        {
            logOutput = options.o_logOutput.Value;
            logMinLevel = options.o_logLevel.Value;
            LogName = $"AssetStudioCLI_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogName);

            var ver = typeof(Program).Assembly.GetName().Version;
            LogToFile(LoggerEvent.Verbose, $"---AssetStudioCLI v{ver} | Logger launched---\n" +
                                           $"CMD Args: {string.Join(" ", options.cliArgs)}");
        }

        private static string ColorLogLevel(LoggerEvent logLevel)
        {
            string formattedLevel = $"[{logLevel}]";
            switch (logLevel)
            {
                case LoggerEvent.Info:
                    return $"{formattedLevel.Color(CLIAnsiColors.BrightCyan)}";
                case LoggerEvent.Warning:
                    return $"{formattedLevel.Color(CLIAnsiColors.BrightYellow)}";
                case LoggerEvent.Error:
                    return $"{formattedLevel.Color(CLIAnsiColors.BrightRed)}";
                default:
                    return formattedLevel;
            }
        }

        private static string FormatMessage(LoggerEvent logMsgLevel, string message, bool consoleMode = false)
        {
            var curTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            message = message.TrimEnd();
            var multiLine = message.Contains('\n');
            
            string formattedMessage;
            if (consoleMode)
            {
                string colorLogLevel = ColorLogLevel(logMsgLevel);
                formattedMessage = $"{colorLogLevel} {message}";
                if (multiLine)
                {
                    formattedMessage = formattedMessage.Replace("\n", $"\n{colorLogLevel} ");
                }
            }
            else
            {
                message = Regex.Replace(message, @"\e\[[0-9;]*m(?:\e\[K)?", "");  //Delete ANSI colors
                var logLevel = $"{logMsgLevel.ToString().ToUpper(),-7}";
                formattedMessage = $"{curTime} | {logLevel} | {message}";
                if (multiLine)
                {
                    formattedMessage = formattedMessage.Replace("\n", $"\n{curTime} | {logLevel} | ");
                }
            }
            return formattedMessage;
        }

        public void LogToConsole(LoggerEvent logMsgLevel, string message)
        {
            if (logOutput != LogOutputMode.File)
            {
                Console.WriteLine(FormatMessage(logMsgLevel, message, consoleMode: true));
            }
        }

        public async void LogToFile(LoggerEvent logMsgLevel, string message)
        {
            if (logOutput != LogOutputMode.Console)
            {
                using (var sw = new StreamWriter(LogPath, append: true, System.Text.Encoding.UTF8))
                {
                    await sw.WriteLineAsync(FormatMessage(logMsgLevel, message));
                }
            }
        }

        public void Log(LoggerEvent logMsgLevel, string message, bool ignoreLevel)
        {
            if ((logMsgLevel < logMinLevel && !ignoreLevel) || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (logOutput != LogOutputMode.File)
            {
                LogToConsole(logMsgLevel, message);
            }
            if (logOutput != LogOutputMode.Console)
            {
                LogToFile(logMsgLevel, message);
            }
        }
    }
}
