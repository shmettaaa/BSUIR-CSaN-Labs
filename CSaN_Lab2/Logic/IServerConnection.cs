using System.Threading.Tasks;

namespace CSaN_Lab2.Logic
{
    public interface IServerConnection : IClientConnection
    {
        Task<IClientConnection> AcceptClientAsync();
    }
}