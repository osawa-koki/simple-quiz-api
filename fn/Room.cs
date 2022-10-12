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
    /// ルーム詳細取得
    /// </summary>
	/// <remarks>
	/// 	
	/// 	Sample request:
	/// 		GET /room/fba8c49f09f140d693ddf2a33491a82e
	/// 	
	/// </remarks>
    /// <returns>
	/// 	{
	///			"room_id": "fba8c49f09f140d693ddf2a33491a82e",
	///			"room_name": "簡単クイズ♪",
	///			"room_icon": "3ddf2a33491a82efba8c49f09f140d69.png",
	///			"explanation": "ITに関する簡単なクイズで～す。",
	///			"rgdt": "2022-10-25 11:...",
	///			"updt": "2022-11-15 11:...",
	///			"user_name": "koko",
	///			"user_icon": "140d693ddf2a33491a82efba8c49f09f"
	/// 	}
    /// </returns>
	/// <response code="200">正常にテンプレート詳細を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="403">指定したルームにアクセスする権限がありません。</response>
	/// <response code="404">指定したルームは存在しません。</response>
	/// <response code="500">ルーム詳細取得処理中に例外が発生しました。</response>
    [HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Detail(string room_id, HttpContext context)
	{

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


			client.Add("SELECT r.room_id, r.room_name, r.room_icon, r.explanation, r.rgdt, r.updt, u.user_name, u.user_icon, ow.user_id, ow.session_id");
			client.Add("FROM rooms r");
			client.Add("LEFT JOIN room_owners ow ON r.room_id = ow.room_id");
			client.Add("LEFT JOIN users u ON ow.user_id = u.user_id");
			client.Add("WHERE room_id = @room_id;");
			client.AddParam(room_id);
			client.SetDataType("@room_id", SqlDbType.VarChar);

			var room = client.Select();

			if (room == null) return Results.NotFound(new {message = "指定したルームは存在しません。"});
			if (int.Parse(room["is_valid"].ToString() ?? "-1") != 1) return Results.BadRequest(new {message = "指定したルームは既に終了しています。"});
			if (room["user_id"].ToString() != user_id && room["session_id"].ToString() != session_id) return Results.Forbid();

			return Results.Ok(room);

		}
		catch
		{
			return Results.Problem();
		}
	}




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
	/// 	[
	/// 		{
	///				"room_id": "fba8c49f09f140d693ddf2a33491a82e",
	///				"room_name": "簡単クイズ♪",
	///				"room_icon": "3ddf2a33491a82efba8c49f09f140d69.png",
	///				"explanation": "ITに関する簡単なクイズで～す。",
	///				"rgdt": "2022-10-25 11:...",
	///				"updt": "2022-11-15 11:...",
	///				"user_name": "koko",
	///				"user_icon": "140d693ddf2a33491a82efba8c49f09f"
	/// 		}
	/// 	]
	/// </returns>
	/// <response code="200">正常にテンプレート詳細を取得できました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="500">ルーム一覧取得処理中に例外が発生しました。</response>
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


			client.Add("SELECT r.room_id, r.room_name, r.room_icon, r.explanation, r.rgdt, r.updt, u.user_name, u.user_icon");
			client.Add("FROM rooms r");
			client.Add("LEFT JOIN room_owners ow ON r.room_id = ow.room_id");
			client.Add("LEFT JOIN users u ON ow.user_id = u.user_id");
			client.Add("WHERE is_valid = 1 AND (r.is_public = 1 OR ow.user_id = @user_id OR ow.session_id = @session_id);");
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


