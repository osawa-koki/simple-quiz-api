using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using MailMod;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;


/// <summary>
/// テンプレート構造体
/// </summary>
internal struct TemplateStruct
{
	internal int quiztemplate_id;
	internal bool is_public;
	internal string content;
	internal List<string> keywords;
}



internal static class Template
{


    /// <summary>
    /// テンプレート一覧取得
    /// </summary>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にテンプレート一覧を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="500">テンプレートの取得処理中に例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult List(int since, int per_page, HttpContext context)
	{
		if (since < 0 || per_page < 0)
		{
			return Results.BadRequest("正の値を入力してください。");
		}
		if (30 < per_page)
		{
			return Results.BadRequest("一度に取得できるテンプレート数は30までです。");
		}

		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add("SELECT t.quiztemplate_id, t.is_public, t.content, u.user_name, u.user_icon");
			client.Add("FROM quiz_templates t");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE is_public = 1 OR owning_user = @user_id OR owning_session = @session_id");
			client.Add("ORDER BY t.quiztemplate_id ASC");
			client.Add($"OFFSET {since} ROWS"); // SQLインジェクション攻撃対策は不要
			client.Add($"FETCH NEXT {per_page} ROWS ONLY;"); // SQLインジェクション攻撃対策は不要
			client.AddParam(user_id ?? ""); // ログインしていなければ、存在しないIDを指定する。 -> user_id未指定と同じ
			client.AddParam(auth_filled);
			var templates = client.SelectAll();
			return Results.Ok(templates);

		}
		catch
		{
			return Results.Problem();
		}
	}


    /// <summary>
    /// テンプレート検索
    /// </summary>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">検索結果を正しく取得できました。</response>
	/// <response code="400">不正なパラメタが指定されました。</response>
	/// <response code="500">テンプレートの検索処理中に例外が発生しました。</response>
    [HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Search(string search_by, int per_page, HttpContext context)
	{
		if (per_page < 0)
		{
			return Results.BadRequest("正の値を入力してください。");
		}
		if (30 < per_page)
		{
			return Results.BadRequest("一度に取得できるテンプレート数は30までです。");
		}

		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add($"SELECT TOP {per_page} k.quiztemplate_id, t.is_public, t.content, u.user_name, u.user_icon"); // SQLインジェクション攻撃対策は不要
			client.Add("FROM quiz_template_keywords k");
			client.Add("INNER JOIN quiz_templates t ON k.quiztemplate_id = t.quiztemplate_id");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE is_public = 1 OR owning_user = @user_id OR owning_session = @session_id");
			client.Add("ORDER BY t.quiztemplate_id ASC");
			client.AddParam(user_id ?? ""); // ログインしていなければ、存在しないIDを指定する。 -> user_id未指定と同じ
			client.AddParam(auth_filled);
			var templates = client.SelectAll();
			return Results.Ok(templates);

		}
		catch
		{
			return Results.Problem();
		}



	}




}


