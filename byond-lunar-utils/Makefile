source_files := $(wildcard src/*.cpp)
object_files := $(patsubst %.cpp,%.o,$(source_files))

CFLAGS := -g -fPIC -m32 -Iinclude/ -Isrc/ -lbyond -lext

%.o : %.cpp
	$(CC) -c $(CFLAGS) $(CPPFLAGS) $< -o $@

liblunarutils.so: $(object_files)
	$(CC) -shared $(CFLAGS) $(CPPFLAGS) $(object_files) -o liblunarutils.so

all: liblunarutils.so
clean:
	rm $(object_files)