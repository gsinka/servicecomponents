using System;
using Autofac;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Logging.Serilog;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public class NhibernateModule : Module
    {
        private readonly string _connectionString;
        private readonly Action<MappingConfiguration> _mapping;
        private readonly Action<Configuration> _exposeConfiguration;

        public NhibernateModule(string connectionString, Action<MappingConfiguration> mapping, Action<Configuration> exposeConfiguration)
        {
            _connectionString = connectionString;
            _mapping = mapping;
            _exposeConfiguration = exposeConfiguration;
        }

        override protected void Load(ContainerBuilder builder)
        {
            NHibernateLogger.SetLoggersFactory(new SerilogLoggerFactory());

            builder.Register(context =>

                Fluently.Configure()
                    .Database(PostgreSQLConfiguration.PostgreSQL82.ConnectionString(_connectionString))
                    .Mappings(map => _mapping(map))
                    .ExposeConfiguration(cfg => _exposeConfiguration(cfg))
                    .BuildConfiguration()

            ).As<Configuration>().SingleInstance();

            builder.Register(context => context.Resolve<Configuration>().BuildSessionFactory())
                .As<ISessionFactory>()
                .SingleInstance();

            builder.Register(context => context.Resolve<ISessionFactory>().OpenSession())
                .As<ISession>()
                .InstancePerLifetimeScope()
                .AutoActivate();

        }
    }
}
