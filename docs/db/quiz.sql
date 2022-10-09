-- クイズ

CREATE TABLE quiz(
	quiz_id VARCHAR(32) PRIMARY KEY,
	belong_to VARCHAR(32),
	INDEX belong_to(belong_to),
	FOREIGN KEY quiz_room(belong_to) REFERENCES room(room_id) ON DELETE CASCADE ON UPDATE CASCADE
);

