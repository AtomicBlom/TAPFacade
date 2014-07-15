using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade.Test.Model
{
	[ServiceContract]
	public interface IBasicServiceAPM
	{
		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/IBasicServiceAPM/GetString", ReplyAction = "http://tempuri.org/IBasicServiceAPM/GetStringResponse")]
		IAsyncResult BeginGetString(AsyncCallback callback, object asyncState);

		string EndGetString(IAsyncResult result);
	}

	[ServiceContract]
	public interface IBasicServiceTAP
	{
		[OperationContract]
		Task<string> GetStringAsync();
	}

	public class BasicService : IBasicServiceTAP
	{
		public Task<string> GetStringAsync()
		{
			return TaskEx.FromResult("Test");
		}
	}
}