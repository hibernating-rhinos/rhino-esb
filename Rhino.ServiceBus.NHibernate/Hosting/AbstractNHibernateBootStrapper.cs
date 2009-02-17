using System;
using FluentNHibernate.AutoMap;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using Rhino.ServiceBus.Hosting;
using Rhino.ServiceBus.NHibernate.Resolvers;
using Rhino.ServiceBus.NHibernate.Util;
using Environment = NHibernate.Cfg.Environment;

namespace Rhino.ServiceBus.NHibernate.Hosting
{
	public class AbstractNHibernateBootStrapper : AbstractBootStrapper
	{
		private ISessionFactory sessionFactory;

		public virtual void ConfigureNHibernate(Configuration configuration)
		{
			var model = CreatePersistenceModel();

			model
				.AddEntityAssembly(Assembly)
				.Where(entity =>
						string.IsNullOrEmpty(entity.Namespace) == false &&
					   entity.Namespace.EndsWith("Model") &&
					   entity.GetProperty("Id") != null &&
					   entity.IsAbstract == false
				)
				.Configure(configuration);
		}

		public virtual AutoPersistenceModel CreatePersistenceModel()
		{
			return new AutoPersistenceModel
			{
				Conventions =
					{
						DefaultLazyLoad = true,
						GetForeignKeyNameOfParent = (type => type.Name + "Id"),
						GetForeignKeyName = (info => info.Name + "Id"),
						GetTableName = (type => Inflector.Pluralize(type.Name)),
						GetManyToManyTableName =
							((child, parent) =>
							 Inflector.Pluralize(child.Name) + "To" + Inflector.Pluralize(parent.Name))
					}
			};
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