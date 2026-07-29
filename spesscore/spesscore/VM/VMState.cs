namespace spesscore.VM;

enum VMState : int {
    Active = 1,
    Paused = 2,
    IOWait = 4
}