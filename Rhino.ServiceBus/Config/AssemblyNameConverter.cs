using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Reflection;

namespace Rhino.ServiceBus.Config
{
    /// <summary>
    /// AssemblyNameConverter
    /// </summary>
    internal sealed class AssemblyNameConverter : ConfigurationConverterBase
    {
        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            var assembly = GetAssembly((string)data, false);
            if (assembly == null)
                throw new ArgumentException(string.Format("Type_cannot_be_resolved {0}", (string)data));
            return assembly;
        }

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            if (!(value is Type))
                ValidateType(value, typeof(Assembly));
            return (value != null ? ((Assembly)value).FullName : null);
        }

        private void ValidateType(object value, Type expected)
        {
            if (value != null && value.GetType() != expected)
                throw new ArgumentException(string.Format("Converter_unsupported_value_type {0}", expected.Name));
        }

        private static Assembly GetAssembly(string assemblyString, bool throwOnError)
        {
            var fileAsUri = (assemblyString.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ? new Uri(assemblyString) : null);
            if (fileAsUri == null || !fileAsUri.IsFile)
                return Assembly.Load(assemblyString);
            return Assembly.LoadFile(fileAsUri.LocalPath);
        }
    }
}