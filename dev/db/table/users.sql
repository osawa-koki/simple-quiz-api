-- ユーザ情報

CREATE TABLE users(
	user_id VARCHAR(16) PRIMARY KEY,
	mail VARCHAR(254) UNIQUE,
	user_name NVARCHAR(25) NOT NULL,
	pw VARCHAR(512) NOT NULL,
	comment NVARCHAR(300) DEFAULT '',
	user_icon VARCHAR(32) NULL,
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);
CREATE NONCLUSTERED INDEX mail_idx ON users(mail ASC);
