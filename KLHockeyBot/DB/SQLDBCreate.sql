DROP TABLE IF EXISTS player;
DROP TABLE IF EXISTS event;
DROP TABLE IF EXISTS voting;
DROP TABLE IF EXISTS vote;

CREATE TABLE player(
    id INTEGER PRIMARY KEY,
    number INTEGER NULL,
    name TEXT NOT NULL,
    userid INTEGER NOT NULL,
    lastname TEXT NOT NULL,
	lastname_lower TEXT NOT NULL,
    photo TEXT NULL,	
	position TEXT NULL,
	status TEXT NULL,
	secondname TEXT NULL,
	birthday TEXT NULL
);

CREATE TABLE event(
    id INTEGER PRIMARY KEY,
	type TEXT NOT NULL,
    date TEXT NOT NULL,
    time TEXT NOT NULL,
    place TEXT NULL,
    address TEXT NULL,
    details TEXT NULL,
    result TEXT NULL
);

CREATE TABLE voting(
    id INTEGER PRIMARY KEY,
    messageid INTEGER NULL,
    question TEXT NOT NULL
);

CREATE TABLE vote(
    id INTEGER PRIMARY KEY,
    messageid INTEGER NULL,
    userid INTEGER NOT NULL,
    username TEXT NOT NULL,
    name TEXT NOT NULL,
    surname TEXT NOT NULL,
    data TEXT NOT NULL
);

CREATE INDEX vote_messageid_idx ON vote (messageid);