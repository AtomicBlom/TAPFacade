using System;
using System.Reflection;
using System.Reflection.Emit;

namespace BinaryVibrance.NetworkTools.Async.AttributeDuplicator
{

	/// <summary>
	/// A Marker interface for the generic IAttributeDuplicator. Do not implement this.
	/// </summary>
	public interface IAttributeDuplicator {}

	/// <summary>
	/// Duplicates an attribute, attempting to match the Constructor, Properties and Fields of the original Attribute.
	/// 
	/// Used in lieu of GetCustomAttributesData() missing in Silverlight.
	/// </summary>
	/// <typeparam name="TAttribute">The type of the attribute being duplicated</typeparam>
    public interface IAttributeDuplicator<in TAttribute> : IAttributeDuplicator where TAttribute : Attribute
    {
		/// <summary>
		/// Creates a CustomAttributeBuilder that represents a copy of the attribute provided.
		/// </summary>
		/// <param name="attachedMember">The Class, Method or Property that the attribute is being copied from</param>
		/// <param name="attribute">The attribute being copied</param>
		/// <returns>The CustomAttributeBuilder that represents the copy</returns>
		CustomAttributeBuilder GetCustomAttributeBuilder(MemberInfo attachedMember, TAttribute attribute);
    }
}