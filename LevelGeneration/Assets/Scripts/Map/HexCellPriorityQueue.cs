using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCellPriorityQueue {

    List<HexCell> list = new List<HexCell>();
    int count = 0;
    int minimum = int.MaxValue;

    /// <summary>
    /// Returns the count of the Hex priority list
    /// </summary>
    public int Count {
        get {
            return count;
        }
    }

    /// <summary>
    /// Adds Cell into the list. Checks its priority to see if it is the new minimum priority cell. Assigns the cell in order of their priority. 
    /// If there is multiple cells with the same priority, the cell will link to the previous one.
    /// </summary>
    /// <param name="cell"> Hex cell added into list </param>
    public void Enqueue (HexCell cell) {
        count += 1;
        int priority = cell.SearchPriority;
        if(priority < minimum) {
            minimum = priority;
        }
        while (priority >= list.Count) {
            list.Add(null);
        }
        cell.NextWithSamePriority = list[priority];
        list[priority] = cell;
    }

    /// <summary>
    /// Grabs the first cell of the list based on its priority. The cell is then replaced by at that same location in the list if it shares the same priority with another hex.
    /// If there is no other with the same priority, it becomes a null and will be skipped in later iterations. Essentially removes it from the list 
    /// </summary>
    /// <returns> Hex cell with the lowest priority </returns>
    public HexCell Dequeue() {
        count -= 1;
        for(; minimum < list.Count; minimum++) {
            HexCell cell = list[minimum];
            if(cell != null) {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// Replaces a hex cells in list with same priorty to a new cell. Loops through all chained hexes with same priority to replace certain hex
    /// </summary>
    /// <param name="cell"> Cell that needs to be changed </param>
    /// <param name="oldPriority"> Its old priority index </param>
    public void Change(HexCell cell, int oldPriority) {
        HexCell current = list[oldPriority];
        HexCell next = current.NextWithSamePriority;
        if(current == cell) {
            list[oldPriority] = next;
        }
        else {
            while(next != cell) {
                current = next;
                next = current.NextWithSamePriority;
            }
            current.NextWithSamePriority = cell.NextWithSamePriority;
        }
        Enqueue(cell);
        count -= 1;
    }
       
    /// <summary>
    /// Clears list
    /// </summary>
    public void Clear() {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}
