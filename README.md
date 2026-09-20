# CrossApp

Практикум з крос-платформного програмування.

## Лабораторна робота 2

## Структура Solution

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   ├── EnvironmentInfo.cs
    │   └── EnvironmentReport.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs
```

## Build

Збірка всієї Solution:

```powershell
dotnet build
```

Збірка окремо бібліотеки `Core`:

```powershell
dotnet build src/Core/Core.csproj
```

## Run

Запуск консольного застосунку:

```powershell
dotnet run --project src/Cli
```

`Core` окремо не запускається, оскільки є бібліотекою класів.

## Publish

### Windows x64 — self-contained

```powershell
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
```

У self-contained режимі необхідний .NET Runtime включається до публікації.

### Windows x64 — framework-dependent

```powershell
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

У framework-dependent режимі Runtime не включається до публікації. Для запуску потрібен встановлений сумісний .NET Runtime.


## Порівняння публікацій

| RID       | Режим               |   Розмір | Потрібен встановлений Runtime |
| --------- | ------------------- | -------: | ----------------------------- |
| win-x64   | self-contained      | 76.86 MB | Ні                            |
| win-x64   | framework-dependent |  0.20 MB | Так                           |
| linux-x64 | self-contained      | 78.82 MB | Ні                            |
| linux-x64 | framework-dependent |  0.12 MB | Так                           |

Self-contained публікація має більший розмір, оскільки містить необхідні компоненти .NET Runtime.

Framework-dependent публікація значно менша, оскільки використовує Runtime, встановлений у системі.