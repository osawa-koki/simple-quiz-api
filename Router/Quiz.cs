using DBMod;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

#pragma warning disable


#pragma warning restore

internal static class Quiz
{

	/// <summary>
    /// クイズ一覧を取得します。
    /// </summary>
	/// <remarks>
	/// Sample request:
	/// 	
	/// 		GET /quiz/e5a27d5062e36847e9a036504bcdd8d8
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にテンプレート詳細を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="404">指定したテンプレートは存在しません。</response>
	/// <response code="500">テンプレート詳細取得処理中に例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult List(string quiztemplate_id, [FromHeader(Name = "Authorization")] string session_id = "")
	{
		if (session_id == "") return Results.BadRequest(new {message = "セッションIDを指定してください。"});
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
			client.AddParam(quiztemplate_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);
			var template = client.Select();

			if (template == null) return Results.NotFound(new {message = "指定したテンプレートIDは存在しません。"});

			if (template["owning_user"]?.ToString() != user_id && template["owning_session"]?.ToString() != session_id) return Results.StatusCode(403);

			client.Add("SELECT keyword");
			client.Add("FROM quiz_template_keywords");
			client.Add("WHERE quiztemplate_id = @quiztemplate_id");
			client.AddParam(quiztemplate_id);
			client.SetDataType("@quiztemplate_id", SqlDbType.VarChar);
			List<string> keywords = new();
			foreach (var keyword in client.SelectAll())
			{
				keywords.Add((string)keyword["keyword"]);
			}

			TemplateDetailStruct templateDetailStruct = new(
				quiztemplate_id,
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



}


