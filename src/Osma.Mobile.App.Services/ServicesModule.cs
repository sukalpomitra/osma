using System.Net.Http;
using AgentFramework.Core.Handlers.Agents;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Features.TrustPing;
using Hyperledger.Aries.Runtime;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.Discovery;

namespace Osma.Mobile.App.Services
{
    public class ServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Populate(new ServiceCollection());

            builder
                .RegisterType<DefaultConnectionHandler>()
                .Keyed<IMessageHandler>("connection")
                .As<IMessageHandler>();

            builder
                .RegisterType<DefaultCredentialHandler>()
                .Keyed<IMessageHandler>("credential")
                .As<IMessageHandler>();

            builder
                .RegisterType<DefaultProofHandler>()
                .Keyed<IMessageHandler>("proof")
                .As<IMessageHandler>();

            builder
                .RegisterType<DefaultTrustPingMessageHandler>()
                .Keyed<IMessageHandler>("trust")
                .As<IMessageHandler>();

            builder
                .RegisterType<DefaultAgent>()
                .As<IAgent>();

            builder
                .RegisterType<HttpMessageDispatcher>()
                .As<IMessageDispatcher>();

            builder
                .RegisterType<HttpClientHandler>()
                .As<HttpMessageHandler>();

            builder
                .RegisterType<EventAggregator>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<AgentContextProvider>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultWalletRecordService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultWalletService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultPoolService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultConnectionService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultCloudRegistrationService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultCredentialService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultProofService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultProvisioningService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<DefaultMessageService>()
                .AsImplementedInterfaces()
                .SingleInstance();
            
            builder
                .RegisterType<DefaultLedgerService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<DefaultSchemaService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<DefaultTailsService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<DefaultDiscoveryService>()
                .AsImplementedInterfaces()
                .SingleInstance();
        }
    }
}
