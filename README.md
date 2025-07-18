# CodeGrade - Система за автоматично оценяване на код

Модерна образователна система за автоматично оценяване на програмен код, предназначена за учители и ученици в сферата на програмирането.

## 🚀 Функционалности

- **Автоматично оценяване**: Изпълнение на код чрез Judge0 API
- **Множество езици**: Поддръжка за C#, Python, Java, JavaScript, C++, PHP, Ruby, Go, Rust и др.
- **Роли и права**: Администратори, учители и ученици
- **Реално време**: Мигновени резултати от тестови случаи
- **Детайлни отчети**: Статистики и графики за прогреса
- **Модерен UI**: Responsive дизайн с TailwindCSS

## 🛠 Технологичен стек

- **Backend**: ASP.NET Core 9.0 MVC
- **ORM**: Entity Framework Core
- **База данни**: SQL Server (LocalDB)
- **Автентикация**: ASP.NET Identity
- **UI**: TailwindCSS + Font Awesome
- **Изпълнение на код**: Judge0 API (без нужда от Docker)

## 📋 Изисквания

- .NET 9.0 SDK
- SQL Server LocalDB
- Интернет връзка (за Judge0 API)
- Visual Studio 2022 или VS Code

## 🚀 Инсталация

### 1. Клониране на проекта

```bash
git clone <repository-url>
cd CodeGrade
```

### 2. Възстановяване на пакети

```bash
dotnet restore
```

### 3. Конфигурация на база данни

```bash
dotnet ef database update
```

### 4. Настройка на Judge0 API

1. Регистрирайте се в [RapidAPI](https://rapidapi.com)
2. Намерете "Judge0 CE" API и абонирайте се за безплатния план
3. Копирайте вашия API ключ
4. Добавете ключа в `appsettings.json`:

```json
{
  "Judge0": {
    "ApiKey": "your-rapidapi-key-here"
  }
}
```

### 5. Стартиране на приложението

```bash
dotnet run
```

## 👥 Потребителски роли

### Администратор
- Управление на потребители
- Създаване на класове и предмети
- Системни настройки

### Учител
- Създаване и редактиране на задачи
- Добавяне на тестови случаи
- Преглед на решения и оценки
- Експорт на резултати

### Ученик
- Преглед на активни задачи
- Подаване на решения
- Следване на прогреса
- Преглед на оценки

## 🔧 Конфигурация

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CodeGradeDb;Trusted_Connection=True;"
  },
  "Judge0": {
    "ApiKey": "your-rapidapi-key-here",
    "ApiHost": "judge0-ce.p.rapidapi.com",
    "BaseUrl": "https://judge0-ce.p.rapidapi.com"
  },
  "ExecutionLimits": {
    "DefaultTimeLimit": 30,
    "DefaultMemoryLimit": 128,
    "MaxSubmissionsPerHour": 10
  }
}
```

## 📊 Поддържани езици

| Език | Версия | Статус |
|------|--------|--------|
| C# | .NET Core | ✅ |
| Python | 3.8.1 | ✅ |
| Java | OpenJDK 13.0.1 | ✅ |
| JavaScript | Node.js 12.14.0 | ✅ |
| C++ | GCC 9.2.0 | ✅ |
| C | GCC 9.2.0 | ✅ |
| PHP | 7.4.1 | ✅ |
| Ruby | 2.7.0 | ✅ |
| Go | 1.13.5 | ✅ |
| Rust | 1.40.0 | ✅ |
| Swift | 5.2.3 | ✅ |
| Kotlin | 1.3.70 | ✅ |
| Scala | 2.13.2 | ✅ |
| R | 4.0.0 | ✅ |
| Dart | 2.7.2 | ✅ |

## 🔒 Безопасност

- Изолирани изпълнения чрез Judge0 API
- Ограничения за време и памет
- Валидация на входни данни
- CSRF защита
- Rate limiting за API заявки

## 📈 Производителност

- Асинхронно изпълнение на задачи
- Кеширане на резултати
- Оптимизирани заявки към базата данни
- Ефективно управление на API заявки

## 💰 Разходи

- **Judge0 API**: Безплатно до 1000 заявки/ден
- **Платен план**: $10/месец за 100,000 заявки
- **Вашето приложение**: Безплатно (без Docker нужди)

## 🚀 Разполагане

### SmarterASP.NET (Shared Hosting)

Този проект е оптимизиран за shared hosting среди като SmarterASP.NET:

- ✅ **Няма нужда от Docker**
- ✅ **Работи в shared hosting**
- ✅ **Минимални изисквания за сървъра**
- ✅ **Лесно разполагане**

### Стъпки за разполагане:

1. **Подгответе Judge0 API ключ**
2. **Качете файловете в SmarterASP.NET**
3. **Конфигурирайте базата данни**
4. **Добавете API ключа в appsettings.json**
5. **Стартирайте приложението**

## 🔧 Поддръжка

### Добавяне на нов език

За да добавите поддръжка за нов език:

1. Проверете дали Judge0 API го поддържа
2. Добавете language ID в `Judge0CodeExecutorService.GetLanguageId()`
3. Тествайте с примерен код

### Мониторинг

Системата включва вградено логване за:
- API заявки и отговори
- Грешки при изпълнение
- Производителност на системата

## 📞 Поддръжка

За въпроси и проблеми:
- Създайте issue в GitHub
- Проверете документацията на Judge0 API
- Консултирайте се с RapidAPI поддръжката 
