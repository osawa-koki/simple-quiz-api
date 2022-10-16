

CREATE TABLE room_proceed_to(
	room_from VARCHAR(32) NOT NULL,
	room_to VARCHAR(32) NOT NULL,
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);
CREATE NONCLUSTERED INDEX room_relation_idx ON room_proceed_to (room_from);

