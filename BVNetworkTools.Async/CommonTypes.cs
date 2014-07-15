using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Runtime.CompilerServices;

namespace BinaryVibrance.NetworkTools.Async
{
	internal class CommonTypes
	{
		public CommonTypes(Type taskType)
		{
			TaskType = taskType;

			if (taskType == typeof (void) || taskType == typeof (Task))
			{
				ReturnType = typeof(void);
				AsyncTaskMethodBuilderOfReturnType = taskType == typeof(void)
					                                     ? typeof (AsyncVoidMethodBuilder)
					                                     : typeof (AsyncTaskMethodBuilder);
				TaskAwaiterOfReturnType = typeof (TaskAwaiter);
				TaskOfReturnType = typeof (Task);
				TaskFactoryOfReturnType = typeof (TaskFactory);
				AsyncResultAndReturn = typeof (Action<>).MakeGenericType(new[] {typeof (IAsyncResult)});
			}
			else if (IsGenericTaskType)
			{
				ReturnType = taskType.GetGenericArguments()[0];
				AsyncTaskMethodBuilderOfReturnType = typeof (AsyncTaskMethodBuilder<>).MakeGenericType(ReturnType);
				TaskAwaiterOfReturnType = typeof (TaskAwaiter<>).MakeGenericType(ReturnType);
				TaskOfReturnType = typeof (Task<>).MakeGenericType(ReturnType);
				TaskFactoryOfReturnType = typeof (TaskFactory<>).MakeGenericType(new[] {ReturnType});
				AsyncResultAndReturn = typeof (Func<,>).MakeGenericType(new[] {typeof (IAsyncResult), ReturnType});
			}
		}

		public Type ReturnType { get; private set; }
		public Type TaskType { get; private set; }

		public bool IsGenericTaskType
		{
			get { return TaskType.IsGenericType && TaskType.GetGenericTypeDefinition() == typeof (Task<>); }
		}

		public Type AsyncTaskMethodBuilderOfReturnType { get; private set; }
		public Type TaskAwaiterOfReturnType { get; private set; }
		public Type TaskOfReturnType { get; private set; }
		public Type TaskFactoryOfReturnType { get; private set; }
		public Type AsyncResultAndReturn { get; private set; }

		public bool IsValidAsyncMethod
		{
			get { var result = TaskType == typeof(void) || TaskType == typeof(Task) || TaskType.IsGenericType && TaskType.GetGenericTypeDefinition() == typeof(Task<>);
				return result;
			}

		}	

		public CustomAttributeBuilder GetCompilerGeneratedAttribute()
		{
			Type attribute = typeof (CompilerGeneratedAttribute);
			ConstructorInfo constructor = attribute.GetConstructor(Type.EmptyTypes);
			Debug.Assert(constructor != null);
			return new CustomAttributeBuilder(constructor, new object[0]);
		}

		public CustomAttributeBuilder GetDebuggerStepThroughAttribute()
		{
			Type attribute = typeof (DebuggerStepThroughAttribute);
			ConstructorInfo constructor = attribute.GetConstructor(Type.EmptyTypes);
			Debug.Assert(constructor != null);
			return new CustomAttributeBuilder(constructor, new object[0]);
		}

		public CustomAttributeBuilder GetAsyncStateMachineAttribute(Type stateMachine)
		{
			var asyncStateMachineAttribute = typeof(AsyncStateMachineAttribute).GetConstructor(new[] { typeof(Type) });
			Debug.Assert(asyncStateMachineAttribute != null);
			var x = new CustomAttributeBuilder(asyncStateMachineAttribute, new object[] { stateMachine });
			return x;
		}

		public CustomAttributeBuilder GetDebuggerHiddenAttribute()
		{
			Type attribute = typeof(DebuggerHiddenAttribute);
			ConstructorInfo constructor = attribute.GetConstructor(Type.EmptyTypes);
			Debug.Assert(constructor != null);
			return new CustomAttributeBuilder(constructor, new object[0]);
		}
	}
}