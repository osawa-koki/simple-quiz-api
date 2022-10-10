using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using MailMod;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;


/// <summary>
/// "/auth/pre_signup"に送るJSONデータ
/// </summary>
public struct PreSignUpStruct
{
	/// <summary>
    /// 仮登録を行うメールアドレス
    /// </summary>
    public string mail;
};


/// <summary>
/// "/auth/signup"に送るJSONデータ
/// </summary>
public struct SignUpStruct
{
	/// <summary>
    /// 仮会員登録時に発行したトークン
    /// </summary>
    public string token;
	/// <summary>
    /// ユーザ名(50文字以内)
    /// </summary>
    public string user_name;
	/// <summary>
    /// パスワード(8-32)
    /// </summary>
    public string password;
};


internal static class Auth
{


    /// <summary>
    /// セッション管理用のトークンを生成します。
    /// </summary>
    /// <returns>
	/// {
	/// 	"token": "fba8c49f09f140d693ddf2a33491a82e"
	/// }
    /// </returns>
	/// <response code="200">正常にトークンの生成処理が実行されました。</response>
	/// <response code="500">トークン生成中に例外が発生しました。</response>
    [HttpGet]
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


    /// <summary>
    /// 指定したトークンがログイン情報を保有しているかを判定します。
    /// </summary>
    /// <returns>
	/// {
	/// 	"is_login": true
	/// }
	/// </returns>
	/// <response code="200">正常にログイン中かどうかを判定できました。</response>
	/// <response code="400">指定したトークンが不正です。</response>
	/// <response code="500">ログイン判定中に例外が発生しました。</response>
    [HttpGet]
	[Route("auth/is_login")]
	[Produces("application/json")]
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
				return Results.Ok(new {is_login = true});
			}
			else
			{
				return Results.Ok(new {is_login = false});
			}
		}
		catch
		{
			return Results.Problem();
		}
	}


    /// <summary>
    /// 指定したメールアドレスを対象に仮会員登録処理を行います。
    /// </summary>
	/// <remarks>
	/// Sample request:
	///
	/// 	POST /auth/signup
	/// 	{
	/// 		"mail": "test@example.com"
	/// 	}
	///
	/// </remarks>
	/// <param name="preSignUpStruct"></param>
	/// <response code="200">正常に仮会員登録処理が完了しました。</response>
	/// <response code="400">不正なメールアドレスが指定されました。</response>
	/// <response code="500">会員登録処理中に例外が発生しました。</response>
	[Route("auth/pre_signup")]
	[Produces("application/json")]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult PreSignUp(PreSignUpStruct preSignUpStruct)
	{
		string mail = preSignUpStruct.mail;
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


    /// <summary>
    /// 本登録処理を実行します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	///
	/// 	POST /auth/signup
	/// 	{
	/// 		"token": "fba8c49f09f140d693ddf2a33491a82e",
	/// 		"user_name": "hogehoge",
	/// 		"password": "foofoo",
	/// 		"comment": "hogefoo"
	/// 	}
	///
	/// </remarks>
	/// <param name="signUpStruct"></param>
	/// <response code="200">正常に仮会員登録処理が完了しました。</response>
	/// <response code="400">不正なメールアドレスが指定されました。</response>
	/// <response code="500">会員登録処理中に例外が発生しました。</response>
	[Route("auth/signup")]
	[Produces("application/json")]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult SignUp(SignUpStruct signUpStruct)
	{
		string token = signUpStruct.token;
		string user_name = signUpStruct.user_name;
		string password = signUpStruct.password;
		try
		{
			// ユーザ名のチェック
			if (user_name.Length < 3 || 50 < user_name.Length)
			{
				return Results.BadRequest(new { message = "ユーザ名の長さが不正です。"});
			}
			// パスワードのチェック
			if (password.Length < 8 || 32 < password.Length)
			{
				return Results.BadRequest(new { message = "パスワードの文字数が不正です。"});
			}

			DBClient client = new();

			// トークンのチェック

			client.Add("SELECT pre_user_id");
			client.Add("FROM pre_users");
			client.Add("WHERE token = @token");
			client.AddParam(token);
			client.SetDataType("@token", SqlDbType.VarChar);
			var result = client.Select();
			if (result == null) return Results.BadRequest(new { message = "指定したトークンは無効です。"});
			string pre_user_id = ((string)result["pre_user_id"]).ToString();


			// 本登録処理
			string? hashed_password = Util.HashPassword(pre_user_id + password);
			if (hashed_password == null) return Results.Problem();

			client.Add("INSERT INTO users(user_id, pw, user_name, comment)");
			client.Add("VALUES(@user_id, @pw, @user_name, @comment)");
			client.AddParam(pre_user_id);
			client.AddParam(hashed_password);
			client.AddParam(user_name);
			client.AddParam(password);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@pw", SqlDbType.VarChar);
			client.SetDataType("@user_name", SqlDbType.VarChar);
			client.SetDataType("@comment", SqlDbType.VarChar);
			client.Execute();
			
			return Results.Ok(new {});
		}
		catch
		{
			return Results.Problem();
		}
	}




    /// <summary>
    /// セッション管理用のトークンを無効化します。
    /// </summary>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にトークンの無効化処理が実行されました。</response>
	/// <response code="500">トークン無効化処理中に例外が発生しました。</response>
    [HttpDelete]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static dynamic SignOut(HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}
		try
		{
			DBClient client = new();
			client.Add("UPDATE sessions");
			client.Add("SET is_valid = 0");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			client.Execute();
			return Results.Ok(new {
				revoke = session_id,
			});
		}
		catch
		{
			return Results.Problem();
		}
	}



}

