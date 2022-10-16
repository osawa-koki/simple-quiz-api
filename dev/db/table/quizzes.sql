-- クイズ

CREATE TABLE quizzes(
	quiz_id VARCHAR(32) PRIMARY KEY,
	room_id VARCHAR(32),
	sequential INT NOT NULL,
	title NVARCHAR(30) NOT NULL,
	content NVARCHAR(100) NOT NULL,
	INDEX idx_belonging(room_id),
	CONSTRAINT fgk_belonging FOREIGN KEY(room_id) REFERENCES rooms(room_id) ON DELETE CASCADE ON UPDATE CASCADE
);

