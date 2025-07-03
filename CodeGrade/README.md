# CodeGrade - Система за автоматично оценяване на код

Модерна образователна система за автоматично оценяване на програмен код, предназначена за учители и ученици в сферата на програмирането.

## 🚀 Функционалности

- **Автоматично оценяване**: Изпълнение на код в изолирани Docker контейнери
- **Множество езици**: Поддръжка за C#, Python, Java, JavaScript
- **Роли и права**: Администратори, учители и ученици
- **Реално време**: Мигновени резултати от тестови случаи
- **Детайлни отчети**: Статистики и графики за прогреса
- **Модерен UI**: Responsive дизайн с TailwindCSS

## 🛠 Технологичен стек

- **Backend**: ASP.NET Core 8.0 MVC
- **ORM**: Entity Framework Core
- **База данни**: SQL Server (LocalDB)
- **Автентикация**: ASP.NET Identity
- **UI**: TailwindCSS + Font Awesome
- **Контейнеризация**: Docker
- **Изпълнение на код**: Изолирани Docker контейнери

## 📋 Изисквания

- .NET 8.0 SDK
- SQL Server LocalDB
- Docker Desktop
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

### 4. Създаване на Docker image

```bash
docker build -f Infrastructure/Docker/Dockerfile.CodeRunner -t codegrade/code-runner:latest .
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
  "Docker": {
    "CodeRunnerImage": "codegrade/code-runner:latest",
    "NetworkName": "codegrade-network"
  },
  "ExecutionLimits": {
    "DefaultTimeLimit": 5,
    "DefaultMemoryLimit": 128,
    "MaxSubmissionsPerHour": 10
  }
}
```

## 📊 Поддържани езици

| Език | Версия | Статус |
|------|--------|--------|
| C# | .NET 8.0 | ✅ |
| Python | 3.11+ | ✅ |
| Java | OpenJDK 17 | ✅ |
| JavaScript | Node.js 18+ | ✅ |

## 🔒 Безопасност

- Изолирани Docker контейнери за изпълнение на код
- Ограничения за време и памет
- Non-root потребители в контейнерите
- Валидация на входни данни
- CSRF защита

## 📈 Производителност

- Кеширане на компилирани резултати
- Асинхронно изпълнение на задачи
- Оптимизирани заявки към базата данни
- CDN за статични файлове

## 🧪 Тестване

```bash
# Unit тестове
dotnet test

# Integration тестове
dotnet test --filter Category=Integration
```

## 📝 Лиценз

MIT License - вижте [LICENSE](LICENSE) файла за подробности.

## 🤝 Принос

1. Fork проекта
2. Създайте feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit промените (`git commit -m 'Add some AmazingFeature'`)
4. Push към branch (`git push origin feature/AmazingFeature`)
5. Отворете Pull Request

## 📞 Поддръжка

За въпроси и проблеми:
- Създайте Issue в GitHub
- Email: support@codegrade.com

## 🔄 Версии

- **v1.0.0** - Първоначална версия
  - Основни функционалности
  - Поддръжка за 4 езика
  - Dashboard за всички роли

## 📚 Документация

За подробна документация вижте [Wiki](https://github.com/your-repo/wiki).

---

**CodeGrade** - Модерна система за оценяване на код 🚀 