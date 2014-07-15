using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BinaryVibrance.TAPFacade.AttributeDuplicator;

namespace BinaryVibrance.TAPFacade
{
	internal class ApmInterfaceCreator<TContract>
	{
		private readonly ModuleBuilder _modBuilder;

		public ApmInterfaceCreator(ModuleBuilder modBuilder)
		{
			_modBuilder = modBuilder;
			//_attributeDecorator = ServiceLocator.Current.GetInstance<IAttributeDecorator>();
		}

		private void CopyAttributes(MemberInfo member, Action<CustomAttributeBuilder> addAttributeAction)
		{
			foreach (Attribute attribute in member.GetCustomAttributes(false).Cast<Attribute>())
			{
				IAttributeDuplicator attributeDuplicatorProvider;

				if (TAPFacadeConfiguration.AttributeProviders.TryGetValue(attribute.GetType(), out attributeDuplicatorProvider))
				{
					var builder = (CustomAttributeBuilder) attributeDuplicatorProvider.GetType()
					                                                                  .GetMethod("GetCustomAttributeBuilder")
					                                                                  .Invoke(attributeDuplicatorProvider,
					                                                                          new object[] {member, attribute});

					addAttributeAction(builder);
				}
			}
		}

		public Type BuildApmInterface()
		{
			lock (DynamicTypeCache.ObjAPMLock)
			{
				Type cachedInstance;
				if (DynamicTypeCache.CreatedAPMInterfaces.TryGetValue(typeof (TContract), out cachedInstance))
				{
					return cachedInstance;
				}

				TypeBuilder typeBuilder = _modBuilder.DefineType(typeof (TContract).FullName + "_ProxyContract",
				                                                 TypeAttributes.Interface | TypeAttributes.Abstract |
				                                                 TypeAttributes.Public);


				CopyAttributes(typeof (TContract), typeBuilder.SetCustomAttribute);

				MethodInfo[] actualMethods = typeof (TContract).GetMethods();
				foreach (MethodInfo methodInfo in actualMethods)
				{
					int parameterPosition = 0;
					Dictionary<int, KeyValuePair<Type, string>> parameterTypes = methodInfo
						.GetParameters()
						.ToDictionary(parameter => ++parameterPosition,
						              parameter => new KeyValuePair<Type, string>(parameter.ParameterType, parameter.Name));

					parameterTypes.Add(++parameterPosition, new KeyValuePair<Type, string>(typeof (AsyncCallback), "callback"));
					parameterTypes.Add(++parameterPosition, new KeyValuePair<Type, string>(typeof (object), "asyncState"));

					var name = methodInfo.Name;
					if (name.EndsWith("Async"))
					{
						name = name.Remove(name.Length - "Async".Length);
					}
					MethodBuilder beginMethodBuilder = typeBuilder.DefineMethod("Begin" + name,
					                                                            MethodAttributes.Public | MethodAttributes.Abstract |
					                                                            MethodAttributes.Virtual,
					                                                            typeof (IAsyncResult),
					                                                            parameterTypes.Select(k => k.Value.Key).ToArray());

					CopyAttributes(methodInfo, beginMethodBuilder.SetCustomAttribute);

					foreach (var entry in parameterTypes)
					{
						beginMethodBuilder.DefineParameter(entry.Key, ParameterAttributes.None, entry.Value.Value);
					}

					Type returnType = methodInfo.ReturnType;
					if (returnType == typeof (Task))
					{
						returnType = null;
					}
					else if (returnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof (Task<>))
					{
						returnType = methodInfo.ReturnType.GetGenericArguments()[0];
					}
					MethodBuilder endMethodBuilder = typeBuilder.DefineMethod("End" + name,
					                                                          MethodAttributes.Public | MethodAttributes.Abstract |
					                                                          MethodAttributes.Virtual,
					                                                          returnType, new[] {typeof (IAsyncResult)});
					endMethodBuilder.DefineParameter(1, ParameterAttributes.None, "result");
				}
				Type createdType = typeBuilder.CreateType();

				DynamicTypeCache.CreatedAPMInterfaces.Add(typeof(TContract), createdType);
				return createdType;
			}
		}
	}
}