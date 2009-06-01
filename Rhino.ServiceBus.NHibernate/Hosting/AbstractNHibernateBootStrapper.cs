using System;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.NHibernate.Resolvers;
using Environment = NHibernate.Cfg.Environment;

namespace Rhino.ServiceBus.NHibernate.Hosting
{
	public class AbstractNHibernateBootStrapper : AbstractBootStrapper
	{
		private ISessionFactory sessionFactory;

		public virtual void ConfigureNHibernate(Configuration configuration)
		{
			configuration.AddAssembly(Assembly);
		}


		public override void BeforeStart()
		{
            var configuration = new Configuration()
				.SetProperty(Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle")
				.SetProperty(Environment.ConnectionStringName, Assembly.GetName().Name)
				.SetProperty(Environment.Dialect, Dialect.AssemblyQualifiedName);

			ConfigureNHibernate(configuration);

			sessionFactory = configuration.BuildSessionFactory();
            container.Kernel.AddComponentInstance<Configuration>(configuration);
			container.Kernel.AddComponentInstance<ISessionFactory>(sessionFactory);
		}

		protected override void ConfigureContainer()
		{
			container.Kernel.Resolver.AddSubResolver(new SessionResolver(container.Kernel));
			base.ConfigureContainer();
		}

		public virtual Type Dialect
		{
			get { return typeof(MsSql2000Dialect); }
		}
	}
}