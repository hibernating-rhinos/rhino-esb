using Rhino.ServiceBus.Config;
namespace Rhino.ServiceBus.Impl
{
    public class OnewayRhinoServiceBusConfiguration : AbstractRhinoServiceBusConfiguration
    {
        protected override void ApplyConfiguration()
        {
            var assemblies = ConfigurationSection.Assemblies;
            if (assemblies != null)
                foreach (AssemblyElement assembly in assemblies)
                    scanAssemblies.Add(assembly.Assembly);
        }

        public MessageOwner[] MessageOwners { get; set; }
    }
}