using dotenv.net;

internal static class Env
{
	internal static string CONNECTION_STRING = "";
	internal static string SMTPSERVER = "";
	internal static int SMTP_PORT = 587;
	internal static string SMTPSERVER_USER = "";
	internal static string SMTPSERVER_PASSWORD = "";


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

		
		string SMTP_PORT_WORD = "SMTP_PORT";
		if (!envVars.ContainsKey(SMTPSERVER_WORD))
		{
			Error($"{SMTP_PORT_WORD}が未設定です。");
		}
		SMTP_PORT = int.Parse(envVars[SMTP_PORT_WORD]);


		string SMTP_USER_WORD = "SMTP_USER";
		if (!envVars.ContainsKey(SMTP_USER_WORD))
		{
			Error($"{SMTP_USER_WORD}が未設定です。");
		}
		SMTPSERVER_USER = envVars[SMTP_USER_WORD];


		string SMTP_PASSWORD_WORD = "SMTP_PASSWORD";
		if (!envVars.ContainsKey(SMTP_PASSWORD_WORD))
		{
			Error($"{SMTP_PASSWORD_WORD}が未設定です。");
		}
		SMTPSERVER_PASSWORD = envVars[SMTP_PASSWORD_WORD];

	}


	private static void Error(string message)
	{
		Console.WriteLine(" ***** 環境変数設定エラー ***** ");
		Console.WriteLine(message);
		Console.WriteLine("");

		Environment.Exit(-1);
	}
}

