namespace Lumination
{
    public static class Log
    {
        public static void Error(string message, string name)
        {
            SuperController.LogError($"{nameof(Lumination)}.{name}: {message}");
        }

        public static void Message(string message, string name)
        {
            SuperController.LogMessage($"{nameof(Lumination)}.{name}: {message}");
        }
    }
}
