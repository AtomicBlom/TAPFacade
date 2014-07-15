using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade.Test.Model
{
	[ServiceContract]
	public interface IFaultContractServiceTAP
	{
		[OperationContract]
		[FaultContract(typeof(MyFault))]
		Task<string> GetStringAsync();
	}

	public class FaultContractServiceTAP : IFaultContractServiceTAP
	{
		public Task<string> GetStringAsync()
		{
			throw new FaultException<MyFault>(new MyFault());
		}
	}

	[DataContract]
	public class MyFault
	{

	}
}