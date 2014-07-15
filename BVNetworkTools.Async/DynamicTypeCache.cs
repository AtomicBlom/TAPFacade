using System;
using System.Collections.Generic;

namespace BinaryVibrance.TAPFacade
{
	internal static class DynamicTypeCache
	{
		public static readonly Dictionary<Type, Type> CreatedAPMInterfaces = new Dictionary<Type, Type>();
		public static readonly object ObjAPMLock = new object();

		public static readonly Dictionary<Type, Type> CreatedTAPFacades = new Dictionary<Type, Type>();
		public static readonly object ObjFacadeLock = new object();
	}
}
