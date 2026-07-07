SPESS_DEVICE(example)
    name = "example"

    SPESS_METHOD(test, "test(ok:b,a:i,b:i,c:i):b")(ok, a, b, c) {
        if (!ok)
            return FALSE
        return a+b == c
    }