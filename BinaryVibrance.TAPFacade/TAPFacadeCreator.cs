using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BinaryVibrance.TAPFacade
{
	internal class TAPFacadeCreator<TContract>
	{
		private readonly ModuleBuilder _moduleBuilder;

		public TAPFacadeCreator(ModuleBuilder moduleBuilder)
		{
			_moduleBuilder = moduleBuilder;
		}

		public TContract BuildFacadeForApmChannel(Type apmInterface, object channel)
		{
			lock (DynamicTypeCache.ObjFacadeLock)
			{
				Type cachedInstance;
				if (DynamicTypeCache.CreatedTAPFacades.TryGetValue(typeof (TContract), out cachedInstance))
				{
					ConstructorInfo constructorInfo2 = cachedInstance.GetConstructor(new[] { channel.GetType() });
					Debug.Assert(constructorInfo2 != null);
					return (TContract)constructorInfo2.Invoke(new[] { channel });
				}

				var facadeDecl = new FacadeDeclaration
					{
						APMInterface = apmInterface,
						TypeBuilder = _moduleBuilder.DefineType(typeof (TContract).FullName + "_Facade",
						                                        TypeAttributes.Public | TypeAttributes.Class |
						                                        TypeAttributes.BeforeFieldInit)
					};
				facadeDecl.TypeBuilder.AddInterfaceImplementation(typeof (TContract));

				facadeDecl.m_ctor = facadeDecl.TypeBuilder.DefineConstructor(
					MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
					MethodAttributes.RTSpecialName,
					CallingConventions.Standard, new[] {channel.GetType()});
				facadeDecl.m_ctor.DefineParameter(1, ParameterAttributes.None, "client");

				//Add proxied channel
				facadeDecl.f_RealClient = facadeDecl.TypeBuilder.DefineField("_realClient", apmInterface,
				                                                             FieldAttributes.Private | FieldAttributes.InitOnly);


				MethodInfo[] actualMethods = typeof (TContract).GetMethods();
				IEnumerable<FacadeMethodDeclarationContext> validMethods =
					actualMethods.Select(methodInfo => new FacadeMethodDeclarationContext(methodInfo, facadeDecl))
					             .Where(context => context.CommonlyReferencedTypes.IsValidAsyncMethod);
				foreach (FacadeMethodDeclarationContext context in validMethods)
				{
					//Create an async result handler

					DeclareClosureClass(context);
					DeclareStateMachineClass(context);
					DeclareFacade_TargetMethod(context);
					DeclareFacade_TargetAsyncEndMethod(context);

					ImplementFacade_TargetMethod(context);
					ImplementFacade_AsyncEndMethod(context);
					ImplementClosure_AsyncResultMethod(context);
					ImplementStateMachine_MoveNextMethod(context);
					ImplementStateMachine_SetStateMachineMethod(context);

					context.Closure.TypeBuilder.CreateType();
					context.StateMachine.TypeBuilder.CreateType();
				}

				ImplementFacade_Constructor(facadeDecl);

				Type createdType = facadeDecl.TypeBuilder.CreateType();
				DynamicTypeCache.CreatedTAPFacades.Add(typeof(TContract), createdType);
				ConstructorInfo constructorInfo = createdType.GetConstructor(new[] {channel.GetType()});
				Debug.Assert(constructorInfo != null);
				var facadeImpl = (TContract) constructorInfo.Invoke(new[] {channel});

				

				return facadeImpl;
			}
		}

		private void DeclareFacade_TargetAsyncEndMethod(FacadeMethodDeclarationContext context)
		{
			MethodBuilder methodBuilder = context.m_EndAsync =
			                              context.FacadeDeclaration.TypeBuilder.DefineMethod(
				                              string.Format("<{0}>b__{1}", context.Name,
				                                            context.FacadeDeclaration.GetNextClosureId()),
				                              MethodAttributes.Private | MethodAttributes.HideBySig,
				                              context.CommonlyReferencedTypes.ReturnType, new[] {typeof (IAsyncResult)});

			methodBuilder.DefineParameter(1, ParameterAttributes.None, "result");
			methodBuilder.SetCustomAttribute(context.CommonlyReferencedTypes.GetCompilerGeneratedAttribute());
		}

		private void DeclareStateMachineClass(FacadeMethodDeclarationContext context)
		{
			StateMachineDeclaration stateMachine = context.StateMachine;
			TypeBuilder typeBuilder = stateMachine.TypeBuilder =
			                          context.FacadeDeclaration.TypeBuilder.DefineNestedType(
				                          string.Format("<{0}>d__{1}", context.Name, context.FacadeDeclaration.GetNextClosureId()),
				                          TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed |
				                          TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPrivate, typeof (ValueType));
			typeBuilder.AddInterfaceImplementation(typeof (IAsyncStateMachine));
			typeBuilder.SetCustomAttribute(context.CommonlyReferencedTypes.GetCompilerGeneratedAttribute());


			Type asyncTaskMethodBuilder = context.CommonlyReferencedTypes.AsyncTaskMethodBuilderOfReturnType;
			Type taskAwaiterOfReturnType = context.CommonlyReferencedTypes.TaskAwaiterOfReturnType;

			stateMachine.f_state = typeBuilder.DefineField("<>1__state", typeof (Int32), FieldAttributes.Public);
			stateMachine.f_thisOfContainer = typeBuilder.DefineField("<>4__this", context.FacadeDeclaration.TypeBuilder,
			                                                         FieldAttributes.Public);

			stateMachine.f_builder = typeBuilder.DefineField("<>t__builder", asyncTaskMethodBuilder, FieldAttributes.Public);
			stateMachine.f_stack = typeBuilder.DefineField("<>t__stack", typeof (object), FieldAttributes.Private);

			stateMachine.f_taskAwaiter = typeBuilder.DefineField("<>u__$awaiter5", taskAwaiterOfReturnType,
			                                                     FieldAttributes.Private);
			foreach (ParameterInfo parameter in context.MethodInfo.GetParameters())
			{
				FieldBuilder field = typeBuilder.DefineField(parameter.Name, parameter.ParameterType, FieldAttributes.Public);
				stateMachine.Parameters.Add(parameter.Position, field);
			}
			stateMachine.f_closure = typeBuilder.DefineField("CS$<>8__locals3", context.Closure.TypeBuilder,
			                                                 FieldAttributes.Public);

			MethodBuilder setStateMachineMethod = context.StateMachine.m_SetStateMachine =
			                                      typeBuilder.DefineMethod("SetStateMachine",
			                                                               MethodAttributes.Private | MethodAttributes.HideBySig |
			                                                               MethodAttributes.NewSlot | MethodAttributes.Virtual |
			                                                               MethodAttributes.Final, null,
			                                                               new[] {typeof (IAsyncStateMachine)});
			typeBuilder.DefineMethodOverride(setStateMachineMethod,
			                                 typeof (IAsyncStateMachine).GetMethod("SetStateMachine",
			                                                                       new[] {typeof (IAsyncStateMachine)}));
			setStateMachineMethod.DefineParameter(1, ParameterAttributes.None, "param0");
			setStateMachineMethod.SetCustomAttribute(context.CommonlyReferencedTypes.GetDebuggerHiddenAttribute());

			MethodBuilder moveNextMethod = context.StateMachine.m_MoveNext =
			                               typeBuilder.DefineMethod("MoveNext",
			                                                        MethodAttributes.Private | MethodAttributes.HideBySig |
			                                                        MethodAttributes.NewSlot | MethodAttributes.Virtual |
			                                                        MethodAttributes.Final, null, Type.EmptyTypes);
			typeBuilder.DefineMethodOverride(moveNextMethod, typeof (IAsyncStateMachine).GetMethod("MoveNext", Type.EmptyTypes));
		}

		private void DeclareClosureClass(FacadeMethodDeclarationContext context)
		{
			ClosureDeclaration closure = context.Closure;
			TypeBuilder typeBuilder = closure.TypeBuilder =
			                          context.FacadeDeclaration.TypeBuilder.DefineNestedType(
				                          string.Format("<>c__DisplayClass{0}", context.FacadeDeclaration.GetNextClosureId()),
				                          TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed |
				                          TypeAttributes.BeforeFieldInit |
				                          TypeAttributes.NestedPrivate, typeof (object));
			typeBuilder.SetCustomAttribute(context.CommonlyReferencedTypes.GetCompilerGeneratedAttribute());

			closure.m_ctor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.HideBySig |
			                                                      MethodAttributes.SpecialName |
			                                                      MethodAttributes.RTSpecialName);

			closure.f_thisOfContainer = typeBuilder.DefineField("<>4__this", context.FacadeDeclaration.TypeBuilder,
			                                                    FieldAttributes.Public);

			foreach (ParameterInfo parameter in context.MethodInfo.GetParameters())
			{
				FieldBuilder field = typeBuilder.DefineField(parameter.Name, parameter.ParameterType, FieldAttributes.Public);
				closure.Parameters.Add(parameter.Position, field);
			}

			closure.m_AsyncResult = typeBuilder.DefineMethod(string.Format("<{0}>b__{1}", context.Name, 0),
			                                                 MethodAttributes.Public | MethodAttributes.HideBySig,
			                                                 typeof (IAsyncResult),
			                                                 new[] {typeof (AsyncCallback), typeof (object)});
			closure.m_AsyncResult.DefineParameter(1, ParameterAttributes.None, "callback");
			closure.m_AsyncResult.DefineParameter(2, ParameterAttributes.None, "state");
		}

		private void DeclareFacade_TargetMethod(FacadeMethodDeclarationContext context)
		{
			int parameterPosition = 0;
			Dictionary<int, KeyValuePair<Type, string>> parameterTypes = context.MethodInfo
			                                                                    .GetParameters()
			                                                                    .ToDictionary(parameter => ++parameterPosition,
			                                                                                  parameter =>
			                                                                                  new KeyValuePair<Type, string>(
				                                                                                  parameter.ParameterType,
				                                                                                  parameter.Name));

			MethodBuilder methodBuilder = context.m_TargetMethod =
			                              context.FacadeDeclaration.TypeBuilder.DefineMethod(context.Name,
			                                                                                 MethodAttributes.Public |
			                                                                                 MethodAttributes.Virtual |
			                                                                                 MethodAttributes.HideBySig |
			                                                                                 MethodAttributes.NewSlot |
			                                                                                 MethodAttributes.Final,
			                                                                                 context.CommonlyReferencedTypes
			                                                                                        .TaskType,
			                                                                                 parameterTypes.Select(
				                                                                                 k => k.Value.Key).ToArray());
			foreach (var entry in parameterTypes)
			{
				methodBuilder.DefineParameter(entry.Key, ParameterAttributes.None, entry.Value.Value);
			}
			methodBuilder.SetCustomAttribute(context.CommonlyReferencedTypes.GetDebuggerStepThroughAttribute());
			methodBuilder.SetCustomAttribute(
				context.CommonlyReferencedTypes.GetAsyncStateMachineAttribute(context.StateMachine.TypeBuilder));
		}

		private static void LoadArgumentHelper(ParameterInfo parameter, ILGenerator il)
		{
			switch (parameter.Position + 1)
			{
				case 0:
					il.Emit(OpCodes.Ldarg_0);
					break;
				case 1:
					il.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldarg_3);
					break;
				default:
					il.Emit(OpCodes.Ldarg_S, parameter.Position + 1);
					break;
			}
		}

