using NLog;

namespace DUSTER.EComm.Data.Helpers.Logger
{
    public partial class ErrorLogger : IErrorLogger
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        public ErrorLogger() { }

        public void Exception(Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }

        public void Log(string message)
        {
            Logger.Info(message);
        }

        public void Warning(string message)
        {
            Logger.Warn(message);
        }
    }
}
