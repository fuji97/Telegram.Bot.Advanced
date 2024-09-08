echo off
set ver=%1
dotnet build
nuget add bin/Debug/Telegram.Bot.Advanced.%ver%.nupkg -source %NUGET_REPO%