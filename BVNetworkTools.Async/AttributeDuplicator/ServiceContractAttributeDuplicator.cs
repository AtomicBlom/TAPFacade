using System;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;

namespace BinaryVibrance.TAPFacade.AttributeDuplicator
{
	internal class ServiceContractAttributeDuplicator : IAttributeDuplicator<ServiceContractAttribute>
    {
		public CustomAttributeBuilder GetCustomAttributeBuilder(MemberInfo memberInfo, ServiceContractAttribute attribute)
        {
            Type type = typeof (ServiceContractAttribute);

            ConstructorInfo con = type.GetConstructor(Type.EmptyTypes);
            var constructorArgs = new object[] {};

            return new CustomAttributeBuilder(con, constructorArgs);
        }
    }
}