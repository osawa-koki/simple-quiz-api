using dotenv.net;

internal static class Env
{
	internal static string? CONNECTION_STRING;
	internal static string? SMTPSERVER;
	internal static string? SMTPSERVER_USER;
	internal static string? SMTPSERVER_PASSWORD;


	internal static void Init()
	{
		DotEnv.Load();
		var envVars = DotEnv.Read();

		string CONNECTION_STRING_WORD = "CONNECTION_STRING";
		if (!envVars.ContainsKey(CONNECTION_STRING_WORD))
		{
			Error($"{CONNECTION_STRING_WORD}が未設定です。");
		}
		CONNECTION_STRING = envVars[CONNECTION_STRING_WORD];


		string SMTPSERVER_WORD = "SMTPSERVER";
		if (!envVars.ContainsKey(SMTPSERVER_WORD))
		{
			Error($"{SMTPSERVER_WORD}が未設定です。");
		}
		SMTPSERVER = envVars[SMTPSERVER_WORD];


		string SMTPSERVER_USER_WORD = "SMTPSERVER_USER";
		if (!envVars.ContainsKey(SMTPSERVER_USER_WORD))
		{
			Error($"{SMTPSERVER_USER_WORD}が未設定です。");
		}
		SMTPSERVER_USER = envVars[SMTPSERVER_USER_WORD];


		string SMTPSERVER_PASSWORD_WORD = "SMTPSERVER_PASSWORD";
		if (!envVars.ContainsKey(SMTPSERVER_PASSWORD_WORD))
		{
			Error($"{SMTPSERVER_PASSWORD_WORD}が未設定です。");
		}
		SMTPSERVER_PASSWORD = envVars[SMTPSERVER_PASSWORD_WORD];

	}


	private static void Error(string message)
	{
		Console.WriteLine(" ***** 環境変数設定エラー ***** ");
		Console.WriteLine(message);
		Console.WriteLine("");

		Environment.Exit(-1);
	}
}

