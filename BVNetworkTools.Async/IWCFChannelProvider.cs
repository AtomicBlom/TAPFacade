using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace BinaryVibrance.NetworkTools.Async
{
	/// <summary>
	/// Provides a Channel, usually as created by ChannelFactory<>.
	/// </summary>
	public interface IWCFChannelProvider
	{
		/// <summary>
		/// Builds the WCF Channel
		/// </summary>
		/// <param name="apmInterface">The Asynchronous Programming Model based interface to wrap</param>
		/// <param name="binding">The binding to use</param>
		/// <param name="endpoint">The Endpoint to connect to</param>
		/// <returns>The created channel</returns>
		object BuildWCFChannel(Type apmInterface, Binding binding, EndpointAddress endpoint);
	}
}