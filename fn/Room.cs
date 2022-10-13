using DBMod;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

internal struct RoomCreateStruct
{
	internal string room_name;
	internal string? room_icon;
	internal string? explanation;
	internal int? pw;
	internal bool is_public;
}

internal struct RoomUpdateStruct
{
	internal string room_id;
	internal string room_name;
	internal string? room_icon;
	internal string? explanation;
	internal int? pw;
	internal bool is_public;
}


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



	/// <summary>
	/// ルーム新規作成
	/// </summary>
	/// <remarks>
	/// 	
	/// 	Sample request:
	/// 		POST /room
	/// 		{
	/// 			"room_name": "ITクイズ大会♪",
	/// 			"room_icon": "491a82efba8c49f09f140d693ddf2a33.png"
	/// 			"explanation": "ITに関する簡単なクイズ大会で～す♪"
	/// 			"pw": null,
	/// 			"is_public": true
	/// 		}
	/// 	
	/// </remarks>
	/// <returns>
	/// 	{
	///			"uri": "https://simple-quiz.org/room?room_id=fba8c49f09f140d693ddf2a33491a82e"
	/// 	}
	/// </returns>
	/// <response code="201">正常にルームが作成されました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="500">ルーム作成処理中に例外が発生しました。</response>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Create(RoomCreateStruct roomCreateStruct, HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		string room_name = roomCreateStruct.room_name;
		if (room_name.Length < 3 || 30 < room_name.Length) return Results.BadRequest(new {message = "ルーム名は3文字以上、30文字以内で入力してください。"});


		string? room_icon = roomCreateStruct.room_icon;
		if (room_icon != null)
		{
			if (Regex.IsMatch(room_icon, @"\.{2,}")) return Results.BadRequest(new {message = "ディレクトリトラバーサル攻撃のおそれのある文字列が指定されています。"});
			if (Regex.IsMatch(room_icon, @"[^a-zA-Z0-9\.]")) return Results.BadRequest(new {message = "不正な文字が画像ファイル名として使用されています。"});
			if (room_icon.Length < 32 || 38 < room_icon.Length) return Results.BadRequest(new {message = "画像ファイル名の長さが正しくありません。"});
		}

		string? explanation = roomCreateStruct.explanation;
		if (explanation != null)
		{
			if (100 < explanation.Length) return Results.BadRequest(new {message = "説明文は100文字以内で入力してください。"});
		}


		string? password = roomCreateStruct.pw?.ToString();
		if (password != null)
		{
			if (password.Length != 4) return Results.BadRequest(new {message = "パスワードは4文字で構成してください。"});
		}

		bool is_public = roomCreateStruct.is_public;


		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			string room_id = Guid.NewGuid().ToString("N");

			client.Add("INSERT INTO rooms(room_id, room_name, room_icon, explanation, pw, is_public)");
			client.Add("VALUES(@room_id, @room_name, @room_icon, @explanation, @pw, @is_public);");
			client.AddParam(room_id);
			client.AddParam(room_name);
			client.AddParam(room_icon != null ? room_icon : DBNull.Value);
			client.AddParam(explanation != null ? explanation : DBNull.Value);
			client.AddParam(password != null ? password : DBNull.Value);
			client.AddParam(is_public ? 1 : 0);
			client.SetDataType("@room_id", SqlDbType.VarChar);
			client.SetDataType("@room_name", SqlDbType.VarChar);
			client.SetDataType("@room_icon", SqlDbType.VarChar);
			client.SetDataType("@explanation", SqlDbType.VarChar);
			client.SetDataType("@pw", SqlDbType.VarChar);
			client.SetDataType("@is_public", SqlDbType.Bit);

			client.Execute();

			return Results.Created($"https://{Env.DOMAIN}/room?room_id={room_id}", null);

		}
		catch
		{
			return Results.Problem();
		}
	}


	/// <summary>
	/// ルーム更新
	/// </summary>
	/// <remarks>
	/// 	
	/// 	Sample request:
	/// 		POST /room
	/// 		{
	/// 			"room_id": "c49f09f140d693ddf2a33491a82efba8",
	/// 			"room_name": "ITクイズ大会♪",
	/// 			"room_icon": "491a82efba8c49f09f140d693ddf2a33.png"
	/// 			"explanation": "ITに関する簡単なクイズ大会で～す♪"
	/// 			"pw": null,
	/// 			"is_public": true
	/// 		}
	/// 	
	/// </remarks>
	/// <returns>
	/// 	{}
	/// </returns>
	/// <response code="200">正常にルームが更新されました。</response>
	/// <response code="400">不正なパラメタが送信されました。</response>
	/// <response code="404">指定したルームは存在しません。</response>
	/// <response code="500">ルーム作成処理中に例外が発生しました。</response>
	[HttpPut]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	internal static IResult Update(RoomUpdateStruct roomUpdateStruct, HttpContext context)
	{
		Microsoft.Extensions.Primitives.StringValues session_id;
		bool auth_filled = context.Request.Headers.TryGetValue("Authorization", out session_id);
		if (!auth_filled || session_id == "")
		{
			return Results.BadRequest(new { message = "認証トークンが不在です。"});
		}

		string room_name = roomUpdateStruct.room_name;
		if (room_name.Length < 3 || 30 < room_name.Length) return Results.BadRequest(new {message = "ルーム名は3文字以上、30文字以内で入力してください。"});


		string? room_icon = roomUpdateStruct.room_icon;
		if (room_icon != null)
		{
			if (Regex.IsMatch(room_icon, @"\.{2,}")) return Results.BadRequest(new {message = "ディレクトリトラバーサル攻撃のおそれのある文字列が指定されています。"});
			if (Regex.IsMatch(room_icon, @"[^a-zA-Z0-9\.]")) return Results.BadRequest(new {message = "不正な文字が画像ファイル名として使用されています。"});
			if (room_icon.Length < 32 || 38 < room_icon.Length) return Results.BadRequest(new {message = "画像ファイル名の長さが正しくありません。"});
		}

		string? explanation = roomUpdateStruct.explanation;
		if (explanation != null)
		{
			if (100 < explanation.Length) return Results.BadRequest(new {message = "説明文は100文字以内で入力してください。"});
		}


		string? password = roomUpdateStruct.pw?.ToString();
		if (password != null)
		{
			if (password.Length != 4) return Results.BadRequest(new {message = "パスワードは4文字で構成してください。"});
		}

		bool is_public = roomUpdateStruct.is_public;


		DBClient client = new();

		try
		{
			client.Add("SELECT user_id");
			client.Add("FROM sessions");
			client.Add("WHERE session_id = @session_id;");
			client.AddParam(session_id);
			client.SetDataType("@session_id", SqlDbType.VarChar);
			var user_id = client.Select()?["user_id"]?.ToString();

			client.Add("SELECT user_id, session_id");
			client.Add("FROM room_owners");
			client.Add("WHERE room_id = @room_id;");
			var owner = client.Select();

			if (owner == null) return Results.NotFound();
			if (owner["user_id"].ToString() != user_id && owner["session_id"].ToString() != session_id) return Results.Forbid();

			client.Add("UPDATE rooms");
			client.Add("SET");
			client.Add("	room_name = @room_name");
			client.Add("	room_icon = @room_icon");
			client.Add("	explanation = @explanation");
			client.Add("	pw = @pw");
			client.Add("	is_public = @is_public");
			client.Add("WHERE room_id = @room_id;");
			client.AddParam(room_name);
			client.AddParam(room_icon != null ? room_icon : DBNull.Value);
			client.AddParam(explanation != null ? explanation : DBNull.Value);
			client.AddParam(password != null ? password : DBNull.Value);
			client.AddParam(is_public ? 1 : 0);
			client.AddParam(roomUpdateStruct.room_id);
			client.SetDataType("@room_name", SqlDbType.VarChar);
			client.SetDataType("@room_icon", SqlDbType.VarChar);
			client.SetDataType("@explanation", SqlDbType.VarChar);
			client.SetDataType("@pw", SqlDbType.VarChar);
			client.SetDataType("@is_public", SqlDbType.Bit);
			client.SetDataType("@room_id", SqlDbType.VarChar);

			client.Execute();

			return Results.Ok(new {});

		}
		catch
		{
			return Results.Problem();
		}
	}



}


