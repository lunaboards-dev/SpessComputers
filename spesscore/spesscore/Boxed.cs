namespace spesscore;

// ugly hack lol
struct Boxed<T>
{
    public T Value;

    public Boxed(T val) { Value = val; }
}