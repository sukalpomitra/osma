using System.Net.Http;
using AgentFramework.AspNetCore;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Handlers.Agents;
using AgentFramework.Core.Handlers.Internal;
using AgentFramework.Core.Runtime.Transport;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using AgentFramework.Core.Handlers;

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
