using System;
using System.IO;
using System.Reflection;
using System.Web;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using System.Linq;

namespace Rhino.ServiceBus.Util
{
	public static class CurrentMessage
	{
		private static readonly PropertyInfo currentContext;
		private static readonly Hashtable<Type, Func<object, string>> type2ToString = new Hashtable<Type, Func<object, string>>();

		static CurrentMessage()
		{
			try
			{
				var profilerAsm = Assembly.Load("HibernatingRhinos.Profiler.Appender");
				if(profilerAsm == null)
					return;
				var profilerIntegration = profilerAsm.GetType("HibernatingRhinos.Profiler.Appender.ProfilerIntegration");
				if(profilerIntegration == null)
					return;
				currentContext = profilerIntegration.GetProperty("CurrentSessionContext");
			}
			catch
			{
				// we ignore if not found / failed
			}
		}

		public static IDisposable Track(CurrentMessageInformation msg)
		{
			if(currentContext == null)
				return null;
			if(msg == null )
				return null;

			var str = string.Join(", ", msg.AllMessages.Select(o => ToString(o)).ToArray());
			currentContext.SetValue(null, str, null);
			return new DisposableAction(() => currentContext.SetValue(null, null, null));
		}

		private static string ToString(object msg)
		{
			Func<object, string> val = null;
			var type = msg.GetType();
			type2ToString.Read(reader => reader.TryGetValue(type, out val));
			if (val != null)
				return val(msg);

			type2ToString.Write(writer =>
			{
				var overrideToString = type.GetMethod("ToString").DeclaringType != typeof(object);
				if(overrideToString)
					val = o => o.ToString();
				else
					val = o => type.Name;
				writer.Add(type, val);
			});

			return val(msg);
		}
	}
}