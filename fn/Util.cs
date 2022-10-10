using System.Security.Cryptography;


internal static class Util
{
	internal static string MergeUri(string a, string b)
	{
		return new Uri(new Uri(a), b).AbsolutePath;
	}

	internal static string? HashPassword(string target)
	{
		return SHA512.Create(target)?.ToString();
	}


}


