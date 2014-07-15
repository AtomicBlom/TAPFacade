using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;

namespace BinaryVibrance.NetworkTools.Async.AttributeDuplicator
{
    internal class FaultContractAttributeDuplicator : IAttributeDuplicator<FaultContractAttribute>
    {
		public CustomAttributeBuilder GetCustomAttributeBuilder(MemberInfo attachedMember, FaultContractAttribute attribute)
        {
			var attachedMemberType = attachedMember.DeclaringType;
			if (attachedMemberType == null)
			{
				throw new MemberAccessException(string.Format("Could not determine Declaring type of member {0}", attachedMember.Name));
			}
			var serviceContracts = attachedMemberType.GetCustomAttributes(typeof(ServiceContractAttribute), false).SingleOrDefault() as ServiceContractAttribute;
			var @namespace = "http://tempuri.org/";
			if (serviceContracts != null && !string.IsNullOrWhiteSpace(serviceContracts.Namespace))
			{
				@namespace = serviceContracts.Namespace;
			}
			
			var name = attachedMember.Name;
			if (name.EndsWith("Async"))
			{
				name = name.Remove(name.Length - "Async".Length);
			}

			//[System.ServiceModel.FaultContractAttribute(
			//	typeof(MyBudget.CompositeClient.Module.Account.BudgetService.InvalidPaymentDateFaultContract), 
			//	Action="http://tempuri.org/IBudgetService/ModifyBudgetInvalidPaymentDateFaultContractFault", 
			//	Name="InvalidPaymentDateFaultContract", 
			//	Namespace="http://schemas.datacontract.org/2004/07/MyBudget.Domain.Budget.Fault")]
	        var type = typeof (FaultContractAttribute);
	        ConstructorInfo constructor = type.GetConstructor(new[] {typeof (Type)});
			if (constructor == null)
			{
				throw new MemberAccessException("Could not locate Constructor of FaultContractAttribute");
			}
			var constructorArgs = new object[] { attribute.DetailType };
			
			var namedProperties = new Dictionary<PropertyInfo, object>();
			if (!string.IsNullOrEmpty(attribute.Action))
			{
				namedProperties.Add(type.GetProperty("Action"), attribute.Action);
			}
			else
			{
				namedProperties.Add(type.GetProperty("Action"), !string.IsNullOrEmpty(attribute.Action)
														? attribute.Action
														: string.Format("{0}{1}/{2}{3}Fault", @namespace, attachedMemberType.Name,
																		name, attribute.DetailType.Name));
			}

			namedProperties.Add(type.GetProperty("Name"),
				string.IsNullOrEmpty(attribute.Name) ? attribute.DetailType.Name : attribute.Name);

			namedProperties.Add(type.GetProperty("Namespace"),
				string.IsNullOrEmpty(attribute.Namespace)
					? string.Format("http://schemas.datacontract.org/2004/07/{0}", attribute.DetailType.Namespace)
					: attribute.Namespace);

#if !SILVERLIGHT
			if (attribute.HasProtectionLevel)
			{
				namedProperties.Add(type.GetProperty("ProtectionLevel"), attribute.ProtectionLevel);
			}
#endif
			return new CustomAttributeBuilder(constructor, constructorArgs, namedProperties.Keys.ToArray(), namedProperties.Values.ToArray());
        }
    }
}