-- ルーム

CREATE TABLE rooms(
	room_id VARCHAR(32) PRIMARY KEY,
	room_name NVARCHAR(30) NOT NULL,
	room_icon VARCHAR(38) NULL,
	explanation NVARCHAR(100) NULL,
	pw CHAR(4) NULL CHECK(LEN(pw) = 4),
	is_public BIT NOT NULL,
	is_valid BIT DEFAULT 1,
	owning_user VARCHAR(16) NULL,
	owning_session VARCHAR(32) NULL,
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);


