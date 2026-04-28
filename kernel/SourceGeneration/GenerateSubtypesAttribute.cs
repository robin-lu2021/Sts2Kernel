using System;
using System.Diagnostics.CodeAnalysis;

namespace MegaCrit.Sts2.SourceGeneration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class GenerateSubtypesAttribute : Attribute
{
	public DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; set; }
}
