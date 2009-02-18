using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;


namespace Rhino.ServiceBus.Host
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public string ServiceName
		{
			get
			{
				return serviceInstaller1.ServiceName;
			}
		}

	    public ServiceAccount Account
	    {
	        get
	        {
	            return serviceProcessInstaller1.Account;
	        }
	    }

		public ProjectInstaller()
		{
			InitializeComponent();
		}

		public string DisplayName
		{
			set
			{
				serviceInstaller1.DisplayName = value;
				serviceInstaller1.ServiceName = value;
			}
		}

		public string Description
		{
			set
			{
				this.serviceInstaller1.Description = value;
			}
		}


	}
}
