using System;
using System.Collections.Generic;
using System.Linq;
using BinaryVibrance.NetworkTools.Async.AttributeDuplicator;

namespace BinaryVibrance.NetworkTools.Async
{
	/// <summary>
	/// Provides Configuration for the AsyncFactory
	/// </summary>
	public static class TAPFacadeConfiguration
	{
		internal static readonly Dictionary<Type, IAttributeDuplicator> AttributeProviders;
		internal static IWCFChannelProvider ChannelProvider;

		static TAPFacadeConfiguration()
		{
			AttributeProviders = new Dictionary<Type, IAttributeDuplicator>();
			AddAttributeHandler<FaultContractAttributeDuplicator>();
			AddAttributeHandler<OperationContractAttributeDuplicator>();
			AddAttributeHandler<ServiceContractAttributeDuplicator>();
			AddAttributeHandler<ServiceKnownTypeAttributeDuplicator>();

			ChannelProvider = new DefaultWCFChannelProvider();
		}

		/// <summary>
		/// Adds an Attribute Duplicator to the registry
		/// </summary>
		/// <typeparam name="T">The type of the Duplicator</typeparam>
		public static void AddAttributeHandler<T>() where T : IAttributeDuplicator, new()
		{
			AddAttributeHandler(new T());
		}

		/// <summary>
		/// Adds an instance of a Attribute Duplicator to the registry
		/// </summary>
		/// <param name="duplicator">An instance of a duplicator</param>
		public static void AddAttributeHandler(IAttributeDuplicator duplicator)
		{
			var type = duplicator.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAttributeDuplicator<>))
			                     .Select(p => p.GetGenericArguments()[0]).Single();
			AttributeProviders.Add(type, duplicator);
		}

		/// <summary>
		/// Provides an alternative WCF Channel Provider
		/// </summary>
		/// <param name="wcfChannelProvider">The alternative provider</param>
		public static void SetWCFChannelProvider(IWCFChannelProvider wcfChannelProvider)
		{
			ChannelProvider = wcfChannelProvider;
		}

#if !SILVERLIGHT
		public static bool SaveResult { get; private set;}
		internal static string SaveLocation { get; set; }
		public static void SaveDynamicAssemblyTo(string directory)
		{
			SaveLocation = directory;
			SaveResult = true;
		}
#endif
	}
}