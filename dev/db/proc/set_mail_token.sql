

CREATE PROCEDURE set_mail_token
@mail VARCHAR(254),
@token VARCHAR(32),
@user_id VARCHAR(32),
@user_name NVARCHAR(25),
@pw VARCHAR(512),
@comment NVARCHAR(300),
@user_icon VARCHAR(38),
AS
BEGIN

DECLARE @is_exist BIT = 0;

SELECT @is_exist = 1
WHERE EXISTS(
	SELECT mail
	FROM pre_users
	WHERE mail = @mail
);

INSERT INTO pre_users(token, mail, user_id, user_name, pw, comment, user_icon)
SELECT @token, @mail, @user_id, @user_name, @pw, @comment, @user_icon
WHERE @is_exist = 0;

UPDATE pre_users
SET
	token = @token,
	user_id = @user_id,
	user_name = @user_name,
	pw = @pw,
	comment = @comment,
	user_icon = @user_icon,
	updt = dbo.GET_TOKYO_DATETIME()
WHERE mail = @mail AND @is_exist = 1;


END

