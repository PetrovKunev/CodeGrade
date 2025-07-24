namespace CodeGrade.ViewModels
{
    public class ModalViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string IconBgClass { get; set; } = string.Empty;
        public string ButtonText { get; set; } = "Разбрах";
        public string ButtonClass { get; set; } = "bg-indigo-500";
        public string CloseButtonId { get; set; } = string.Empty;
        public ModalType Type { get; set; } = ModalType.Info;

        public static ModalViewModel CreateSuccess(string id, string title, string message)
        {
            return new ModalViewModel
            {
                Id = id,
                Title = title,
                Message = message,
                IconClass = "fas fa-check-circle text-green-600",
                IconBgClass = "bg-green-100",
                ButtonClass = "bg-green-500",
                CloseButtonId = $"close-{id}",
                Type = ModalType.Success
            };
        }

        public static ModalViewModel CreateError(string id, string title, string message)
        {
            return new ModalViewModel
            {
                Id = id,
                Title = title,
                Message = message,
                IconClass = "fas fa-exclamation-triangle text-red-600",
                IconBgClass = "bg-red-100",
                ButtonClass = "bg-red-500",
                CloseButtonId = $"close-{id}",
                Type = ModalType.Error
            };
        }

        public static ModalViewModel CreateWarning(string id, string title, string message)
        {
            return new ModalViewModel
            {
                Id = id,
                Title = title,
                Message = message,
                IconClass = "fas fa-exclamation-triangle text-yellow-600",
                IconBgClass = "bg-yellow-100",
                ButtonClass = "bg-yellow-500",
                CloseButtonId = $"close-{id}",
                Type = ModalType.Warning
            };
        }

        public static ModalViewModel CreateInfo(string id, string title, string message)
        {
            return new ModalViewModel
            {
                Id = id,
                Title = title,
                Message = message,
                IconClass = "fas fa-info-circle text-blue-600",
                IconBgClass = "bg-blue-100",
                ButtonClass = "bg-blue-500",
                CloseButtonId = $"close-{id}",
                Type = ModalType.Info
            };
        }

        public static ModalViewModel CreateConfirm(string id, string title, string message, string confirmText = "Да", string cancelText = "Не")
        {
            return new ModalViewModel
            {
                Id = id,
                Title = title,
                Message = message,
                IconClass = "fas fa-question-circle text-blue-600",
                IconBgClass = "bg-blue-100",
                ButtonClass = "bg-blue-500",
                CloseButtonId = $"close-{id}",
                Type = ModalType.Confirm
            };
        }
    }

    public enum ModalType
    {
        Success,
        Error,
        Warning,
        Info,
        Confirm
    }
} 