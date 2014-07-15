using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;
using System.Threading;
using BinaryVibrance.TAPFacade.Test.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinaryVibrance.TAPFacade.Test
{
	[TestClass]
	public class ApmInterfaceCreatorIntegrationTests
	{
		private ModuleBuilder _modBuilder;

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
		public void BuildApmInterface_StandardServiceContract_BeginMethodExists()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var beginMethod = apmInterface.GetMethod("BeginGetString");

			Assert.IsNotNull(beginMethod);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_InterfaceHasServiceContract()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var serviceContractAttribute = apmInterface.GetCustomAttributes(typeof(ServiceContractAttribute), false).Cast<ServiceContractAttribute>().SingleOrDefault();
			Assert.IsNotNull(serviceContractAttribute);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_BeginMethodHasOperationContract()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(OperationContractAttribute), false).OfType<OperationContractAttribute>().Single();
			Assert.IsNotNull(operationContractAttribute);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_BeginMethodHasAsyncPatternTrue()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(OperationContractAttribute), false).OfType<OperationContractAttribute>().Single();
			Assert.IsTrue(operationContractAttribute.AsyncPattern);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_BeginMethodHasAction()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(OperationContractAttribute), false).OfType<OperationContractAttribute>().Single();
			Assert.AreEqual("http://tempuri.org/IBasicServiceTAP/GetString", operationContractAttribute.Action);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_BeginMethodHasReplyAction()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(OperationContractAttribute), false).OfType<OperationContractAttribute>().Single();
			Assert.AreEqual("http://tempuri.org/IBasicServiceTAP/GetStringResponse", operationContractAttribute.ReplyAction);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_EndMethodExists()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var endMethod = apmInterface.GetMethod("BeginGetString");
			Assert.IsNotNull(endMethod);
		}

		[TestMethod]
		public void BuildApmInterface_StandardServiceContract_RemovesAsyncFromMethodName()
		{
			var sut = new ApmInterfaceCreator<IBasicServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var beginMethodThatShouldExist = apmInterface.GetMethod("BeginGetString");
			var beginMethodThatShouldNotExist = apmInterface.GetMethod("BeginGetStringAsync");
			Assert.IsNotNull(beginMethodThatShouldExist);
			Assert.IsNull(beginMethodThatShouldNotExist);
		}

		[TestMethod]
		public void BuildApmInterface_CustomNamespaceServiceContract_ActionIncludesCustomNamespace()
		{
			var sut = new ApmInterfaceCreator<INamespaceServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(OperationContractAttribute), false).OfType<OperationContractAttribute>().Single();
			Assert.AreEqual("http://binaryvibrance.net/wsdl/INamespaceServiceTAP/GetString", operationContractAttribute.Action);
		}

		[TestMethod]
		public void BuildApmInterface_FaultContractServiceContract_ReconstructsFaultContractAttributes()
		{
			var sut = new ApmInterfaceCreator<IFaultContractServiceTAP>(_modBuilder);
			var apmInterface = sut.BuildApmInterface();
			var operationContractAttribute = apmInterface.GetMethod("BeginGetString").GetCustomAttributes(typeof(FaultContractAttribute), false).OfType<FaultContractAttribute>().Single();
			Assert.AreEqual(typeof(MyFault), operationContractAttribute.DetailType);
		}
	}
}
