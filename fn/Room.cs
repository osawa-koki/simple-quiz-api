using DBMod;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using MailMod;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;



internal static class Room
{

	/// <summary>
    /// ルーム一覧取得
    /// </summary>
	/// <remarks>
	/// 	
	/// 	Sample request:
	/// 		GET /room/list?since=10&amp;per_page=30
	/// 	
	/// </remarks>
    /// <returns>
	/// {}
    /// </returns>
	/// <response code="200">正常にテンプレート詳細を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="403">指定したテンプレートにアクセスする権限がありません。</response>
	/// <response code="500">テンプレート詳細取得処理中に例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult List(int since, int per_page, HttpContext context)
	{
		if (since < 0 || per_page < 0)
		{
			return Results.BadRequest("正の値を入力してください。");
		}
		if (30 < per_page)
		{
			return Results.BadRequest("一度に取得できるルーム数は30までです。");
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
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString() ?? "";


			client.Add("SELECT r.room_id, r.room_name, r.room_icon, r.explanation, r.rgdt, r.updt");
			client.Add("FROM rooms r");
			client.Add("LEFT JOIN room_owners ow ON r.room_id = ow.room_id");
			client.Add("WHERE r.is_public = 1 OR ow.user_id = @user_id OR ow.session_id = @session_id;");
			client.AddParam(user_id);
			client.AddParam(session_id);
			client.SetDataType("@user_id", SqlDbType.VarChar);
			client.SetDataType("@session_id", SqlDbType.VarChar);

			var rooms = client.SelectAll();

			return Results.Ok(rooms);

		}
		catch
		{
			return Results.Problem();
		}
	}


}


