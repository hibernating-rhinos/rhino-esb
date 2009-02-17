using Castle.Core;
using Castle.MicroKernel;
using NHibernate;

namespace Rhino.ServiceBus.NHibernate.Resolvers
{
    public class SessionResolver : ISubDependencyResolver
    {
        private readonly IKernel kernel;

        public SessionResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        private ISessionFactory SessionFactory
        {
            get { return kernel.Resolve<ISessionFactory>(); }
        }

        #region ISubDependencyResolver Members

        public object Resolve(CreationContext context, ISubDependencyResolver parentResolver, ComponentModel model,
                              DependencyModel dependency)
        {
            if (dependency.TargetType == typeof (ISession))
                return SessionFactory.OpenSession();
            return SessionFactory.OpenStatelessSession();
        }

        public bool CanResolve(CreationContext context, ISubDependencyResolver parentResolver, ComponentModel model,
                               DependencyModel dependency)
        {
            return dependency.TargetType == typeof (ISession) ||
                   dependency.TargetType == typeof (IStatelessSession);
        }

        #endregion
    }
}