// ReSharper disable InconsistentNaming
		private void ImplementFacade_Constructor(FacadeDeclaration facadeDecl)
		{
			ConstructorInfo objectConstructor = typeof (object).GetConstructor(Type.EmptyTypes);
			Debug.Assert(objectConstructor != null);

			ILGenerator il = facadeDecl.m_ctor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0); //L_0000: ldarg.0 
			il.Emit(OpCodes.Call, objectConstructor); //L_0001: call instance void [mscorlib]System.Object::.ctor()
			il.Emit(OpCodes.Nop); //L_0006: nop 
			il.Emit(OpCodes.Nop); //L_0007: nop 
			il.Emit(OpCodes.Ldarg_0); //L_0008: ldarg.0 
			il.Emit(OpCodes.Ldarg_1); //L_0009: ldarg.1 
			il.Emit(OpCodes.Stfld, facadeDecl.f_RealClient);
				//L_000a: stfld class ExampleServiceReference.ServiceReference1.IService1 AsyncWCFTest.ServiceReference1.DoWork1::_realClient
			il.Emit(OpCodes.Nop); //L_000f: nop 
			il.Emit(OpCodes.Ret); //L_0010: ret 
		}

		private void ImplementFacade_AsyncEndMethod(FacadeMethodDeclarationContext context)
		{
			MethodInfo channelEndMethod =
				context.FacadeDeclaration.APMInterface.GetMethod(string.Format("End{0}", context.CorrectedName),
				                                                 new[] {typeof (IAsyncResult)});

			ILGenerator il = context.m_EndAsync.GetILGenerator();
			Label l_Exit = il.DefineLabel();
			LocalBuilder data = null;
			if (context.CommonlyReferencedTypes.ReturnType != typeof (void))
			{
				data = il.DeclareLocal(context.CommonlyReferencedTypes.ReturnType);
			}
			il.Emit(OpCodes.Ldarg_0); //Load IAsyncResult parameter
			il.Emit(OpCodes.Ldfld, context.FacadeDeclaration.f_RealClient);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, channelEndMethod);
			if (data != null)
			{
				il.Emit(OpCodes.Stloc_S, data);
				il.Emit(OpCodes.Br_S, l_Exit);
				il.MarkLabel(l_Exit);
				il.Emit(OpCodes.Ldloc_S, data);
			}
			else
			{
				il.Emit(OpCodes.Nop);
			}
			il.Emit(OpCodes.Ret);
		}

		private static void ImplementFacade_TargetMethod(FacadeMethodDeclarationContext context)
		{
			MethodInfo getTask = null;
			MethodInfo create = context.CommonlyReferencedTypes.AsyncTaskMethodBuilderOfReturnType.GetMethod("Create",
			                                                                                                 Type.EmptyTypes);
			MethodInfo start =
				context.CommonlyReferencedTypes.AsyncTaskMethodBuilderOfReturnType.GetMethod("Start")
				       .MakeGenericMethod(context.StateMachine.TypeBuilder);

			ILGenerator il = context.m_TargetMethod.GetILGenerator();

			LocalBuilder task = null;
/*loc_0*/
			LocalBuilder d__ = il.DeclareLocal(context.StateMachine.TypeBuilder);

			if (context.CommonlyReferencedTypes.TaskType != typeof (void))
			{
				getTask = context.CommonlyReferencedTypes.AsyncTaskMethodBuilderOfReturnType.GetProperty("Task").GetGetMethod();
/*loc_1*/
				task = il.DeclareLocal(context.CommonlyReferencedTypes.TaskOfReturnType);
			}
/*loc_2*/
			LocalBuilder builder = il.DeclareLocal(context.CommonlyReferencedTypes.AsyncTaskMethodBuilderOfReturnType);

			Label l_Exit = il.DefineLabel();

			il.Emit(OpCodes.Ldloca_S, d__); //L_0000: ldloca.s d__
			il.Emit(OpCodes.Ldarg_0); //L_0002: ldarg.0 
			il.Emit(OpCodes.Stfld, context.StateMachine.f_thisOfContainer);
				//L_0003: stfld class AsyncWCFTest.ServiceReference1.DoWork1 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>4__this
			il.Emit(OpCodes.Ldloca_S, d__); //L_0008: ldloca.s d__
			foreach (ParameterInfo parameter in context.MethodInfo.GetParameters())
			{
				LoadArgumentHelper(parameter, il); //L_000a: ldarg.1 
				il.Emit(OpCodes.Stfld, context.StateMachine.Parameters[parameter.Position]);
					//L_000b: stfld string AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::arg1
				il.Emit(OpCodes.Ldloca_S, d__); //L_0010: ldloca.s d__
			}

			il.Emit(OpCodes.Call, create);
				//L_0012: call valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::Create()
			il.Emit(OpCodes.Stfld, context.StateMachine.f_builder);
				//L_0017: stfld valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
			il.Emit(OpCodes.Ldloca_S, d__); //L_001c: ldloca.s d__
			il.Emit(OpCodes.Ldc_I4_M1); //L_001e: ldc.i4.m1 
			il.Emit(OpCodes.Stfld, context.StateMachine.f_state);
				//L_001f: stfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.Emit(OpCodes.Ldloca_S, d__); //L_0024: ldloca.s d__
			il.Emit(OpCodes.Ldfld, context.StateMachine.f_builder);
				//L_0026: ldfld valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
			il.Emit(OpCodes.Stloc_S, builder); //L_002b: stloc.2 
			il.Emit(OpCodes.Ldloca_S, builder); //L_002c: ldloca.s builder
			il.Emit(OpCodes.Ldloca_S, d__); //L_002e: ldloca.s d__
			il.Emit(OpCodes.Call, start);
				//L_0030: call instance void [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::Start<valuetype AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4>(!!0&)
			if (task != null)
			{
				il.Emit(OpCodes.Ldloca_S, d__); //L_0035: ldloca.s d__
				il.Emit(OpCodes.Ldflda, context.StateMachine.f_builder);
				//L_0037: ldflda valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
				il.Emit(OpCodes.Call, getTask);
				//L_003c: call instance class [mscorlib]System.Threading.Tasks.Task`1<!0> [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::get_Task()
				il.Emit(OpCodes.Stloc_S, task); //L_0041: stloc.1 
				il.Emit(OpCodes.Br_S, l_Exit); //L_0042: br.s L_0044
				il.MarkLabel(l_Exit);
				il.Emit(OpCodes.Ldloc_S, task); //L_0044: ldloc.1 
			}
			else
			{
				il.Emit(OpCodes.Br_S, l_Exit); //L_0042: br.s L_0044
				il.MarkLabel(l_Exit);
			}
			il.Emit(OpCodes.Ret); //L_0045: ret 
		}

		private void ImplementClosure_AsyncResultMethod(FacadeMethodDeclarationContext context)
		{
			Type[] parameters =
				context.MethodInfo.GetParameters()
				       .Select(p => p.ParameterType)
				       .Concat(new[] {typeof (AsyncCallback), typeof (object)})
				       .ToArray();
			MethodInfo channelBeginMethod =
				context.FacadeDeclaration.APMInterface.GetMethod(string.Format("Begin{0}", context.CorrectedName), parameters);

			ILGenerator il = context.Closure.m_AsyncResult.GetILGenerator();
			Label label = il.DefineLabel();

			il.DeclareLocal(typeof (IAsyncResult));

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, context.Closure.f_thisOfContainer);
			il.Emit(OpCodes.Ldfld, context.FacadeDeclaration.f_RealClient);
			foreach (FieldBuilder field in context.Closure.Parameters.Values)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, field);
			}
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, channelBeginMethod);
			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Br_S, label);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ret);
		}

		private void ImplementStateMachine_SetStateMachineMethod(FacadeMethodDeclarationContext context)
		{
			StateMachineDeclaration stateMachine = context.StateMachine;
			CommonTypes types = context.CommonlyReferencedTypes;

			MethodInfo setStateMachineMethod2 = types.AsyncTaskMethodBuilderOfReturnType.GetMethod("SetStateMachine",
			                                                                                       new[]
				                                                                                       {typeof (IAsyncStateMachine)});

			ILGenerator il = stateMachine.m_SetStateMachine.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldflda, stateMachine.f_builder);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, setStateMachineMethod2);
			il.Emit(OpCodes.Ret);
		}

		private void ImplementStateMachine_MoveNextMethod(FacadeMethodDeclarationContext context)
		{
			CommonTypes types = context.CommonlyReferencedTypes;
			StateMachineDeclaration stateMachine = context.StateMachine;

			MethodInfo getFactoryMethod = types.TaskOfReturnType.GetProperty("Factory").GetGetMethod();
			Type asyncCallbackAndStateAndResult =
				typeof (Func<,,>).MakeGenericType(new[] {typeof (AsyncCallback), typeof (object), typeof (IAsyncResult)});
			ConstructorInfo asyncCallbackAndStateAndResultConstructor =
				asyncCallbackAndStateAndResult.GetConstructor(new[] {typeof (object), typeof (IntPtr)});
			MethodInfo getResult = types.TaskAwaiterOfReturnType.GetMethod("GetResult", Type.EmptyTypes);
			MethodInfo setResult = types.AsyncTaskMethodBuilderOfReturnType.GetMethod("SetResult");
			MethodInfo setException = types.AsyncTaskMethodBuilderOfReturnType.GetMethod("SetException",
			                                                                             new[] {typeof (Exception)});
			MethodInfo awaitUnsafeOnCompleted = types.AsyncTaskMethodBuilderOfReturnType.GetMethod("AwaitUnsafeOnCompleted")
			                                         .MakeGenericMethod(new[]
				                                         {types.TaskAwaiterOfReturnType, stateMachine.TypeBuilder});
			Type asyncResultAndReturn = types.AsyncResultAndReturn;
			ConstructorInfo asyncResultAndStateConstructor =
				asyncResultAndReturn.GetConstructor(new[] {typeof (object), typeof (IntPtr)});
			MethodInfo getAwaiter;
			if (context.CommonlyReferencedTypes.IsGenericTaskType)
			{
				getAwaiter =
					typeof (AwaitExtensions).GetMethods()
					                        .Single(p => p.Name == "GetAwaiter" && p.IsGenericMethodDefinition)
					                        .MakeGenericMethod(new[] {context.CommonlyReferencedTypes.ReturnType});
			}
			else
			{
				getAwaiter = typeof (Task).GetMethod("GetAwaiter") ??
				             typeof (AwaitExtensions).GetMethod("GetAwaiter", new[] {typeof (Task)});
			}

			MethodInfo getIsCompleted = types.TaskAwaiterOfReturnType.GetProperty("IsCompleted").GetGetMethod();
			MethodInfo taskFactoryMethod = types.TaskFactoryOfReturnType
			                                    .GetMethods()
			                                    .Where(t => t.Name == "FromAsync")
			                                    .Select(x => new {M = x, P = x.GetParameters()})
			                                    .Where(x => x.P.Length == 3 &&
			                                                x.P[0].ParameterType == asyncCallbackAndStateAndResult &&
			                                                x.P[1].ParameterType == asyncResultAndReturn &&
			                                                x.P[2].ParameterType == typeof (object))
			                                    .Select(x => x.M)
			                                    .Single();


			Debug.Assert(asyncResultAndStateConstructor != null, "asyncResultAndStateConstructor != null");
			Debug.Assert(asyncCallbackAndStateAndResultConstructor != null, "funcConstructor != null");

			ILGenerator il = context.StateMachine.m_MoveNext.GetILGenerator();
			Label l_StateIsMinus3 = il.DefineLabel();
			Label l_StateIs0 = il.DefineLabel();
			Label l_StateDefault = il.DefineLabel();
			Label l_StateDefaultCode = il.DefineLabel();
			Label l_SkipToLabelExit = il.DefineLabel();
			Label l_leaveSwitch = il.DefineLabel();
			Label l_OnAwaiterCompleted = il.DefineLabel();
			Label l_Return = il.DefineLabel();

			LocalBuilder data = null;
/*loc_0*/
			LocalBuilder flag = il.DeclareLocal(typeof (bool));
			if (context.CommonlyReferencedTypes.ReturnType != typeof (void))
			{
/*loc_1*/
				data = il.DeclareLocal(context.CommonlyReferencedTypes.ReturnType);
			}
/*loc_2*/
			LocalBuilder exception = il.DeclareLocal(typeof (Exception));
/*loc_3*/
			LocalBuilder num = il.DeclareLocal(typeof (Int32));
/*loc_4*/
			LocalBuilder awaiter = il.DeclareLocal(types.TaskAwaiterOfReturnType);
/*loc_5*/
			LocalBuilder awaiter2 = il.DeclareLocal(types.TaskAwaiterOfReturnType);

			Label l_Exit = il.BeginExceptionBlock();
			il.Emit(OpCodes.Ldc_I4_1); //L_0000: ldc.i4.1 
			il.Emit(OpCodes.Stloc_S, flag); //L_0001: stloc.0 
			il.Emit(OpCodes.Ldarg_0); //L_0002: ldarg.0 
			il.Emit(OpCodes.Ldfld, stateMachine.f_state);
				//L_0003: ldfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.Emit(OpCodes.Stloc_S, num); //L_0008: stloc.3 
			il.Emit(OpCodes.Ldloc_S, num); //L_0009: ldloc.3 
			il.Emit(OpCodes.Ldc_I4, -3); //L_000a: ldc.i4.s -3
			il.Emit(OpCodes.Beq, l_StateIsMinus3); //L_000c: beq.s L_0014
			il.Emit(OpCodes.Ldloc_S, num); //L_000e: ldloc.3 
			il.Emit(OpCodes.Ldc_I4_0); //L_000f: ldc.i4.0 
			il.Emit(OpCodes.Beq, l_StateIs0); //L_0010: beq.s L_0019
			il.Emit(OpCodes.Br, l_StateDefault); //L_0012: br.s L_001e
			il.MarkLabel(l_StateIsMinus3);
			il.Emit(OpCodes.Br, l_SkipToLabelExit); //L_0014: br L_00df
			il.MarkLabel(l_StateIs0);
			il.Emit(OpCodes.Br, l_leaveSwitch); //L_0019: br L_00ae
			il.MarkLabel(l_StateDefault);
			il.Emit(OpCodes.Br, l_StateDefaultCode); //L_001e: br.s L_0020
			il.MarkLabel(l_StateDefaultCode);
			il.Emit(OpCodes.Nop); //L_0020: nop 
			il.Emit(OpCodes.Ldarg_0); //L_0021: ldarg.0 
			il.Emit(OpCodes.Newobj, context.Closure.m_ctor);
				//L_0022: newobj instance void AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2::.ctor()
			il.Emit(OpCodes.Stfld, stateMachine.f_closure);
				//L_0027: stfld class AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::CS$<>8__locals3
			foreach (var parameterKvp in stateMachine.Parameters)
			{
				il.Emit(OpCodes.Ldarg_0); //L_002c: ldarg.0 
				il.Emit(OpCodes.Ldfld, stateMachine.f_closure);
					//L_002d: ldfld class AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::CS$<>8__locals3
				il.Emit(OpCodes.Ldarg_0); //L_0032: ldarg.0 
				il.Emit(OpCodes.Ldfld, parameterKvp.Value);
					//L_0033: ldfld string AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::arg1
				il.Emit(OpCodes.Stfld, context.Closure.Parameters[parameterKvp.Key]);
					//L_0038: stfld string AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2::arg1
			}
			il.Emit(OpCodes.Ldarg_0); //L_003d: ldarg.0 
			il.Emit(OpCodes.Ldfld, stateMachine.f_closure);
				//L_003e: ldfld class AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::CS$<>8__locals3
			il.Emit(OpCodes.Ldarg_0); //L_0043: ldarg.0 
			il.Emit(OpCodes.Ldfld, context.StateMachine.f_thisOfContainer);
				//L_0044: ldfld class AsyncWCFTest.ServiceReference1.DoWork1 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>4__this
			il.Emit(OpCodes.Stfld, context.Closure.f_thisOfContainer);
				//L_0049: stfld class AsyncWCFTest.ServiceReference1.DoWork1 AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2::<>4__this
			il.Emit(OpCodes.Nop); //L_004e: nop 
			il.Emit(OpCodes.Call, getFactoryMethod);
				//L_004f: call class [mscorlib]System.Threading.Tasks.TaskFactory`1<!0> [mscorlib]System.Threading.Tasks.Task`1<class ExampleServiceReference.ServiceReference1.SampleData>::get_Factory()
			il.Emit(OpCodes.Ldarg_0); //L_0054: ldarg.0 
			il.Emit(OpCodes.Ldfld, stateMachine.f_closure);
				//L_0055: ldfld class AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::CS$<>8__locals3
			il.Emit(OpCodes.Ldftn, context.Closure.m_AsyncResult);
				//L_005a: ldftn instance class [mscorlib]System.IAsyncResult AsyncWCFTest.ServiceReference1.DoWork1/<>c__DisplayClass2::<DoWork>b__0(class [mscorlib]System.AsyncCallback, object)
			il.Emit(OpCodes.Newobj, asyncCallbackAndStateAndResultConstructor);
				//L_0060: newobj instance void [mscorlib]System.Func`3<class [mscorlib]System.AsyncCallback, object, class [mscorlib]System.IAsyncResult>::.ctor(object, native int)
			il.Emit(OpCodes.Ldarg_0); //L_0065: ldarg.0 
			il.Emit(OpCodes.Ldfld, context.StateMachine.f_thisOfContainer);
				//L_0066: ldfld class AsyncWCFTest.ServiceReference1.DoWork1 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>4__this
			il.Emit(OpCodes.Ldftn, context.m_EndAsync);
				//L_006b: ldftn instance class ExampleServiceReference.ServiceReference1.SampleData AsyncWCFTest.ServiceReference1.DoWork1::<DoWork>b__1(class [mscorlib]System.IAsyncResult)
			il.Emit(OpCodes.Newobj, asyncResultAndStateConstructor);
				//L_0071: newobj instance void [mscorlib]System.Func`2<class [mscorlib]System.IAsyncResult, class ExampleServiceReference.ServiceReference1.SampleData>::.ctor(object, native int)
			il.Emit(OpCodes.Ldnull); //L_0076: ldnull 
			il.Emit(OpCodes.Callvirt, taskFactoryMethod);
				//L_0077: callvirt instance class [mscorlib]System.Threading.Tasks.Task`1<!0> [mscorlib]System.Threading.Tasks.TaskFactory`1<class ExampleServiceReference.ServiceReference1.SampleData>::FromAsync(class [mscorlib]System.Func`3<class [mscorlib]System.AsyncCallback, object, class [mscorlib]System.IAsyncResult>, class [mscorlib]System.Func`2<class [mscorlib]System.IAsyncResult, !0>, object)
			il.Emit(OpCodes.Call, getAwaiter);
				//L_007c: call valuetype [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<!!0> [Microsoft.Threading.Tasks]AwaitExtensions::GetAwaiter<class ExampleServiceReference.ServiceReference1.SampleData>(class [mscorlib]System.Threading.Tasks.Task`1<!!0>)
			il.Emit(OpCodes.Stloc_S, awaiter); //L_0081: stloc.s awaiter
			il.Emit(OpCodes.Ldloca_S, awaiter); //L_0083: ldloca.s awaiter
			il.Emit(OpCodes.Call, getIsCompleted);
				//L_0085: call instance bool [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData>::get_IsCompleted()
			il.Emit(OpCodes.Brtrue, l_OnAwaiterCompleted); //L_008a: brtrue.s L_00cd
			il.Emit(OpCodes.Ldarg_0); //L_008c: ldarg.0 
			il.Emit(OpCodes.Ldc_I4_0); //L_008d: ldc.i4.0 
			il.Emit(OpCodes.Stfld, stateMachine.f_state);
				//L_008e: stfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.Emit(OpCodes.Ldarg_0); //L_0093: ldarg.0 
			il.Emit(OpCodes.Ldloc_S, awaiter); //L_0094: ldloc.s awaiter
			il.Emit(OpCodes.Stfld, stateMachine.f_taskAwaiter);
				//L_0096: stfld valuetype [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>u__$awaiter5
			il.Emit(OpCodes.Ldarg_0); //L_009b: ldarg.0 
			il.Emit(OpCodes.Ldflda, stateMachine.f_builder);
				//L_009c: ldflda valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
			il.Emit(OpCodes.Ldloca_S, awaiter); //L_00a1: ldloca.s awaiter
			il.Emit(OpCodes.Ldarg_0); //L_00a3: ldarg.0 
			il.Emit(OpCodes.Call, awaitUnsafeOnCompleted);
				//L_00a4: call instance void [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::AwaitUnsafeOnCompleted<valuetype [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData>, valuetype AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4>(!!0&, !!1&)
			il.Emit(OpCodes.Nop); //L_00a9: nop 
			il.Emit(OpCodes.Ldc_I4_0); //L_00aa: ldc.i4.0 
			il.Emit(OpCodes.Stloc_S, flag); //L_00ab: stloc.0 
			il.Emit(OpCodes.Leave, l_Return); //L_00ac: leave.s L_010f
			il.MarkLabel(l_leaveSwitch);
			il.Emit(OpCodes.Ldarg_0); //L_00ae: ldarg.0 
			il.Emit(OpCodes.Ldfld, stateMachine.f_taskAwaiter);
				//L_00af: ldfld valuetype [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>u__$awaiter5
			il.Emit(OpCodes.Stloc_S, awaiter); //L_00b4: stloc.s awaiter
			il.Emit(OpCodes.Ldarg_0); //L_00b6: ldarg.0 
			il.Emit(OpCodes.Ldloca_S, awaiter2); //L_00b7: ldloca.s awaiter2
			il.Emit(OpCodes.Initobj, types.TaskAwaiterOfReturnType);
				//L_00b9: initobj [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData>
			il.Emit(OpCodes.Ldloc_S, awaiter2); //L_00bf: ldloc.s awaiter2
			il.Emit(OpCodes.Stfld, stateMachine.f_taskAwaiter);
				//L_00c1: stfld valuetype [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>u__$awaiter5
			il.Emit(OpCodes.Ldarg_0); //L_00c6: ldarg.0 
			il.Emit(OpCodes.Ldc_I4_M1); //L_00c7: ldc.i4.m1 
			il.Emit(OpCodes.Stfld, stateMachine.f_state);
				//L_00c8: stfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.MarkLabel(l_OnAwaiterCompleted);
			il.Emit(OpCodes.Ldloca_S, awaiter); //L_00cd: ldloca.s awaiter
			il.Emit(OpCodes.Call, getResult);
				//L_00cf: call instance !0 [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData>::GetResult()
			il.Emit(OpCodes.Ldloca, awaiter); //L_00d4: ldloca.s awaiter
			il.Emit(OpCodes.Initobj, types.TaskAwaiterOfReturnType);
				//L_00d6: initobj [Microsoft.Threading.Tasks]Microsoft.Runtime.CompilerServices.TaskAwaiter`1<class ExampleServiceReference.ServiceReference1.SampleData>
			if (data != null)
			{
				il.Emit(OpCodes.Stloc_S, data); //L_00dc: stloc.1 
			}
			else
			{
				il.Emit(OpCodes.Nop);
			}
			il.Emit(OpCodes.Leave, l_Exit); //L_00dd: leave.s L_00f9
			il.MarkLabel(l_SkipToLabelExit);
			il.BeginCatchBlock(typeof (Exception)); //L_00df: leave.s L_00f9
			il.Emit(OpCodes.Stloc_S, exception); //L_00e1: stloc.2 
			il.Emit(OpCodes.Ldarg_0); //L_00e2: ldarg.0 
			il.Emit(OpCodes.Ldc_I4, -2); //L_00e3: ldc.i4.s -2
			il.Emit(OpCodes.Stfld, stateMachine.f_state);
				//L_00e5: stfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.Emit(OpCodes.Ldarg_0); //L_00ea: ldarg.0 
			il.Emit(OpCodes.Ldflda, stateMachine.f_builder);
				//L_00eb: ldflda valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
			il.Emit(OpCodes.Ldloc_S, exception); //L_00f0: ldloc.2 
			il.Emit(OpCodes.Call, setException);
				//L_00f1: call instance void [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::SetException(class [mscorlib]System.Exception)
			il.Emit(OpCodes.Nop); //L_00f6: nop 
			il.Emit(OpCodes.Leave, l_Return); //L_00f7: leave.s L_010f
			//il.MarkLabel(l_Exit);
			il.EndExceptionBlock();
			il.Emit(OpCodes.Nop); //L_00f9: nop 
			il.Emit(OpCodes.Ldarg_0); //L_00fa: ldarg.0 
			il.Emit(OpCodes.Ldc_I4, -2); //L_00fb: ldc.i4.s -2
			il.Emit(OpCodes.Stfld, stateMachine.f_state);
				//L_00fd: stfld int32 AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>1__state
			il.Emit(OpCodes.Ldarg_0); //L_0102: ldarg.0 
			il.Emit(OpCodes.Ldflda, stateMachine.f_builder);
				//L_0103: ldflda valuetype [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData> AsyncWCFTest.ServiceReference1.DoWork1/<DoWork>d__4::<>t__builder
			if (data != null)
			{
				il.Emit(OpCodes.Ldloc_S, data); //L_0108: ldloc.1 
			}
			il.Emit(OpCodes.Call, setResult);
				//L_0109: call instance void [System.Threading.Tasks]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<class ExampleServiceReference.ServiceReference1.SampleData>::SetResult(!0)
			il.Emit(OpCodes.Nop); //L_010e: nop 
			il.MarkLabel(l_Return);
			il.Emit(OpCodes.Nop); //L_010f: nop 
			il.Emit(OpCodes.Ret); //L_0110: ret 
		}

// ReSharper restore InconsistentNaming
	}
}