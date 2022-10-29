

CREATE TABLE room_keywords(
	room_id VARCHAR(32),
	keyword NVARCHAR(30),
	CONSTRAINT fgk_room_kwd FOREIGN KEY(room_id) REFERENCES rooms(room_id) ON DELETE CASCADE ON UPDATE CASCADE,
);

