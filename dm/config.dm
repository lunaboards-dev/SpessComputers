/datum/config/string/spess_workspace_path
    default = "./spesscomputers/"

/datum/config/string/spess_ipc_socket_path
    default = "socket"

/datum/config/string/spess_executable_path
    default = world.system_type == MS_WINDOWS ? "spesscore/spesscore.exe" : "spesscore/spesscore"

/datum/config/number/spess_thread_count
    default = 4
    integer = TRUE
    min_val = 1

/datum/config/number/spess_input_buffer_size
    default = 1024
    integer = TRUE
    min_val = 512

/datum/config/number/spess_event_buffer_size
    default = 32
    integer = TRUE
    min_val = 8

/datum/config/number/spess_max_net_packet_size
    default = 4096
    integer = TRUE
    min_val = 512

/datum/config/number/spess_max_active_computers
    default = 200
    integer = TRUE

/datum/config/number/spess_max_memory_per_computer
    default = 12288 // KiB
    integer = TRUE

/datum/config/number_list/spess_memory_sizes

/datum/config/number_list/spess_disk_sizes

/datum/config/flag/spess_no_create_default_tables

// debug settings
/datum/config/flag/spess_debug_allow_gc_hooks

/datum/config/flag/spess_debug_use_native_pattern_matching

/datum/config/flag/spess_debug_enable_control_ws