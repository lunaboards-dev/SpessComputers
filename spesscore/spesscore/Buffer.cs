struct RingBuffer<T>(uint size) where T : struct
{
    T[] buffer = new T[size];
    uint ptr = 0;
    long size = size;
    public uint Count => ptr;

    public T[] Read(uint amt)
    {
        if (ptr == 0) return [];
        long ramt = Math.Min(amt, size);
        T[] rtv = new T[ramt];
        Array.Copy(buffer, rtv, ramt);
        Array.Copy(buffer, ramt, buffer, 0, size-ramt);
        size -= ramt;
        return rtv;
    }

    public T? Next()
    {
        if (ptr == 0) return default;
        var res = Read(1);
        if (res.Length == 0) return default; // ???
        return res[0];
    }

    public uint Write(T[] data)
    {
        int dsize = data.Length;
        if (dsize == 0)
        {
            return ptr;
        }
        if (dsize >= size) // how did we get here
        {
            long doffset = dsize - size;
            Array.Copy(buffer, 0, data, doffset, size);
            ptr = (uint)size;
            return ptr;
        }
        if (dsize+ptr > size)
        {
            long shift = dsize+ptr-size;
            Array.Copy(buffer, shift, buffer, 0, size-shift);
            ptr -= (uint)shift;
        }
        Array.Copy(data, 0, buffer, ptr, dsize);
        ptr+= (uint)dsize;
        return ptr;
    }

    public uint Add(T val)
    {
        return Write([val]);
    }

    public void Clear()
    {
        ptr = 0;
    }
}