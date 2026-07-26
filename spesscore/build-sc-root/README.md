# build-sc-root
Poorly named, used to create read only images. Sorry about that.

## Script format
Root scripts are a list of commands and rules, with the first character specifying the type.

Currently supported types are as follows:
* `@` - Command
* `~` - Modify
* `-` - Exclude
* `+` - Add entry

Adding entries always have their arguments applied before rules are applied, and commands always run before files are added.

### Rules
Rules are a line that specifies a type, a path, and arguments and filters. Paths CANNOT contain spaces, as whitespace delimits the arguments. A trailing slash applies only to the children of the directory specified by the path, while not specifying a trailing slash also applies the rule to the directory. Filters are arguments with their key prefixed with `$`.

Example: `~path/to/file foo=bar $biz=baz`

### Commands
Special commands executed before the image is created.

Syntax: `@command argument`

#### `root`
Aliases: `source`
This command specifies the root of the image source files.

#### `output`
This command specifies the output file name of the image.

#### `exec`
This command executes an SQL query.

### Filters

#### `$ext=<arg>`
Filters by extension. Do not include a leading period.

#### `$type=<arg>`
Filters by file type. This can be `dir`, `file`, or `link`. (Or aliases thereof)

#### `$virtual`
Only apply to virtual files (added by `+` rules)

#### `$real`
Only apply to real files (those in the filesystem)

#### `$!exists`
Only apply to files that don't exist on the filesystem. Only useful for `+` rules.

### Arguments
These modify file metadata.

#### `chown=<owner>:<group>`
Changes the file ownership. Both owner and group must be specified.

#### `chmod=<rwx>`
Changes the file permissions. Specified as `rwxrwxrwx`, with bits being set to 0 with `-`.
**NOTE:** Permissions are stored with owner-read being bit 0 and other-execute being bit 8.

#### `type=<arg>`
Sets entry type. Only valid for `+` rules. See `$type` for accepted types.

#### `target=<path>`
Sets a symlink's target.