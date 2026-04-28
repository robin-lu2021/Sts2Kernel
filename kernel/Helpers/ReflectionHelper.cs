using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MegaCrit.Sts2.Core.Helpers;

public static class ReflectionHelper
{
	public const BindingFlags allAccessLevels = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static Type[]? _allTypes;

	public static bool SubtypesAvailable => true;

	public static Type[] AllTypes
	{
		get
		{
			if (_allTypes == null)
			{
				_allTypes = Assembly.GetAssembly(typeof(ReflectionHelper)).GetTypes();
			}
			return _allTypes;
		}
	}

	public static Type[] ModTypes => Array.Empty<Type>();

	public static IEnumerable<Type> GetSubtypes(Type parentType)
	{
		return from type in AllTypes.Concat(ModTypes)
			where (object)type != null && !type.IsAbstract && !type.IsInterface && InheritsOrImplements(type, parentType)
			select type;
	}

	public static IEnumerable<Type> GetSubtypesInMods(Type parentType)
	{
		return ModTypes.Where((Type type) => (object)type != null && !type.IsAbstract && !type.IsInterface && InheritsOrImplements(type, parentType));
	}

	public static IEnumerable<Type> GetSubtypes<T>() where T : class
	{
		return GetSubtypes(typeof(T));
	}

	public static IEnumerable<Type> GetSubtypesInMods<T>() where T : class
	{
		return GetSubtypesInMods(typeof(T));
	}

	public static bool InheritsOrImplements([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type derived, Type baseType)
	{
		if (!derived.IsSubclassOf(baseType))
		{
			return derived.GetInterfaces().Contains(baseType);
		}
		return true;
	}
}
