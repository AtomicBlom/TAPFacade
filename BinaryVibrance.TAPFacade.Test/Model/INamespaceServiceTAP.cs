using System.ServiceModel;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade.Test.Model
{
	[ServiceContract(Namespace = "http://binaryvibrance.net/wsdl/")]
	internal interface INamespaceServiceTAP
	{
		[OperationContract]
		Task<string> GetString();
	}
}