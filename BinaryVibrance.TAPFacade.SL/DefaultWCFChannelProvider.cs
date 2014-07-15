using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace BinaryVibrance.TAPFacade
{
	internal class DefaultWCFChannelProvider : IWCFChannelProvider
	{
		public object BuildWCFChannel(Type apmInterface, Binding binding, EndpointAddress endpoint)
		{
			Type channelFactory = typeof(ChannelFactory<>).MakeGenericType(apmInterface);
			var constructor = channelFactory.GetConstructor(new[] { typeof(Binding), typeof(EndpointAddress) });
			Debug.Assert(constructor != null);
			var channelFactoryObj = constructor.Invoke(new object[] { binding, endpoint });
			return channelFactory.GetMethod("CreateChannel", Type.EmptyTypes).Invoke(channelFactoryObj, new object[0]);
		}
	}
}
