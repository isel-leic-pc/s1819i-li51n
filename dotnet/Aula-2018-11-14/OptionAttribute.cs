using System;
using System.Reflection;

[assembly:CLSCompliant(true)]

namespace ReflectionUtils
{
	/// <summary>
	/// Summary description for Option.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class OptionAttribute : Attribute
	{
		public string Description;
		public string Nickname;
		
		 
	}
}
