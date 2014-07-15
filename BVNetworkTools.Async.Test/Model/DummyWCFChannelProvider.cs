using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using BinaryVibrance.NetworkTools.Async;

namespace BVNetworkTools.Async.Test.Model
{
	public class DummyWCFChannelProvider : IWCFChannelProvider
	{
		public object BuildWCFChannel(Type apmInterface, Binding binding, EndpointAddress endpoint)
		{
			return new BasicServiceAPMImpl();
		}
	}

	public class BasicServiceAPMImpl : IBasicServiceAPM
	{
		public IAsyncResult BeginGetString(AsyncCallback callback, object asyncState)
		{
			throw new NotImplementedException();
		}

		public string EndGetString(IAsyncResult result)
		{
			throw new NotImplementedException();
		}
	}
}