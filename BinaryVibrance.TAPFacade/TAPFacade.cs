using System;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace BinaryVibrance.TAPFacade
{
	/// <summary>
	/// Enabled TAP based WCF services
	/// </summary>
	/// <typeparam name="TContract">The TAP Contract</typeparam>
	public static class TAPFacade<TContract>
	{
		/// <summary>
		/// Creates a APM contract, then builds a WCF Channel around it, and finally wraps it all with a TAP compatible facade
		/// </summary>
		/// <param name="binding">The Binding to use</param>
		/// <param name="endpoint">The Endpoint to communicate with</param>
		/// <returns>The TAP Facade to the built service</returns>
		public static TContract Create(Binding binding, EndpointAddress endpoint)
		{
			var assemblyName = new AssemblyName { Name = typeof(TContract).FullName + "_ServiceProxy" };
			AppDomain thisDomain = Thread.GetDomain();
// ReSharper disable JoinDeclarationAndInitializer
			AssemblyBuilder asmBuilder;
			ModuleBuilder modBuilder;
// ReSharper restore JoinDeclarationAndInitializer
#if !SILVERLIGHT
			
			if (TAPFacadeConfiguration.SaveResult)
			{
				asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, TAPFacadeConfiguration.SaveLocation);
				modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name + "_Module", assemblyName.FullName + "_Module.dll");
			}
			else
			{
				asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
				modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name, true);
			}
#else
			asmBuilder = thisDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			modBuilder = asmBuilder.DefineDynamicModule(assemblyName.Name, true);
#endif
			var apmInterfaceCreator = new ApmInterfaceCreator<TContract>(modBuilder);
			var tapFacadeCreator = new TAPFacadeCreator<TContract>(modBuilder);
			
			Type slInterface = apmInterfaceCreator.BuildApmInterface();
			object channel = TAPFacadeConfiguration.ChannelProvider.BuildWCFChannel(slInterface, binding, endpoint);
			TContract facade = tapFacadeCreator.BuildFacadeForApmChannel(slInterface, channel);

#if !SILVERLIGHT
			if (TAPFacadeConfiguration.SaveResult)
			{
				asmBuilder.Save(assemblyName.FullName + "_Dynlib.dll");
			}
#endif
			return facade;
		}
	}
}
