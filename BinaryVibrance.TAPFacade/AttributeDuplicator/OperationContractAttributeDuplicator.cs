using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;

namespace BinaryVibrance.TAPFacade.AttributeDuplicator
{
	internal class OperationContractAttributeDuplicator : IAttributeDuplicator<OperationContractAttribute>
    {
        public CustomAttributeBuilder GetCustomAttributeBuilder(MemberInfo attachedMember, OperationContractAttribute attribute)
        {
			var attachedMemberType = attachedMember.DeclaringType;
			if (attachedMemberType == null)
			{
				throw new MemberAccessException(string.Format("Could not determine Declaring type of member {0}", attachedMember.Name));
			}
	        var serviceContracts = attachedMemberType.GetCustomAttributes(typeof (ServiceContractAttribute), false).SingleOrDefault() as ServiceContractAttribute;
			string @namespace = "http://tempuri.org/";
			if (serviceContracts != null && !string.IsNullOrWhiteSpace(serviceContracts.Namespace))
			{
				@namespace = serviceContracts.Namespace;
			}
            Type type = typeof (OperationContractAttribute);

			var name = attachedMember.Name;
			if (name.EndsWith("Async"))
			{
				name = name.Remove(name.Length - "Async".Length);
			}

            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			Debug.Assert(constructor != null);
            var constructorArgs = new object[] {};
	        var namedProperties = new Dictionary<PropertyInfo, object>
		        {
			        {type.GetProperty("AsyncPattern"), true},
			        {
				        type.GetProperty("Action"), !string.IsNullOrEmpty(attribute.Action)
					                                    ? attribute.Action
					                                    : string.Format("{0}{1}/{2}", @namespace, attachedMemberType.Name,
					                                                    name)
			        },
			        {
				        type.GetProperty("ReplyAction"), !string.IsNullOrEmpty(attribute.ReplyAction)
					                                         ? attribute.ReplyAction
					                                         : string.Format("{0}{1}/{2}Response", @namespace,
					                                                         attachedMemberType.Name,
					                                                         name)
			        }
		        };
			if (attribute.IsOneWay)
			{
				namedProperties.Add(type.GetProperty("IsOneWay"), attribute.IsOneWay);
			}
#if !SILVERLIGHT
	        if (attribute.HasProtectionLevel)
			{
				namedProperties.Add(type.GetProperty("ProtectionLevel"), attribute.ProtectionLevel);
			}
			if (!attribute.IsInitiating) //Default true
			{
				namedProperties.Add(type.GetProperty("IsInitiating"), attribute.IsInitiating);
			}
			if (attribute.IsTerminating)
			{
				namedProperties.Add(type.GetProperty("IsTerminating"), attribute.IsTerminating);
			}
			if (!string.IsNullOrEmpty(attribute.Name))
			{
				namedProperties.Add(type.GetProperty("Name"), attribute.Name);
			}
#endif

			return new CustomAttributeBuilder(constructor, constructorArgs, namedProperties.Keys.ToArray(), namedProperties.Values.ToArray());
        }
    }
}