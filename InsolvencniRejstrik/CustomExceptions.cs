using System;

namespace InsolvencniRejstrik
{
	public class UnknownPersonException : ApplicationException
	{
		public UnknownPersonException(string message) : base(message)
		{ }
	}

	public class UnknownRoleException : ApplicationException
	{
		public UnknownRoleException(string message) : base(message)
		{ }
	}
}
