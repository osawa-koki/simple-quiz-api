using DBMod;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;


/// <summary>
/// テンプレート構造体
/// </summary>
internal struct TemplateStruct
{
	internal int? quiztemplate_id;
	internal bool is_public;
	internal string content;
	internal string? transfer_to;
	internal List<string> keywords;
	internal DateTime? rgdt;
	internal DateTime? updt;
}



internal static class Template
{

	/// <summary>
    /// テンプレート詳細取得
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 		GET /template/120
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にテンプレート詳細を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="403">指定したテンプレートにアクセスする権限がありません。</response>
	/// <response code="404">指定したテンプレートは存在しません。</response>
	/// <response code="500">テンプレート詳細取得処理中に例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Detail(int template_id, HttpContext context)
	{
		if (template_id < 0)
		{
			return Results.BadRequest(new {message = "テンプレートIDには正の整数を指定してください。"});
		}

		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			// 詳細取得処理
			client.Add("SELECT t.owning_user, t.owning_session, t.is_public, t.content, t.rgdt, t.updt, u.user_name, u.user_icon");
			client.Add("FROM quiz_templates t");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE is_public = 1 OR owning_user = @user_id OR owning_session = @session_id");
			client.Add("	AND quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			var template = client.Select();

			if (template == null)
			{
				return Results.NotFound(new {message = "指定したテンプレートIDは存在しません。"});
			}

			if (template["owning_user"].ToString() != user_id && template["owning_session"] != session_id)
			{
				return Results.Forbid();
			}

			TemplateStruct answer = new();
			answer.quiztemplate_id = template_id;
			answer.is_public = int.Parse(template["is_public"]?.ToString() ?? "-1") == 1;
			answer.content = template["content"].ToString() ?? "*****";
			answer.keywords = new();
			answer.rgdt = DateTime.Parse(template["rgdt"].ToString() ?? "*****");
			answer.updt = DateTime.Parse(template["updt"].ToString() ?? "*****");

			client.Add("SELECT keyword");
			client.Add("FROM quiz_template_keywords");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			client.AddParam(template_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.Int);
			var keywords = client.SelectAll();

			foreach (var keyword in keywords)
			{
				answer.keywords.Add(keyword["keyword"]?.ToString() ?? "*****");
			}

			return Results.Ok(answer);

		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


    /// <summary>
    /// テンプレート一覧取得
    /// </summary>
	/// <remarks>
	/// 	
	/// 	Sample request:
	/// 		GET /template/list?since=10&amp;per_page=30
	/// 	
	/// </remarks>
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

		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
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
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	GET /template/search?search_by=ランキング&amp;per_page=30
	/// 	
	/// </remarks>
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

		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add($"SELECT TOP {per_page} k.quiztemplate_id, t.is_public, t.content, u.user_name, u.user_icon"); // SQLインジェクション攻撃対策は不要
			client.Add("FROM quiz_template_keywords k");
			client.Add("INNER JOIN quiz_templates t ON k.quiztemplate_id = t.quiztemplate_id");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE is_public = 1 OR owning_user = @user_id OR owning_session = @session_id");
			client.Add("ORDER BY t.quiztemplate_id ASC;");
			client.AddParam(user_id ?? ""); // ログインしていなければ、存在しないIDを指定する。 -> user_id未指定と同じ
			client.AddParam(auth_filled);
			var templates = client.SelectAll();
			return Results.Ok(templates);

		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}


    /// <summary>
    /// テンプレート作成
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	POST /template
	/// 	{
	/// 		"is_public": true,
	/// 		"content": "世界で${number}番目に高い山は???",
	/// 		"keywords": ["ランキング", "山", "教養", "地理"]
	/// 	}
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">テンプレートの作成に成功しました。</response>
	/// <response code="400">不正なパラメタが指定されました。</response>
	/// <response code="500">テンプレートの作成処理中に例外が発生しました。</response>
    [HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Create(TemplateStruct templateStruct, HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		if (10 < templateStruct.keywords.Count)
		{
			return Results.BadRequest(new { message = "登録できるキーワードは10個までです。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			// テンプレートの登録
			client.Add("INSERT INTO quiz_templates(owning_user, owning_session, is_public, content)");
			client.Add("VALUES(@owning_user, @owning_session, @is_public, @content)");
			client.AddParam(user_id != null ? user_id : DBNull.Value);
			client.AddParam(session_id);
			client.AddParam(templateStruct.is_public ? 1 : 0);
			client.AddParam(templateStruct.content);
			client.SetDataType("@", SqlDbType.VarChar);
			client.SetDataType("@", SqlDbType.VarChar);
			client.SetDataType("@", SqlDbType.Bit);
			client.SetDataType("@", SqlDbType.VarChar);
			client.Execute();

			// 登録したIDを取得
			client.Add("SELECT TOP 1 quiztemplate_id");
			client.Add("FROM quiz_templates");
			client.Add("WHERE owning_session = @owning_session");
			client.Add("ORDER BY quiztemplate_id DESC;");
			client.AddParam(session_id);
			client.SetDataType("@owning_session", SqlDbType.VarChar);
			var created_id = int.Parse(client.Select()?["quiztemplate_id"]?.ToString() ?? "-1");

			if (created_id == -1)
			{
				return Results.Problem();
			}


			// テンプレートキーワードの登録
			client.Add("INSERT INTO quiz_template_keywords(quiztemplate_id, keyword)");
			client.Add("VALUES");
			List<string> keywords = new();
			foreach (var keyword in templateStruct.keywords)
			{
				keywords.Add($"({created_id}, {keyword})");
			}
			client.Add(string.Join(",", keywords) + ";");
			client.Execute();

			return Results.Ok(new {});
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}





    /// <summary>
    /// テンプレート変更
    /// </summary>
	/// Sample request:
	/// 	
	/// 	PUT /template
	/// 	{
	/// 		"quiztemplate_id": 100,
	/// 		"is_public": true,
	/// 		"content": "世界で${number}番目に高い山は???",
	/// 		"transfer_to": "user_hoge",
	/// 		"keywords": ["ランキング", "山", "教養", "地理"]
	/// 	}
	/// 	
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">指定したテンプレートを正常に変更しました。</response>
	/// <response code="400">指定したパラメタが不正です。</response>
	/// <response code="403">指定したテンプレートにアクセスする権限がありません。</response>
	/// <response code="404">指定したテンプレートは存在しません。</response>
	/// <response code="500">テンプレートの変更処理中に例外が発生しました。</response>
    [HttpPut]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Update(int template_id, TemplateStruct templateStruct, HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add("SELECT owning_user, owning_session");
			client.Add("FROM quiz_templates");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			client.AddParam(template_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var result = client.Select();

			if (result == null)
			{
				return Results.NotFound(new {message = "指定したトークンで示されるクイズテンプレートは存在しません。"});
			}

			var owning_user = result["owning_user"].ToString();
			var owning_session = result["owning_session"].ToString();

			if (user_id != owning_user && session_id != owning_session)
			{
				return Results.Forbid();
			}

			// テンプレートの更新処理
			client.Add("UPDATE quiz_templates");
			client.Add("SET");
			client.Add("	owning_user = @owning_user");
			client.Add("	is_public = @is_public");
			client.Add("	content = @content");
			client.Add("	updt = CURRENT_TIMESTAMP;");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			// transfer_toプロパティがメールアドレスとして有効であれば所有者を変更。
			// 型推論が弱い、、、
			client.AddParam(Regex.IsMatch(templateStruct.transfer_to ?? "", @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$") ? templateStruct.transfer_to ?? "____" : (user_id != null ? user_id : DBNull.Value));
			client.AddParam(templateStruct.is_public ? 1 : 0);
			client.AddParam(templateStruct.content);
			client.AddParam(template_id);
			client.SetDataType("@owning_user", SqlDbType.VarChar);
			client.SetDataType("@is_public", SqlDbType.Bit);
			client.SetDataType("@content", SqlDbType.VarChar);
			client.SetDataType("@quiztemplate_id", SqlDbType.Int);

			// 既に存在するキーワード一覧を削除
			client.Add("DELETE FROM quiz_template_keywords");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.Int);
			client.Execute();


			// テンプレートキーワードの登録
			client.Add("INSERT INTO quiz_template_keywords(quiztemplate_id, keyword)");
			client.Add("VALUES");
			List<string> keywords = new();
			foreach (var keyword in templateStruct.keywords)
			{
				keywords.Add($"({template_id}, {keyword})");
			}
			client.Add(string.Join(",", keywords) + ";");
			client.Execute();

			return Results.Ok(new {});

		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}




    /// <summary>
    /// テンプレート削除
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 	DELETE /template/100
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">指定したテンプレートを正常に削除しました。</response>
	/// <response code="400">指定したパラメタが不正です。</response>
	/// <response code="403">指定したテンプレートにアクセスする権限がありません。</response>
	/// <response code="404">指定したテンプレートは存在しません。</response>
	/// <response code="500">テンプレートの削除処理中に例外が発生しました。</response>
    [HttpDelete]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Delete(int template_id, HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id_raw;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id_raw);
		string session_id = session_id_raw.ToString();
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add("SELECT owning_user, owning_session");
			client.Add("FROM quiz_templates");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			client.AddParam(template_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var result = client.Select();

			if (result == null)
			{
				return Results.NotFound(new {message = "指定したトークンで示されるクイズテンプレートは存在しません。"});
			}

			var owning_user = result["owning_user"].ToString();
			var owning_session = result["owning_session"].ToString();

			if (user_id != owning_user && session_id != owning_session)
			{
				return Results.Forbid();
			}

			client.Add("DELETE FROM quiz_templates");
			client.Add("FROM quiz_templates");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);
			client.Execute();

			return Results.Ok(new {});
		}
		catch (Exception ex)
		{
			return Results.Problem($"{ex}");
		}
	}



}


