

CREATE PROCEDURE execute_register
@token VARCHAR(32)
AS
BEGIN

DECLARE @is_exist BIT = 0;

-- トークンが有効か判定
SELECT @is_exist = 1
WHERE EXISTS(
	SELECT token
	FROM pre_users
	WHERE DATEADD(HOUR, -1, dbo.GET_TOKYO_DATETIME()) < updt AND token = @token
);

IF @is_exist = 0
BEGIN
    THROW -1, '指定したトークンは無効です。';
END

-- pre_usersテーブルからusersテーブルへ移行
INSERT INTO users(user_id, mail, user_name, pw, comment, user_icon)
SELECT user_id, mail, user_name, pw, comment, user_icon
WHERE token = @token;

-- pre_usersテーブルのデータは削除
DELETE FROM users
WHERE token = @token;

END

