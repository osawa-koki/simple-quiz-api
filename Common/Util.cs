using System.Security.Cryptography;
using System.Text;

internal static class Util
{
	internal static string MergeUri(string a, string b)
	{
		return new Uri(new Uri(a), b).AbsolutePath;
	}


	private static SHA256 sha256 = SHA256.Create();
	internal static string Hasher_sha256(string target)
	{
		var hashed = sha256.ComputeHash(Encoding.UTF8.GetBytes(target));
		return BitConverter.ToString(hashed).Replace("-", "");
	}


}


