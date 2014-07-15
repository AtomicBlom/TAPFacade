using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.ServiceModel;

namespace BinaryVibrance.TAPFacade.AttributeDuplicator
{
	internal class ServiceKnownTypeAttributeDuplicator : IAttributeDuplicator<ServiceKnownTypeAttribute>
    {
		public CustomAttributeBuilder GetCustomAttributeBuilder(MemberInfo memberInfo, ServiceKnownTypeAttribute attribute)
        {
            Type type = typeof (ServiceKnownTypeAttribute);
			
			ConstructorInfo con;
			object[] constructorArgs;
			//Get Types from method in another class
			if (attribute.DeclaringType != null && !string.IsNullOrEmpty(attribute.MethodName))
			{
				con = type.GetConstructor(new[] { typeof(string), typeof(Type) });
				constructorArgs = new object[] { attribute.MethodName, attribute.DeclaringType };
			}
				//Get Types from method in current class
			else if (!string.IsNullOrEmpty(attribute.MethodName))
			{
				con = type.GetConstructor(new[] { typeof(string) });
				constructorArgs = new object[] { attribute.MethodName};
			}
				//Define Specific Type
			else
			{
				Debug.Assert(attribute.Type != null);
				con = type.GetConstructor(new[] { typeof(Type) });
				constructorArgs = new object[] { (attribute).Type };
			}

            

            return new CustomAttributeBuilder(con, constructorArgs);
        }
    }
}