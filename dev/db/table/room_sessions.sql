-- ルームとセッションの関連

CREATE TABLE room_sessions(
	room_id VARCHAR(32),
	session_id VARCHAR(32),
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	PRIMARY KEY(room_id, session_id),
	CONSTRAINT fgk_rs_rooms FOREIGN KEY(room_id) REFERENCES rooms(room_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT fgk_rs_sessions FOREIGN KEY(session_id) REFERENCES sessions(session_id) ON DELETE CASCADE ON UPDATE CASCADE
);

