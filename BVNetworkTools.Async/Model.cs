using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace BinaryVibrance.NetworkTools.Async
{
	internal class FacadeDeclaration
	{
		public TypeBuilder TypeBuilder { get; set; }
		public Type APMInterface { get; set; }

// ReSharper disable InconsistentNaming
		public FieldBuilder f_RealClient { get; set; }
		public ConstructorBuilder m_ctor { get; set; }
// ReSharper restore InconsistentNaming

		private int _closureId = 0;
		public int GetNextClosureId()
		{
			return Interlocked.Increment(ref _closureId);
		}
	}

	internal class FacadeMethodDeclarationContext
	{
		public FacadeDeclaration FacadeDeclaration { get; private set; }

		public FacadeMethodDeclarationContext(MethodInfo methodInfo, FacadeDeclaration facadeDeclaration)
		{
			FacadeDeclaration = facadeDeclaration;
			MethodInfo = methodInfo;
			CommonlyReferencedTypes = new CommonTypes(methodInfo.ReturnType);

			Closure = new ClosureDeclaration();
			StateMachine = new StateMachineDeclaration();
		}

		public string Name { get { return MethodInfo.Name; } }
		public string CorrectedName
		{
			get
			{
				var name = MethodInfo.Name;
				if (name.EndsWith("Async"))
				{
					name = name.Remove(name.Length - "Async".Length);
				}
				return name;
			}
		}
		public StateMachineDeclaration StateMachine { get; private set; }
		public ClosureDeclaration Closure { get; private set; }
		public MethodInfo MethodInfo { get; private set; }

// ReSharper disable InconsistentNaming
		public MethodBuilder m_EndAsync { get; set; }
		public MethodBuilder m_TargetMethod { get; set; }
// ReSharper restore InconsistentNaming

		public CommonTypes CommonlyReferencedTypes { get; private set; }

		
	}

	internal class StateMachineDeclaration
	{
		public StateMachineDeclaration()
		{
			Parameters = new Dictionary<int, FieldBuilder>();
		}
		
		public TypeBuilder TypeBuilder { get; set; }
		// ReSharper disable InconsistentNaming
		public MethodBuilder m_MoveNext { get; set; }
		public MethodBuilder m_SetStateMachine { get; set; }

		public FieldBuilder f_state { get; set; }
		public FieldBuilder f_thisOfContainer { get; set; }
		public FieldBuilder f_builder { get; set; }
		public FieldBuilder f_stack { get; set; }
		public FieldBuilder f_taskAwaiter { get; set; }

		public Dictionary<int, FieldBuilder> Parameters { get; private set; }

		public FieldBuilder f_closure { get; set; }
		// ReSharper restore InconsistentNaming
	}

	internal class ClosureDeclaration
	{
		public ClosureDeclaration()
		{
			Parameters = new Dictionary<int, FieldBuilder>();
		}

		public TypeBuilder TypeBuilder { get; set; }
		// ReSharper disable InconsistentNaming
		public ConstructorBuilder m_ctor { get; set; }
		public MethodBuilder m_AsyncResult { get; set; }
		public FieldBuilder f_thisOfContainer { get; set; }
		public Dictionary<int, FieldBuilder> Parameters { get; private set; }
		// ReSharper restore InconsistentNaming
	}

}
