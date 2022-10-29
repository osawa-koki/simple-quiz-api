

CREATE TABLE quiz_template_keywords(
	quiztemplate_id VARCHAR(32),
	keyword NVARCHAR(30),
	CONSTRAINT fgk_tmpl_kwd FOREIGN KEY(quiztemplate_id) REFERENCES quiz_templates(quiztemplate_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT fgk_pre_kwds FOREIGN KEY(keyword) REFERENCES prepared_quiz_template_keywords(keyword) ON DELETE CASCADE ON UPDATE CASCADE
);


