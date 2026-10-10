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

## Інваріанти доменної моделі

Доменна сутність `Product` гарантує виконання таких правил:

1. Ідентифікатор товару (`Id`) не може бути порожнім або складатися лише з пробілів.

2. SKU товару не може бути порожнім або складатися лише з пробілів.

3. Назва товару (`Name`) не може бути порожньою або складатися лише з пробілів.

4. Одиниця вимірювання (`Unit`) не може бути порожньою або складатися лише з пробілів.

5. Початковий залишок товару не може бути від'ємним.

6. Кількість товару при надходженні повинна бути більшою за нуль.

7. Кількість товару при видачі повинна бути більшою за нуль.

8. Не можна видати товару більше, ніж є в поточному залишку.

9. Стан товару може змінюватися лише дозволеними переходами:
   - `Active → Suspended`;
   - `Suspended → Active`;
   - `Active → Archived`;
   - `Suspended → Archived`.

10. Заборонені переходи між станами призводять до `InvalidOperationException`.

11. Критичний стан товару не має публічних setter-ів. 
   Зміна залишку та стану виконується тільки через доменні методи.

12. Конструктор `Product` є приватним. 
   Створення товару виконується через фабричний метод `Product.Create()`,
   який перевіряє всі необхідні інваріанти.