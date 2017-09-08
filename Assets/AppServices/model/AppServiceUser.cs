using System;

namespace Unity3dAzure.AppServices
{
	[Serializable]
	public class AppServiceUser
	{
		public string authenticationToken;
		public User user;
	}
}