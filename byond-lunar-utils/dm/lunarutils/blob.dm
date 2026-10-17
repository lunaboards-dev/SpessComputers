#define IS_CLOSED (ptr == null)
#define LOAD_EXT(call) call = load_ext(world.system_type == "windows" ? "./lunarutils.dll" : "./liblunarutils.so", "byond:"+#call)

/datum/lu_blob
    var/ptr

    var/global/lu_blob_read
    var/global/lu_blob_read8
    var/global/lu_blob_read16
    var/global/lu_blob_readfloat
    var/global/lu_blob_seek
    var/global/lu_blob_size
    var/global/lu_blob_close

/datum/lu_blob/proc/open_lib()
    if (lu_blob_read != null)
        return
    LOAD_EXT(lu_blob_read)
    LOAD_EXT(lu_blob_read8)
    LOAD_EXT(lu_blob_read16)
    LOAD_EXT(lu_blob_readfloat)
    LOAD_EXT(lu_blob_seek)
    LOAD_EXT(lu_blob_size)
    LOAD_EXT(lu_blob_close)

/datum/lu_blob/proc/closed()
    open_lib()
    return IS_CLOSED

/datum/lu_blob/proc/readstr(count)
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_read)(src, count)

/datum/lu_blob/proc/read8(signed = FALSE)
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_read8)(src, signed)

/datum/lu_blob/proc/read16(signed = FALSE, be = FALSE)
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_read16)(src, signed, be)

/datum/lu_blob/proc/readfloat(be = FALSE)
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_readfloat)(src, be)

/datum/lu_blob/proc/seek(whence = SEEK_CUR, offset = 0)
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_seek)(src, whence, offset)

/datum/lu_blob/proc/size()
    open_lib()
    if IS_CLOSED
        throw "cannot read from closed handle"
    return call_ext(lu_blob_size)(src)

/datum/lu_blob/proc/close()
    open_lib()
    if (!IS_CLOSED)
        return call_ext(lu_blob_close)(src)

/proc/lu_openfile(path)
    return call_ext(world.system_type == "windows" ? "./lunarutils.dll" : "./liblunarutils.so", "byond:lu_blob_openfile")(path)

#undef IS_CLOSED
#undef LOAD_EXT