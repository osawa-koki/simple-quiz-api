using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using MailMod;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;


public struct SignUpStruct
{
    public string mail;
	public string password;
};


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
		try
		{
			Microsoft.Extensions.Primitives.StringValues session_id;
			bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
			if (!auth_filled || session_id == "")
			{
				return Results.BadRequest(new { message = "認証トークンが不在です。"});
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
				return Results.BadRequest(new { message = "指定した認証トークンに不備があります。"});
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
		catch
		{
			return Results.Problem();
		}
	}

	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	internal static IResult SignUp(SignUpStruct signUpStruct)
	{
		string mail = signUpStruct.mail;
		string password = signUpStruct.password;
		try
		{
			if (!Regex.IsMatch(mail, @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
			{
				return Results.BadRequest(new { message = "メールアドレスの形式が不正です。"});
			}
			if (254 < mail.Length)
			{
				return Results.BadRequest(new { message = "メールアドレスの形式が不正です。"});
			}

			DBClient client = new();
			client.Add("SELECT user_id");
			client.Add("FROM users");
			client.Add("WHERE user_id = @user_id");
			client.AddParam(mail);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			var result = client.Select();
			if (result != null) return Results.BadRequest(new { message = "既に登録済みのメールアドレスです。"});
			MailSetting mailSetting = new()
			{
				MailTo = mail,
				MailFrom = Env.SMTPSERVER_USER ?? "",
				Subject = "【simple-quiz】仮会員登録",
				Body = "テスト",
			};
			if (MailClient.Send(mailSetting))
			{
				return Results.Ok();
			}
			else
			{
				return Results.Problem();
			}
		}
		catch
		{
			return Results.Problem();
		}
	}
}

