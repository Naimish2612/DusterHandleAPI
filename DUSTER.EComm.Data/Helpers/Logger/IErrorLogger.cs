namespace DUSTER.EComm.Data.Helpers.Logger
{
    public interface IErrorLogger
    {
        void Exception(Exception ex);
        void Log(string message);
        void Warning(string message);
    }
}
