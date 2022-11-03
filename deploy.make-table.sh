# !/bin/bash

table_name=()
table_name+=("pre_users")
table_name+=("users")
table_name+=("sessions")
table_name+=("rooms")
table_name+=("room_owners")
table_name+=("room_users")
table_name+=("room_keywords")
table_name+=("quizzes")
table_name+=("quiz_templates")
table_name+=("quiz_template_keywords")
table_name+=("friends_following")
table_name+=("friends_followed")


echo "" > table.sql

for v in "${table_name[@]}"
do
    cat dev/db/table/$v.sql >> table.sql
done

