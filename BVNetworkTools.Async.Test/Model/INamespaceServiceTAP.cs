using System.ServiceModel;
using System.Threading.Tasks;

namespace BVNetworkTools.Async.Test.Model
{
	[ServiceContract(Namespace = "http://binaryvibrance.net/wsdl/")]
	internal interface INamespaceServiceTAP
	{
		[OperationContract]
		Task<string> GetString();
	}
}