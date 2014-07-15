using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using BinaryVibrance.TAPFacade.Test.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinaryVibrance.TAPFacade.Test
{
	[TestClass]
	public class TAPFacadeCreatorIntegrationTests
	{
		private ModuleBuilder _modBuilder;
		private static ServiceHost _basicServiceHost;
		private static NetTcpBinding _binding;
		private static string _address;

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			_basicServiceHost = new ServiceHost(typeof(BasicService));
			_binding = new NetTcpBinding(SecurityMode.None, true);
			_address = "net.tcp://localhost:9010/ProducerService";
			ServiceEndpoint endpoint = _basicServiceHost.AddServiceEndpoint(
				typeof(IBasicServiceTAP),
				_binding,
				_address);

			_basicServiceHost.Open();
			Console.WriteLine("The Producer service is running and is listening on:");
			Console.WriteLine("{0} ({1})", endpoint.Address, endpoint.Binding.Name);
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			if (_basicServiceHost != null)
			{
				if (_basicServiceHost.State == CommunicationState.Faulted)
				{
					_basicServiceHost.Abort();
				}
				else
				{
					_basicServiceHost.Close();
				}
			}
		}

		[TestInitialize]
		public void TestSetup()
		{
			TAPFacadeConfiguration.SetWCFChannelProvider(new DummyWCFChannelProvider());

			AppDomain thisDomain = Thread.GetDomain();
			var assemblyName = new AssemblyName { Name = typeof(IBasicServiceTAP).FullName + "_ServiceProxy" };
			var asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			_modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name, true);
		}

		[TestMethod]
		public async void BuildTapFacade_StandardServiceContract_CanCallBasicMethod()
		{
			var sut = new TAPFacadeCreator<IBasicServiceTAP>(_modBuilder);
			var channel = ChannelFactory<IBasicServiceAPM>.CreateChannel(_binding, new EndpointAddress(_address));
			var tapthingy = sut.BuildFacadeForApmChannel(typeof (IBasicServiceAPM), channel);

			var stringResult = await tapthingy.GetStringAsync();

			Assert.AreEqual("Test", stringResult);
		}

		[TestMethod]
		public async void BuildTapFacade_StandardServiceContract_CanCallMethodThatReturnsAGeneric()
		{
			var basicServiceHost = new ServiceHost(typeof(ReturnGenericService));
			const string address = "net.tcp://localhost:9012/ReturnGenericService";
			ServiceEndpoint endpoint = basicServiceHost.AddServiceEndpoint(
				typeof(IReturnGenericServiceTAP),
				_binding,
				address);

			basicServiceHost.Open();
			Console.WriteLine("The Producer service is running and is listening on:");
			Console.WriteLine("{0} ({1})", endpoint.Address, endpoint.Binding.Name);

			var sut = new TAPFacadeCreator<IReturnGenericServiceTAP>(_modBuilder);
			var channel = ChannelFactory<IReturnGenericServiceAPM>.CreateChannel(_binding, new EndpointAddress(address));
			var tapthingy = sut.BuildFacadeForApmChannel(typeof(IReturnGenericServiceAPM), channel);

			var results = await tapthingy.GetApplicationInitializationSettingsAsync("MyApplication");
			
			basicServiceHost.Close();
			Assert.AreEqual(1, results.Count());
		}

		[TestMethod]
		public async void BuildTapFacade_AutoDiscoverKnownTypesServiceContract_HonoursServiceKnownType()
		{
			var basicServiceHost = new ServiceHost(typeof(AutoDiscoverKnownTypesServiceTAP));
			const string address = "net.tcp://localhost:9011/AutoDiscoverKnownTypesService";
			ServiceEndpoint endpoint = basicServiceHost.AddServiceEndpoint(
				typeof(IBasicServiceTAP),
				_binding,
				address);

			basicServiceHost.Open();
			Console.WriteLine("The Producer service is running and is listening on:");
			Console.WriteLine("{0} ({1})", endpoint.Address, endpoint.Binding.Name);

			var sut = new TAPFacadeCreator<IAutoDiscoverKnownTypesServiceTAP>(_modBuilder);
			var channel = ChannelFactory<IAutoDiscoverKnownTypesServiceAPM>.CreateChannel(_binding, new EndpointAddress(address));
			var tapthingy = sut.BuildFacadeForApmChannel(typeof(IAutoDiscoverKnownTypesServiceAPM), channel);

			var animalSqueezed = await tapthingy.SqueezeAnimal(new Dog());

			basicServiceHost.Close();

			Assert.IsTrue(animalSqueezed);
		}
	}
}
