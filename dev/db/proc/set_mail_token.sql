

CREATE PROCEDURE set_mail_token
@mail VARCHAR(254),
@token VARCHAR(32)
AS
BEGIN

DECLARE @is_exist BIT = 0;

SELECT @is_exist = 1
WHERE EXISTS(
	SELECT mail
	FROM pre_users
	WHERE mail = @mail
);

INSERT INTO pre_users(mail, token)
SELECT @mail, @token
WHERE @is_exist <> 0;

UPDATE pre_users
SET token = @token, updt = CURRENT_TIMESTAMP
WHERE mail = @mail AND @is_exist = 1;


END

