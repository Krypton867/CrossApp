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


## Лабораторна робота 5 — сервісний шар і сховища

Реалізовано сервісний шар для роботи з каталогом товарів.

- `ICatalogStore` — інтерфейс сховища.
- `InMemoryCatalogStore` — зберігання в оперативній пам'яті.
- `FileCatalogStore` — зберігання у JSON-файлі.
- `CatalogService` — бізнес-операції з товарами.
- `CachingCatalogStore` — декоратор з кешуванням результатів читання.

Залежності передаються вручну через конструктор `CatalogService`.
Конкретне сховище вибирається у `Program.cs` за аргументом `--file`.
Контейнер Microsoft.Extensions.DependencyInjection не використовується.

### Запуск

```powershell
dotnet run --project src/Cli
dotnet run --project src/Cli -- --file
```

## Лабораторна робота 6 — події, делегати та лямбда-вирази

Реалізовано механізм сповіщення про зміни каталогу товарів за допомогою подій та делегатів.

- `CatalogChangeKind` — перелік типів змін: додавання, надходження, видача та видалення товару.
- `CatalogChangedEventArgs` — дані про подію: ID товару, тип зміни, зміну кількості, залишок після операції та час події.
- `CatalogService.Changed` — подія, яка сповіщає підписників про успішні зміни каталогу.
- `Func<Product, bool>` — використовується для пошуку товарів за заданою умовою.
- `Action<string>` — використовується для повідомлення про пропущені рядки під час імпорту CSV.
- У `Program.cs` реалізовано підписку та відписку від події, обробник події та лямбда-вираз із замиканням для підрахунку змін.

Повідомлення про зміни виводяться в консоль та записуються до лог-файлу `logs/app.log`.

### Додаткове завдання

Реалізовано альтернативний механізм сповіщень через інтерфейс `INotificationSink`:

- `ConsoleSink` — виведення повідомлень у консоль.
- `FileSink` — запис повідомлень у файл.
- `CompositeNotificationSink` — одночасне використання декількох механізмів сповіщення.

Залежність від механізму сповіщень передається до `CatalogService` через конструктор (ручне впровадження залежностей).

## Лабораторна робота 7 — звіти через LINQ

Реалізовано аналітичне опрацювання колекції товарів за допомогою LINQ to Objects.

- `StockLine` — DTO для представлення рядків звіту.
- `GroupSummary` — DTO для представлення результатів групування.
- `ReportService` — формування звітів із використанням `Where`, `Select`, `GroupBy`, сортування та агрегатних операцій.
- `EnumerableExtensions` — власний метод-розширення `TopBy<T>` для отримання перших N елементів за заданим критерієм.
- `SampleData` — розширено до 18 демонстраційних товарів із різними одиницями вимірювання та залишками.

Реалізовано звіти:
- Топ-5 товарів за кількістю залишку.
- Товари з низьким залишком.
- Групування товарів за одиницями вимірювання.
- Загальна кількість, сума, середнє та максимальне значення залишку.
- Топ одиниць вимірювання за сумарною кількістю.

Додатково реалізовано власний метод-розширення `TopBy<T>`, який використовується у двох звітах.

### Відкладене виконання LINQ

LINQ-запити з відкладеним виконанням не опрацьовують колекцію в момент створення. Виконання відбувається під час перебору або виклику операцій на кшталт `Count()`.

У демонстрації два виклики `Count()` повертають різні результати, оскільки між ними змінюється початкова колекція. Виклик `ToList()` створює окремий знімок результатів, який не змінюється після очищення джерела.

### Запуск звітів

```powershell
dotnet run --project src/Cli -- report-top
dotnet run --project src/Cli -- report-low
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-top-units
dotnet run --project src/Cli -- report-deferred
```

Для використання файлового сховища додається аргумент `--file`.

## Лабораторна робота 9 — шляхи, JSON, конфігурація та secrets

