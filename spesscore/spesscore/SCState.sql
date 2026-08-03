create table computers (
    ref int,
    localtty text,
    drive text,
    eeprom text,
    memory int
);

create table eeprom (
    ref int,
    id text,
    code blob
);

create table drive (
    ref int,
    id text,
    dsize int,
    ipath text
);