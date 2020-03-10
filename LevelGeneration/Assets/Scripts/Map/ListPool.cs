using System.Collections.Generic;
        
public static class ListPool<T>
{

    static Stack<List<T>> stack = new Stack<List<T>>();

    /// <summary>
    /// Returns either the first list on the stack or creates a new list to be used if the stack does not have a list
    /// </summary>
    /// <returns> A reusable list </returns>
    public static List<T> Get()
    {
        if(stack.Count > 0)
        {
            return stack.Pop();
        }
        return new List<T>();
    }

    /// <summary>
    /// Adds List back onto the stack. Clears the list of its contents first though
    /// </summary>
    /// <param name="list"> List</param>
    public static void Add (List<T> list)
    {
        list.Clear();
        stack.Push(list);
    }
}