using DBMod;
using System.Data;


internal static class Auth
{

	internal static dynamic GenerateToken()
	{
		string session_id = Guid.NewGuid().ToString("N");

		DBClient client = new();
		client.Add("INSERT INTO sessions(session_id)");
		client.Add("VALUES(@session_id);");
		client.AddParam(session_id);
		client.SetDataType("@session_id", SqlDbType.VarChar);
		client.Execute();

		return new {
			successed = true,
			error = "",
			session_id = session_id,
		};
	}
}

