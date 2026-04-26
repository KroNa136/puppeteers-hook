using System.Collections.Generic;

public static class CreateList
{
    public static List<T> With<T>(params T[] items) => new(items);
}
