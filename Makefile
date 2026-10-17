source_files := $(wildcard libspesscomputers/*.cpp)
object_files := $(patsubst %.cpp,%.o,$(source_files))

CFLAGS := -g -fPIC -m32 -Lbyond-lunar-utils -Ilibspesscomputers/ -Ibyond-lunar-utils/include -lbyond -lext -llunarutils

.PHONY: byond-lunar-utils/liblunarutils.so spesscore

%.o : %.cpp
	$(CC) -c $(CFLAGS) $(CPPFLAGS) $< -o $@

libspesscomputers.so: byond-lunar-utils/liblunarutils.so $(object_files)
	$(CC) -shared $(CFLAGS) $(CPPFLAGS) $(object_files) -o libspesscomputers.so

byond-lunar-utils/liblunarutils.so:
	$(MAKE) -C byond-lunar-utils

spesscore:
	dotnet publish spesscore/spesscore.slnx -o build

all: libspesscomputers.so spesscore

clean:
	rm $(object_files)