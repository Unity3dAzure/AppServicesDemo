using System;
using System.Collections.Generic;

namespace Unity3dAzure.AppServices
{
	[CLSCompliant(false)]
	[Serializable]
	public class NestedResults<T> : INestedResults
	{
		public uint count { get; set; }
		public List<T> results { get; set; }
	}
}