-- ルームとセッションの関連

CREATE TABLE room_session(
	room_id VARCHAR(32),
	session_id VARCHAR(32),
	rgdt DATETIME DEFAULT CURRENT_TIMESTAMP,
	updt DATETIME DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY(room_id, user_id)
);

