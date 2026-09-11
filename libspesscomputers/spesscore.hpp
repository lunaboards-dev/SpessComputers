#pragma once
#include <byondapi.h>
#include <format>
#include <string.h>
#include <vector>
#include "utils.hpp"

struct sc_state {
    bool running;
    bool crashed;
    int code;
};

class SpessCore {
    public:
        virtual bool spawn(std::string path, std::vector<std::string> args) = 0;
        virtual sc_state state() = 0;
        bool dead(CByondValue &src) {
            auto st = state();
            if (st.running) return false;
            if (st.crashed) { // everything is fucked
                bwoink(src, std::format("spesscore crashed! ({})", strsignal(st.code)));
                return true;
            } else { // what? probably an uncaught C# exception, though this should SIGABRT
                bwoink(src, std::format("spesscore stopped! (exit code: {})", st.code));
                return true;
            }
            return false;
        }
};

extern SpessCore * Spesscore;