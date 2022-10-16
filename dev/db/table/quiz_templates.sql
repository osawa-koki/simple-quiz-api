

CREATE TABLE quiz_templates(
	quiztemplate_id CHAR(32) PRIMARY KEY,
	owning_user VARCHAR(16) NULL,
	owning_session CHAR(32) NOT NULL,
	is_public BIT NOT NULL,
	content VARCHAR(300) NOT NULL,
	n_of_used INT DEFAULT 0,
	n_of_liked INT DEFAULT 0,
	n_of_disliked INT DEFAULT 0,
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);