Конфігурація читається з `src/Cli/appsettings.json` і необов'язкового `appsettings.Development.json`; ці файли копіюються до вихідного каталогу програми. Відносний `Storage:DataPath` відраховується від `AppContext.BaseDirectory`, тому поточний робочий каталог не впливає на місце збереження. `--file` вмикає постійне JSON-сховище, без нього використовується демонстраційний каталог у пам'яті.

### Налаштування

| Ключ | Значення | Призначення |
|---|---|---|
| `Storage:DataPath` | `data` | Каталог даних (відносно папки програми або абсолютний) |
| `Storage:FileName` | `items.json` | Назва JSON-файлу каталогу |
| `Logging:MinimumLevel` | `Information` | Конфігураційний рівень журналювання; наразі відображається CLI |
| `EnvironmentName` | `Production` | Профіль; за потреби перевизначається `DOTNET_ENVIRONMENT` |
| `Warehouse:DefaultUnit` | `шт` | Типова одиниця вимірювання для складу |
| `Integration:ApiKey` | не зберігається в git | Secret із user-secrets або середовища; відображається тільки факт наявності |

При `DOTNET_ENVIRONMENT=Development` додатково завантажується `appsettings.Development.json`, де `Logging:MinimumLevel=Debug`. Середовищні змінні з префіксом `CROSSAPP_` перекривають JSON, подвійне підкреслення `__` позначає вкладені розділи. Окрема змінна `CROSSAPP_DATA_PATH` має пріоритет над `Storage:DataPath`.

### Команди з кореня solution

```powershell
dotnet run --project src/Cli -- export
dotnet run --project src/Cli -- import
dotnet run --project src/Cli -- import data/import.json
dotnet run --project src/Cli -- import-stream data/import.json --file
```

`export` записує повну поточну колекцію до налаштованого файлу; `import` без шляху читає цей файл. У режимі `--file` імпорт із того самого файлу пропустить усі наявні ID; для перевірки додавання використовуйте окремий файл з новими ID.

### PowerShell

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
$env:CROSSAPP_DATA_PATH = "$HOME\CrossAppData"
$env:CROSSAPP_Storage__FileName = "products.json"
$env:CROSSAPP_Integration__ApiKey = "demo-only-value"
dotnet run --project src/Cli -- export
Remove-Item Env:CROSSAPP_Integration__ApiKey
Remove-Item Env:CROSSAPP_DATA_PATH
Remove-Item Env:CROSSAPP_Storage__FileName
Remove-Item Env:DOTNET_ENVIRONMENT
```

Для локальних secrets:

```powershell
dotnet user-secrets set "Integration:ApiKey" "local-demo-value" --project src/Cli
dotnet user-secrets list --project src/Cli
```

Реальні ключі не виводяться у консолі та не записуються до репозиторію. User-secrets призначені для локальної розробки; це не зашифроване захищене сховище для production.

### bash (Linux)

```bash
DOTNET_ENVIRONMENT=Development CROSSAPP_DATA_PATH=/tmp/crossapp-data dotnet run --project src/Cli -- export
CROSSAPP_Storage__FileName=products.json dotnet run --project src/Cli -- export
```

### Перевірка

```powershell
dotnet build
dotnet test
git status --short
```

Запустити `dotnet run --project src/Cli -- export` з кореня, а потім із `src/Cli` виконати `dotnet run -- export`: шлях до даних має залишатися однаковим. Видалення каталогу даних і повторний `export` створюють його заново. Некоректний JSON виводить зрозуміле повідомлення. Кирилиця зберігається у читабельному вигляді через `UnsafeRelaxedJsonEscaping`. Файлове сховище записує `.tmp` і замінює цільовий файл через `File.Move`.

Непокритими автоматизованими тестами можуть залишатися рідкісні збої файлової системи та окремі конфігураційні сценарії; частину з них перевіряємо вручну.