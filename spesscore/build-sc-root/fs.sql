create table fmeta (
    inode integer primary key,
    name text not null,
    parent integer,
    owner integer not null,
    group integer not null,
    mtime integer not null,
    permissions integer not null,
    size integer not null,
    foreign key(parent) references fmeta(inode) on delete cascade
) strict;

create table fdata (
    inode integer not null,
    blkid integer not null,
    data blob not null,
    foreign key(inode) references fmeta(inode) on delete cascade
) strict;