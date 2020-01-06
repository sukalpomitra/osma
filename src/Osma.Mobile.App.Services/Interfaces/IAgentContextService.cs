using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using AgentOptions = Hyperledger.Aries.Configuration.AgentOptions;

namespace Osma.Mobile.App.Services.Interfaces
{
    public interface ICustomAgentContextProvider : IAgentProvider
    {
        bool AgentExists();

        Task<bool> CreateAgentAsync(AgentOptions options);
    }
}
