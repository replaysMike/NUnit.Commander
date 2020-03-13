using System.IO;

namespace NUnit.Commander.IO
{
	public static class ResourceLoader
	{
		/// <summary>
		/// Load an embedded resource
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Stream Load(string name)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			//var names = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
			return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}");
		}
	}
}
