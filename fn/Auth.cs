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
    /// <summary>
    /// 指定した ID に紐付く注文情報を取得します。
    /// </summary>
    /// <param name="orderId">取得する注文情報の ID。</param>
    /// <returns>取得した注文情報。</returns>
    [HttpGet("{orderId}")]
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
		try
		{
			// メールアドレスのチェック
			if (!Regex.IsMatch(mail, @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
			{
				return Results.BadRequest(new { message = "メールアドレスの形式が不正です。"});
			}
			if (254 < mail.Length)
			{
				return Results.BadRequest(new { message = "メールアドレスは254文字以内で入力してください。"});
			}


			DBClient client = new();


			// 既に登録済みかチェック
			client.Add("SELECT user_id");
			client.Add("FROM users");
			client.Add("WHERE user_id = @user_id");
			client.AddParam(mail);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			if (client.Select() != null) return Results.BadRequest(new { message = "既に登録済みのメールアドレスです。"});


			// 一定時間前に送信してたら
			client.Add("SELECT pre_user");
			client.Add("FROM pre_users");
			client.Add("WHERE pre_user = @pre_user");
			client.Add("	AND DATEADD(SECOND, -30, GETDATE()) < updt;");
			if (client.Select() != null) return Results.BadRequest(new { message = "30秒以上間隔を開けてください。"});

			// トークンをセット
			string token = Guid.NewGuid().ToString("N");
			client.Add($"EXEC set_mail_token @pre_user_id = '{mail.Replace("'", "''")}, @token = '{token.Replace("'", "''")}';"); // SQLインジェクション攻撃対策
			client.Execute();


			MailSetting mailSetting = new()
			{
				MailTo = mail,
				MailFrom = Env.SMTPSERVER_USER,
				Subject = "【simple-quiz】仮会員登録",
				Body = $"以下のリンクから会員登録を完成させてください。\r\nリンクの有効期限は10分です。\r\n\r\n{Env.DOMAIN}/register?token={token}",
			};
			if (MailClient.Send(mailSetting))
			{
				return Results.Ok(new {});
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

