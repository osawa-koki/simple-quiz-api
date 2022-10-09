using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;


internal static class Auth
{
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static dynamic GenerateToken()
	{
		try {
			string session_id = Guid.NewGuid().ToString("N");

			DBClient client = new();
			client.Add("INSERT INTO sessions(session_id)");
			client.Add("VALUES(@session_id);");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			client.Execute();

			return Results.Ok(new {
				session_id = session_id,
			});
		}
		catch
		{
			return Results.Problem();
		}

	}

	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult IsLogin(HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { Message = "認証トークンが不在です。"});
		}

		DBClient client = new();
		client.Add("SELECT user_id");
		client.Add("FROM sessions");
		client.Add("WHERE session_id = @session_id;");
		client.AddParam(session_id);
		client.SetDataType("@session_id", SqlDbType.VarChar);
		
		var result = client.Select();

		if (result == null)
		{
			return Results.BadRequest(new { Message = "指定した認証トークンに不備があります。"});
		}

		if (result["user_id"] != DBNull.Value)
		{
			return Results.Ok(new {IsLogin = true});
		}
		else
		{
			return Results.Ok(new {IsLogin = false});
		}
	}

	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	internal static dynamic SignUp(string mail)
	{
		return new {

		};
	}
}

