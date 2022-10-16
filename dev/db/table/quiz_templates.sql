

CREATE TABLE quiz_templates(
	quiztemplate_id INT PRIMARY KEY IDENTITY(0, 1),
	owning_user VARCHAR(16) NULL,
	owning_session VARCHAR(32) NOT NULL,
	is_public BIT NOT NULL,
	content VARCHAR(300) NOT NULL,
	n_of_used INT DEFAULT 0,
	n_of_liked INT DEFAULT 0,
	n_of_disliked INT DEFAULT 0,
	rgdt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME(),
	updt DATETIME DEFAULT dbo.GET_TOKYO_DATETIME()
);

