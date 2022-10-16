using DBMod;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;


public record TemplateSummaryStruct(
	string quiztemplate_id,
	string content,
	bool is_public,
	int n_of_used,
	int n_of_liked,
	int n_of_disliked,
	DateTime rgdt,
	DateTime updt
);


public record TemplateDetailStruct(
	string quiztemplate_id,
	string content,
	bool is_public,
	int n_of_used,
	int n_of_liked,
	int n_of_disliked,
	DateTime rgdt,
	DateTime updt,
	List<string> keywords
);


public record TemplateContentStruct(
	string content,
	bool is_public,
	List<string> keywords
);



internal static class Template
{

	/// <summary>
    /// テンプレート詳細取得
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 		GET /template/a27d5062e36847e9a036504bcdd8d8e5
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
	internal static IResult Detail(string template_id, [FromHeader(Name = "Authorization")] string session_id = "")
	{
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
			client.Add("SELECT t.owning_user, t.owning_session, t.is_public, t.content, t.n_of_used, t.n_of_liked, t.n_of_disliked, t.rgdt, t.updt, u.user_name, u.user_icon");
			client.Add("FROM quiz_templates t");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			var template = client.Select();

			if (template == null) return Results.NotFound(new {message = "指定したテンプレートIDは存在しません。"});

			if (template["owning_user"]?.ToString() != user_id && template["owning_session"]?.ToString() != session_id) return Results.StatusCode(403);

			client.Add("SELECT keyword");
			client.Add("FROM quiz_template_keywords");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			client.AddParam(template_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.Int);
			List<string> keywords = new();
			foreach (var keyword in client.SelectAll())
			{
				keywords.Add((string)keyword["keyword"]);
			}

			TemplateDetailStruct templateDetailStruct = new(
				template_id,
				(string)template["content"],
				(bool)template["is_public"],
				(int)template["n_of_used"],
				(int)template["n_of_liked"],
				(int)template["n_of_disliked"],
				(DateTime)template["rgdt"],
				(DateTime)template["updt"],
				keywords
			);

			return Results.Ok(templateDetailStruct);

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
	/// Sample request:
	/// 	
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
	internal static IResult List(int since, int per_page, [FromHeader(Name = "Authorization")] string session_id = "")
	{
		if (since < 0 || per_page < 0) return Results.BadRequest("正の値を入力してください。");
		if (30 < per_page) return Results.BadRequest("一度に取得できるテンプレート数は30までです。");

		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add("SELECT t.quiztemplate_id, t.is_public, t.content, t.n_of_used, , t.n_of_liked, t.n_of_disliked, u.user_name, u.user_icon");
			client.Add("FROM quiz_templates t");
			client.Add("INNER JOIN users u ON t.owning_user = u.user_id");
			client.Add("WHERE is_public = 1 OR owning_user = @user_id OR owning_session = @session_id");
			client.Add("ORDER BY (t.n_of_liked - t.n_of_disliked) * 3 + t.n_of_used DESC");
			client.Add($"OFFSET {since} ROWS"); // SQLインジェクション攻撃対策は不要
			client.Add($"FETCH NEXT {per_page} ROWS ONLY;"); // SQLインジェクション攻撃対策は不要
			client.AddParam(user_id ?? ""); // ログインしていなければ、存在しないIDを指定する。 -> user_id未指定と同じ
			client.AddParam(session_id);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var templates = client.SelectAll();

			List<TemplateSummaryStruct> templateSummaryStructs = new();
			foreach (var template in templates)
			{
				TemplateSummaryStruct templateSummaryStruct = new(
					(string)template["template_id"],
					(string)template["content"],
					(bool)template["is_public"],
					(int)template["n_of_used"],
					(int)template["n_of_liked"],
					(int)template["n_of_disliked"],
					(DateTime)template["rgdt"],
					(DateTime)template["updt"]
				);
				templateSummaryStructs.Add(templateSummaryStruct);
			}

			return Results.Ok(templateSummaryStructs);

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
	/// 		"content": "世界で${number}番目に高い山は???",
	/// 		"is_public": true,
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
	internal static IResult Create(TemplateContentStruct templateContentStruct, [FromHeader(Name = "Authorization")] string session_id = "")
	{
		if (10 < templateContentStruct.keywords.Count)
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

			string quiztemplate_id = Guid.NewGuid().ToString("N");

			// テンプレートの登録
			client.Add("INSERT INTO quiz_templates(quiztemplate_id, owning_user, owning_session, content, is_public)");
			client.Add("VALUES(@quiztemplate_id, @owning_user, @owning_session, @content, @is_public)");
			client.Add(quiztemplate_id);
			client.AddParam(user_id != null ? user_id : DBNull.Value);
			client.AddParam(session_id);
			client.AddParam(templateContentStruct.content);
			client.AddParam(templateContentStruct.is_public);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);
			client.SetDataType("@owning_user", SqlDbType.VarChar);
			client.SetDataType("@owning_session", SqlDbType.VarChar);
			client.SetDataType("@content", SqlDbType.VarChar);
			client.SetDataType("@is_public", SqlDbType.Bit);
			client.Execute();

			// テンプレートキーワードの登録
			client.Add("INSERT INTO quiz_template_keywords(quiztemplate_id, keyword)");
			client.Add("VALUES");
			List<string> keywords = new();
			foreach (var keyword in templateContentStruct.keywords)
			{
				keywords.Add($"('{quiztemplate_id.Replace("'", "''")}', '{keyword.Replace("'", "''")}')");
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
	/// 	PUT /template/
	/// 	{
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
	internal static IResult Update(string template_id, TemplateContentStruct templateContentStruct, [FromHeader(Name = "Authorization")] string session_id = "")
	{
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
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var result = client.Select();

			if (result == null) return Results.NotFound(new {message = "指定したトークンで示されるクイズテンプレートは存在しません。"});

			var owning_user = result["owning_user"]?.ToString();
			var owning_session = result["owning_session"]?.ToString();

			if (user_id != owning_user && session_id != owning_session) return Results.StatusCode(403);

			// テンプレートの更新処理
			client.Add("UPDATE quiz_templates");
			client.Add("SET");
			client.Add("	content = @content,");
			client.Add("	is_public = @is_public,");
			client.Add("	updt = GET_TOKYO_DATETIME()");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(templateContentStruct.content);
			client.AddParam(templateContentStruct.is_public);
			client.AddParam(template_id);
			client.SetDataType("@content", SqlDbType.VarChar);
			client.SetDataType("@is_public", SqlDbType.Bit);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);

			// 既に存在するキーワード一覧を削除
			client.Add("DELETE FROM quiz_template_keywords");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id;");
			client.AddParam(template_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);
			client.Execute();


			// テンプレートキーワードの登録
			client.Add("INSERT INTO quiz_template_keywords(quiztemplate_id, keyword)");
			client.Add("VALUES");
			List<string> keywords = new();
			foreach (var keyword in templateContentStruct.keywords)
			{
				keywords.Add($"('{template_id.Replace("'", "''")}', '{keyword.Replace("'", "''")}')");
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
	internal static IResult Delete(string template_id, [FromHeader(Name = "Authorization")] string session_id = "")
	{
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

			if (result == null) return Results.NotFound(new {message = "指定したトークンで示されるクイズテンプレートは存在しません。"});

			var owning_user = result["owning_user"]?.ToString();
			var owning_session = result["owning_session"]?.ToString();

			if (user_id != owning_user && session_id != owning_session) Results.StatusCode(403);

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


