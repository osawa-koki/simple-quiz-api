
CREATE TABLE room_owners(
	room_id VARCHAR(32),
	user_id VARCHAR(254),
	PRIMARY KEY(room_id, user_id),
	CONSTRAINT fgk_ro_rooms FOREIGN KEY(room_id) REFERENCES rooms(room_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT fgk_ro_users FOREIGN KEY(user_id) REFERENCES users(user_id) ON DELETE CASCADE ON UPDATE CASCADE
);


