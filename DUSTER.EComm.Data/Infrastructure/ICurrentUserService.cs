using DUSTER.EComm.Data.CommonClass;

namespace DUSTER.EComm.Data.Infrastructure
{
    public interface ICurrentUserService
    {
        CurrentUser? User { get; }
    }
}
