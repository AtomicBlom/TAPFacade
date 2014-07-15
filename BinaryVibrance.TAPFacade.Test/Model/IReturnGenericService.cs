using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade.Test.Model
{
	[ServiceContract]
	public interface IReturnGenericServiceAPM
	{
		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/IReturnGenericServiceAPM/GetApplicationInitializationSettings", ReplyAction = "http://tempuri.org/IReturnGenericServiceAPM/GetApplicationInitializationSettingsResponse")]
		IAsyncResult BeginGetApplicationInitializationSettings(AsyncCallback callback, object asyncState);

		IEnumerable<Setting> EndGetApplicationInitializationSettings(IAsyncResult result);
	}

	[ServiceContract]
	public interface IReturnGenericServiceTAP
	{
		[OperationContract]
		Task<IEnumerable<Setting>> GetApplicationInitializationSettingsAsync(string applicationName);
	}

	public class ReturnGenericService : IReturnGenericServiceTAP
	{
		public async Task<IEnumerable<Setting>> GetApplicationInitializationSettingsAsync(string applicationName)
		{
			return await TaskEx.Run(() => GetApplicationInitializationSettings(applicationName));
		}

		private IEnumerable<Setting> GetApplicationInitializationSettings(string applicationName)
		{
			return new[]
				{
					new Setting
						{
							Key = "Hey there",
							Target = "I can't remember what this is",
							TargetIndex = null,
							Value = "Sexy"
						}
				};
		}
	}

	public class Setting
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public string Target { get; set; }
		public int? TargetIndex { get; set; }
	}
}
