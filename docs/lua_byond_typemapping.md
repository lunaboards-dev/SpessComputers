# Lua<->Byond type mapping

| Lua | Intermediate | Byond | Notes |
| --- | ------------ | ----- | ----- |
| `string` (plain) | `RT_STRING` | String | UTF-8 is handled correctly. Null terminator is used "on the wire". |
| `string` (binary) | `RT_BLOB` | `/datum/lu_blob` | Blob reading in BYOND is handled by Lunar Utils. |
| `number` (int) | `RT_INT` | Number | Integers over 0xFFFFFF will lose precision on the BYOND side. |
| `number` (float) | `RT_FLOAT` | Number | Precision will be lost on the BYOND side due to Lua using double precision floats. |
| `boolean` | `RT_BOOL` | Number | |
| `table` (array) | `RT_LIST` | List | |
| `table` (non-array) | `RT_MAP` | Associative List | Key-value pair order should not be depended on. Lua doesn't have ordered maps. |
| `nil` | `RT_NULL` or `RT_UNDEFINED` | Null | `RT_NULL` is also a flag for nullable types. Both `RT_NULL` and `RT_UNDEFINED` map to `nil` and `NULL`. |