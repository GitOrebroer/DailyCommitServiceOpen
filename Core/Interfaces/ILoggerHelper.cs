namespace Core.Interfaces
{
    public interface ILoggerHelper
    {
        void Log(string message, bool isError);
        void LogException(Exception message);
    }
}