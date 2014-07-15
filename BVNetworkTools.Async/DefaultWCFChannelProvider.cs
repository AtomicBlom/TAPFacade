using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace BinaryVibrance.NetworkTools.Async
{
	internal class DefaultWCFChannelProvider : IWCFChannelProvider
	{
		public object BuildWCFChannel(Type apmInterface, Binding binding, EndpointAddress endpoint)
		{
			Type channelFactory = typeof(ChannelFactory<>).MakeGenericType(apmInterface);
			MethodInfo createChannel = channelFactory.GetMethod("CreateChannel",
													new[] { typeof(Binding), typeof(EndpointAddress) });
			return createChannel.Invoke(null, new object[] { binding, endpoint });
		}
	}
}
