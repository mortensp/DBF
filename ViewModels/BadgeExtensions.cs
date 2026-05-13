namespace DBF.ViewModels
{
    internal static class BadgeExtensions
    {
        internal static void ShowBadge(
            this ControlViewModel vm,
            Action<string> setValue,
            Func<string> getValue,
            string message,
            int delayMs = 20000)
        {
            setValue(message);

            _ = Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                await Execute.OnUIThreadAsync(() =>
                {
                    if (getValue() == message)
                        setValue(null);
                    return Task.CompletedTask;
                });
            });
        }
    }
}
