using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using MailMod;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;


public record SignUpStruct(string token, string user_id, string password, string user_name, string comment);
public record SignInStruct(string uid, string password);

internal static class Auth
{


    /// <summary>
    /// 【完成】セッション管理用のトークンを生成します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Get /auth/session_id
	/// 	
	/// </remarks>
    /// <returns>
	/// Sample response:
	/// 
	/// 	{
	/// 		"token": "fba8c49f09f140d693ddf2a33491a82e"
	/// 	}
	/// 
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
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}

	}


    /// <summary>
    /// 【完成】指定したトークンがログイン情報を保有しているかを判定します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Get /auth/sign_in
	/// 	
	/// </remarks>
    /// <returns>
	/// 	{
	/// 		"is_login": true
	/// 	}
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
	internal static IResult IsLogin([FromHeader(Name = "Authorization")] string session_id)
	{
		try
		{
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
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


    /// <summary>
    /// 【完成】指定したメールアドレスを対象に仮会員登録処理を行います。
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Get /auth/pre_signup
	/// 	
	/// </remarks>
	/// <remarks>
	/// Sample request:
	///
	/// 	POST /auth/signup
	/// 	{
	/// 		"mail": "test@example.com"
	/// 	}
	///
	/// </remarks>
	/// <param name="mail"></param>
	/// <response code="200">正常に仮会員登録処理が完了しました。</response>
	/// <response code="400">不正なメールアドレスが指定されました。</response>
	/// <response code="500">会員登録処理中に例外が発生しました。</response>
	[Route("auth/pre_signup")]
	[Produces("application/json")]
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult PreSignUp(string mail)
	{
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
			client.Add("WHERE mail = @mail");
			client.AddParam(mail);
			client.SetDataType("@mail", SqlDbType.VarChar);
			if (client.Select() != null) return Results.BadRequest(new { message = "既に登録済みのメールアドレスです。"});


			// 一定時間前に送信してたら
			client.Add("SELECT mail");
			client.Add("FROM pre_users");
			client.Add("WHERE mail = @mail");
			client.Add("	AND DATEADD(SECOND, -30, GETDATE()) < updt;");
			client.AddParam(mail);
			client.SetDataType("@mail", SqlDbType.VarChar);
			if (client.Select() != null) return Results.BadRequest(new { message = "30秒以上間隔を開けてください。"});

			// トークンをセット
			string token = Guid.NewGuid().ToString("N");
			client.Add($"EXEC set_mail_token @mail = '{mail.Replace("'", "''")}', @token = '{token.Replace("'", "''")}';"); // SQLインジェクション攻撃対策
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
				return Results.Problem($"メールの送信に失敗しました。");
			}
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


    /// <summary>
    /// 【完成】本登録処理を実行します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	///
	/// 	POST /auth/signup
	/// 	{
	/// 		"token": "fba8c49f09f140d693ddf2a33491a82e",
	/// 		"user_id": "hogehoge",
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
		string user_id = signUpStruct.user_id;
		string password = signUpStruct.password;
		string user_name = signUpStruct.user_name;
		string comment = signUpStruct.comment;
		try
		{
			// ユーザ名のチェック
			if (user_id.Length < 3 || 16 < user_id.Length)
			{
				return Results.BadRequest(new { message = "ユーザIDの長さが不正です。"});
			}
			// パスワードのチェック
			if (!Regex.IsMatch(password, @"^[a-zA-Z0-9-/:-@\[-\`\{-\~]+$"))
			{
				return Results.BadRequest(new { message = "パスワードは空白類似文字を除いた半角英数字のみで構成してください。"});
			}
			if (password.Length < 8 || 32 < password.Length)
			{
				return Results.BadRequest(new { message = "パスワードの文字数が不正です。"});
			}

			DBClient client = new();

			// トークンのチェック

			client.Add("SELECT mail");
			client.Add("FROM pre_users");
			client.Add("WHERE token = @token");
			client.Add("	AND DATEADD(MINUTE, -10, GETDATE()) < updt");
			client.AddParam(token);
			client.SetDataType("@token", SqlDbType.VarChar);
			var result = client.Select();
			if (result == null) return Results.BadRequest(new { message = "指定したトークンは無効です。"});
			string mail = (string)(result["mail"]);


			// 本登録処理
			string? hashed_password = Util.Hasher_sha256($"@{password}@");
			if (hashed_password == null) return Results.Problem("ハッシュ生成に失敗しました。");

			client.Add("INSERT INTO users(user_id, mail, pw, user_name, comment)");
			client.Add("VALUES(@user_id, @mail, @pw, @user_name, @comment)");
			client.AddParam(user_id);
			client.AddParam(mail);
			client.AddParam(hashed_password);
			client.AddParam(user_name);
			client.AddParam(comment);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@mail", SqlDbType.VarChar);
			client.SetDataType("@pw", SqlDbType.VarChar);
			client.SetDataType("@user_name", SqlDbType.VarChar);
			client.SetDataType("@comment", SqlDbType.VarChar);
			client.Execute();
			
			client.Add("DELETE FROM pre_users");
			client.Add("WHERE token = @token");
			client.AddParam(token);
			client.SetDataType("@token", SqlDbType.VarChar);
			client.Execute();

			return Results.Ok(new {});
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}




    /// <summary>
    /// セッション管理用のトークンを無効化します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Delete /auth/signout
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にトークンの無効化処理が実行されました。</response>
	/// <response code="500">トークン無効化処理中に例外が発生しました。</response>
    [HttpDelete]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static dynamic SignOut([FromHeader(Name = "Authorization")] string session_id)
	{
		try
		{
			DBClient client = new();
			client.Add("DELETE FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			client.Execute();
			return Results.Ok();
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


    /// <summary>
    /// サインイン処理
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Post /auth/signin
	/// 	{
	/// 		"uid": "メールアドレス or ユーザID",
	/// 		"pw": "p@ssw0rd"
	/// 	}
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にサインイン処理が実行されました。</response>
	/// <response code="400">認証に失敗しました。</response>
	/// <response code="500">予期せぬ例外が発生しました。</response>
    [HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static dynamic SignIn(SignInStruct signInStruct, [FromHeader(Name = "Authorization")] string session_id)
	{
		try
		{
			string uid = signInStruct.uid;
			string password = signInStruct.password;


			// メールアドレスのチェック
			if (!Regex.IsMatch(uid, @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$") && !(uid.Length < 3 || 16 < uid.Length))
			{
				return Results.BadRequest(new { message = "メールアドレス、またはユーザIDの形式が不正です。"});
			}

			if (!Regex.IsMatch(password, @"^[a-zA-Z0-9-/:-@\[-\`\{-\~]+$"))
			{
				return Results.BadRequest(new { message = "パスワードは空白類似文字を除いた半角英数字のみで構成してください。"});
			}
			if (password.Length < 8 || 32 < password.Length)
			{
				return Results.BadRequest(new { message = "パスワードは8文字以上、32文字以内で入力してください。"});
			}

			// 認証チェック
			var hashed_password = Util.Hasher_sha256($"@{password}@");
			if (hashed_password == null) return Results.Problem();

			DBClient client = new();
			client.Add("SELECT user_id");
			client.Add("FROM users");
			client.Add("WHERE (user_id = @uid OR mail = @uid) AND pw = @pw;");
			client.AddParam(uid);
			client.AddParam(uid);
			client.AddParam(hashed_password);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@pw", SqlDbType.VarChar);

			if (client.Select() != null)
			{
				return Results.Ok(new {});
			}
			return Results.BadRequest(new { message = "認証に失敗しました。"});
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}



    /// <summary>
    /// 【完成】ユーザID使用判定
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	Get /auth/caniuse/hogehoge
	/// 	
	/// </remarks>
    /// <returns>
	/// {
	/// 	"caniuse": true
	/// }
    /// </returns>
	/// <response code="200">正常に結果が取得できました。</response>
	/// <response code="400">指定したパラメタが不正です。</response>
	/// <response code="500">予期せぬ例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static dynamic CanIUse(string user_id)
	{
		try
		{
			if (!Regex.IsMatch(user_id, @"^[a-zA-Z0-9_\-]+$"))
			{
				return Results.BadRequest(new {message = "ユーザ名は半角英数字と「_(アンダースコア)」「-(ハイフン)」のみで構成して下さい。"});
			}
			
			if (user_id.Length < 3 || 16 < user_id.Length)
			{
				return Results.BadRequest(new {message = "ユーザIDは3文字以上、16文字以内で指定してください。"});
			}
			
			DBClient client = new();

			client.Add("SELECT user_id");
			client.Add("FROM users");
			client.Add("WHERE user_id = @user_id;");
			client.AddParam(user_id);
			client.SetDataType("@user_id", SqlDbType.VarChar);

			bool usable = client.Select() == null;

			return Results.Ok(new {caniuse = usable});

		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


}

