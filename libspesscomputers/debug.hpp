#pragma once
#ifdef __linux__
#include <list>
#include <cstdlib>
#include <cstdio>

static std::list<void*> ptrs;

static void * scdalloc(size_t amt) {
    void * blk = malloc(amt);
    if (blk != NULL)
        ptrs.push_back(blk);
    return blk;
}

static void scdfree(void * ptr) {
    for (std::list<void*>::iterator it = ptrs.begin(); it != ptrs.end();) {
        if (*it == ptr) {
            ptrs.erase(it);
            free(ptr);
            return;
        }
    }
    fprintf(stderr, "Double free detected!");
    abort();
}

#ifdef SC_DEBUG
#define sc_alloc scdallc
#define sc_free scdfree
#else
#define sc_alloc malloc
#define sc_free free
#endif

#endif