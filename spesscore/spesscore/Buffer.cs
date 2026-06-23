struct RingBuffer<T>(uint size) where T : struct
{
    T[] buffer = new T[size];
    uint ptr = 0;
    long size = size;
    public uint Count => ptr;

    public T[] Read(uint amt)
    {
        long ramt = Math.Min(amt, size);
        T[] rtv = new T[ramt];
        Array.Copy(buffer, rtv, ramt);
        Array.Copy(buffer, ramt, buffer, 0, size-ramt);
        size -= ramt;
        return rtv;
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
        return ptr;
    }
}