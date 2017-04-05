using Array = System.Array;
using Vector2i = QM2D.Generator.Vector2i;


/// <summary>
/// Extensions to make my life easier.
/// Most of these just allow 2D arrays to work nicely with my Vector2i struct.
/// </summary>
public static class MyExtensions
{
    /// <summary>
    /// Wraps this integer around the range [0, max).
    /// </summary>
    public static int Wrap(this int i, int max)
    {
        while (i < 0)
            i += max;
        return i % max;
    }
    

    public static T Get<T>(this T[,] array, Vector2i pos)
    {
        return array[pos.x, pos.y];
    }
    public static void Set<T>(this T[,] array, Vector2i pos, T newVal)
    {
        array[pos.x, pos.y] = newVal;
    }

    public static bool IsInRange<T>(this T[,] array, Vector2i pos)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x < array.GetLength(0) && pos.y < array.GetLength(1);
    }

    public static int SizeX(this Array array) { return array.GetLength(0); }
    public static int SizeY(this Array array) { return array.GetLength(1); }
    public static Vector2i SizeXY<T>(this T[,] array) { return new Vector2i(array.SizeX(), array.SizeY()); }

    public static Vector2i.Iterator AllIndices<T>(this T[,] array) { return new Vector2i.Iterator(array.SizeXY()); }
}