source_files := $(wildcard *.cpp)
object_files := $(patsubst %.cpp,%.o,$(source_files))

CFLAGS := -g -fPIC -m32 -I./ -lbyond -lext

%.o : %.cpp
	$(CC) -c $(CFLAGS) $(CPPFLAGS) $< -o $@

libspesscomputers.so: $(object_files)
	$(CC) -shared $(CFLAGS) $(CPPFLAGS) $(object_files) -o libspesscomputers.so

all: libspesscomputers.so
clean:
	rm $(object_files)