#define SPESS_DEVICE(name) /datum/spess_peripheral/##name
#define SPESS_METHOD(name, args)var/def_##name = args; proc/##name