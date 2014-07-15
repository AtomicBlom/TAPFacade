using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade.Test.Model
{
	[ServiceContract]
	public interface IAutoDiscoverKnownTypesServiceAPM
	{
		[OperationContract(AsyncPattern = true, Action = "http://tempuri.org/IAutoDiscoverKnownTypesServiceAPM/SqueezeAnimal", ReplyAction = "http://tempuri.org/IAutoDiscoverKnownTypesServiceAPM/SqueezeAnimalResponse")]
		IAsyncResult BeginSqueezeAnimal(AAnimal animal, AsyncCallback callback, object asyncState);

		bool EndSqueezeAnimal(IAsyncResult result);
	}

	[ServiceContract]
	public interface IAutoDiscoverKnownTypesServiceTAP
	{
		[OperationContract]
		[ServiceKnownType("GetAnimalTypes", typeof(AutoDiscoverKnownTypesServiceTAP))]
		Task<bool> SqueezeAnimal(AAnimal animal);
	}

	public class AutoDiscoverKnownTypesServiceTAP : IAutoDiscoverKnownTypesServiceTAP
	{
		public Task<bool> SqueezeAnimal(AAnimal animal)
		{
			return TaskEx.FromResult(true);
		}

		public static IEnumerable<Type> GetAnimalTypes(ICustomAttributeProvider provider)
		{
			yield return typeof (Dog);
		}
	}

	public abstract class AAnimal
	{
		
	}
	
	public class Dog : AAnimal
	{
		
	}
}