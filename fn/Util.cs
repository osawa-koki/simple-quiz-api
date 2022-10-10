

internal static class Util
{
	internal static string MergeUri(string a, string b)
	{
		return new Uri(new Uri(a), b).AbsolutePath;
	}


}


