#define SPESS_DEVICE(name) /datum/spess_peripheral/##name \
/datum/spess_periph_def/##name \
    periph_path = /datum/spess_peripheral/##name

#define SPESS_METHOD(ppath, rtv, argl) ppath =  new /datum/spess_callback(rtv, argl)

#define SC_TUNDEFINED 0
#define SC_TINT 1
#define SC_TFLOAT 2
#define SC_TBOOL 3
#define SC_TSTRING 4
#define SC_TBLOB 5
#define SC_TLIST 6
#define SC_TMAP 7
#define SC_TINVALID 8
#define SC_TNULLABLE 128

// these are for readability
#define SC_TNUMBER SC_TFLOAT
#define SC_TANY SC_TUNDEFINED
#define SC_NONE SC_TNULLABLE