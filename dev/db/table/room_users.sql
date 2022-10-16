-- ルームとユーザの関連

CREATE TABLE room_users(
	room_id VARCHAR(32),
	user_id VARCHAR(16),
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	PRIMARY KEY(room_id, user_id),
	CONSTRAINT fgk_ru_rooms FOREIGN KEY(room_id) REFERENCES rooms(room_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT fgk_rm_users FOREIGN KEY(user_id) REFERENCES users(user_id) ON DELETE CASCADE ON UPDATE CASCADE
);

