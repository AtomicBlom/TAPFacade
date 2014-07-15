using System;
using System.IO;
using System.ServiceModel;
using BVNetworkTools.Async.Test.Model;
using BinaryVibrance.NetworkTools.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BVNetworkTools.Async.Test
{
	[TestClass]
	public class TAPFacadeIntegrationTests
	{
		[TestMethod]
		public void TAPFacade_SaveFunctionality_OutputsFile()
		{
			var binding = new NetTcpBinding(SecurityMode.None, true);
			const string address = "net.tcp://localhost:9010/ProducerService";
			var endpoint = new EndpointAddress(address);

			var currentDir = Environment.CurrentDirectory;
			
			TAPFacadeConfiguration.SaveDynamicAssemblyTo(currentDir);
			TAPFacade<IBasicServiceTAP>.Create(binding, endpoint);

			Assert.IsTrue(File.Exists(Path.Combine(currentDir, string.Format("{0}_ServiceProxy_Dynlib.dll", typeof(IBasicServiceTAP).FullName))));
			Assert.IsTrue(File.Exists(Path.Combine(currentDir, string.Format("{0}_ServiceProxy_Module.dll", typeof(IBasicServiceTAP).FullName))));
		}

		[TestMethod]
		public void TAPFacade_ThrowsManagedFault_ReceivesManagedFault()
		{
			var basicServiceHost = new ServiceHost(typeof(FaultContractServiceTAP));
			var binding = new NetTcpBinding(SecurityMode.None, true);
			const string address = "net.tcp://localhost:9012/ProducerService";
			basicServiceHost.AddServiceEndpoint(
				typeof(IFaultContractServiceTAP),
				binding,
				address);

			basicServiceHost.Open();
			
			try
			{
				var tapFacade = TAPFacade<IFaultContractServiceTAP>.Create(binding, new EndpointAddress(address));
				var result = tapFacade.GetStringAsync();
				result.Wait();

				Assert.Fail();
			}
			catch (Exception e)
			{
				Assert.IsInstanceOfType(e, typeof(AggregateException));
				Assert.IsInstanceOfType(e.InnerException, typeof(FaultException<MyFault>));
			}
			
			basicServiceHost.Close();
		}
	}
}
