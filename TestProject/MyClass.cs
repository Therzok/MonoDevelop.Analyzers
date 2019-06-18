using System;

namespace TestProject
{
	[AttributeUsage(AttributeTargets.Class)]
	class MyAttribute : Attribute
	{

	}

	// Any class implementing this should be exported to MEF
	abstract class BaseClass
	{
	}

	class MyClass : BaseClass
	{

	}

	class MyClassIsNotSealed
	{
		public virtual void DoStuff()
		{

		}

	}

	class IsBroken
	{
		public virtual void NotJava() { }
	}

	sealed class MyClassIsSealed
	{
		public void DoStuff() { }
	}
}
