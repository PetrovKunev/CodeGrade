# Миграция от Docker към Judge0 API

Това ръководство обяснява как да преминете от Docker към Judge0 API в CodeGrade системата.

## 🎯 Защо Judge0 API?

### Предимства
- ✅ **Няма нужда от Docker** на сървъра
- ✅ **Работи в shared hosting** (SmarterASP.NET)
- ✅ **Поддържа 60+ езика** (вместо 4)
- ✅ **Безплатно** до 1000 заявки/ден
- ✅ **По-лесна поддръжка**

### Недостатъци
- ❌ **Изисква интернет връзка**
- ❌ **Rate limiting** от API
- ❌ **Зависи от външна услуга**

## 🔄 Стъпки за миграция

### 1. Премахване на Docker зависимости

#### CodeGrade.csproj
```xml
<!-- Премахнете този ред -->
<PackageReference Include="Docker.DotNet" Version="3.125.15" />
```

#### Program.cs
```csharp
// Променете този ред
// builder.Services.AddScoped<ICodeExecutorService, DockerCodeExecutorService>();
builder.Services.AddScoped<ICodeExecutorService, Judge0CodeExecutorService>();
```

#### appsettings.json
```json
// Премахнете Docker секцията
// "Docker": {
//   "CodeRunnerImage": "codegrade/code-runner:latest",
//   "NetworkName": "codegrade-network"
// },

// Добавете Judge0 секцията
"Judge0": {
  "ApiKey": "your-rapidapi-key-here",
  "ApiHost": "judge0-ce.p.rapidapi.com",
  "BaseUrl": "https://judge0-ce.p.rapidapi.com"
}
```

### 2. Изтриване на Docker файлове

```bash
# Премахнете Docker папката
rm -rf Infrastructure/Docker/

# Премахнете DockerCodeExecutorService
rm Services/DockerCodeExecutorService.cs
```

### 3. Добавяне на Judge0 файлове

- ✅ `Services/Judge0CodeExecutorService.cs` - нов
- ✅ `JUDGE0_SETUP.md` - документация
- ✅ `TestJudge0API.cs` - тестове

## 🧪 Тестване на миграцията

### 1. Локално тестване

```bash
# Възстановете пакети
dotnet restore

# Стартирайте приложението
dotnet run

# Тествайте с прости задачи
```

### 2. Тестване на различни езици

```csharp
// C#
Console.WriteLine("Hello World");

// Python
print("Hello World")

// Java
public class Main {
    public static void main(String[] args) {
        System.out.println("Hello World");
    }
}

// JavaScript
console.log("Hello World");
```

### 3. Тестване на грешки

```csharp
// Грешен C# код
Console.WriteLine("Hello World"
// Липсва затваряща скоба
```

## 📊 Сравнение на функционалността

| Функция | Docker | Judge0 API |
|---------|--------|------------|
| Поддържани езици | 4 | 60+ |
| Изолация | ✅ | ✅ |
| Ограничения за време | ✅ | ✅ |
| Ограничения за памет | ✅ | ✅ |
| Локално изпълнение | ✅ | ❌ |
| Интернет зависимост | ❌ | ✅ |
| Rate limiting | ❌ | ✅ |
| Поддръжка | Сложна | Лесна |

## 🔧 Конфигурация за различни среди

### Development
```json
{
  "Judge0": {
    "ApiKey": "your-dev-api-key",
    "ApiHost": "judge0-ce.p.rapidapi.com"
  },
  "Logging": {
    "LogLevel": {
      "CodeGrade.Services.Judge0CodeExecutorService": "Debug"
    }
  }
}
```

### Production
```json
{
  "Judge0": {
    "ApiKey": "your-prod-api-key",
    "ApiHost": "judge0-ce.p.rapidapi.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

## 🚀 Разполагане в SmarterASP.NET

### 1. Подготовка
```bash
# Създайте production build
dotnet publish -c Release -o ./publish

# Качете publish папката в SmarterASP.NET
```

### 2. Конфигурация
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-sql-server;Database=CodeGradeDb;User Id=username;Password=password;"
  },
  "Judge0": {
    "ApiKey": "your-production-api-key"
  }
}
```

### 3. Тестване
- Тествайте с прости задачи
- Проверете логовете
- Мониторирайте API използването

## 📈 Мониторинг и поддръжка

### RapidAPI Dashboard
1. Отидете в [RapidAPI Dashboard](https://rapidapi.com/dashboard)
2. Намерете Judge0 CE API
3. Проверете статистиките

### Логове в приложението
```csharp
// В appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "CodeGrade.Services.Judge0CodeExecutorService": "Debug"
    }
  }
}
```

## 🔒 Сигурност

### Judge0 API сигурност
- Изолирани изпълнения
- Ограничения за време и памет
- Без мрежов достъп
- Read-only файлова система

### Вашият код
- Валидация на входни данни
- Rate limiting
- Логване на всички заявки

## 💰 Разходи

### Безплатен план
- 1000 заявки/ден
- Подходящ за малки проекти

### Платен план
- $10/месец за 100,000 заявки
- Подходящ за големи проекти

## ✅ Проверен списък

- [ ] Премахнати Docker зависимости
- [ ] Добавен Judge0 API ключ
- [ ] Тествани всички езици
- [ ] Проверени логове
- [ ] Тествано в production
- [ ] Обновена документация
- [ ] Обучен екипът

## 🆘 Поддръжка

### Проблеми с Judge0 API
- [Judge0 Documentation](https://judge0.com/docs)
- [RapidAPI Support](https://rapidapi.com/support)

### Проблеми с CodeGrade
- GitHub Issues
- Email: support@codegrade.com

---

**Важно**: Винаги тествайте миграцията в staging среда преди production! 