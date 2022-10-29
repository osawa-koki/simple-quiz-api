

CREATE TABLE room_members(
	room_id VARCHAR(32),
	user_id VARCHAR(16),
	session_id VARCHAR(32),
	privilege CHAR(1) NOT NULL CHECK(LEN(privilege) = 1), -- 「A: 管理者, P: プレイヤー」
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);
CREATE CLUSTERED INDEX room_member_idx ON room_members (room_id);

