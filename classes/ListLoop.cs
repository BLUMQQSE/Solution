using System.Collections.Generic;
/// <summary>
/// A class for storing an array of items, and retrieving them in a loop. The time since last retrieved is returned
/// along with the element in GetNext().
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListLoop<T>
{
    private List<T> list;
    private double[] time;
    private int nextIndex;
    public ListLoop(List<T> initialArray)
    {
        list = initialArray;
        time = new double[list.Count];

        AppManager.Instance.AppUpdate += Update;
    }

    public void Dispose()
    {
        AppManager.Instance.AppUpdate -= Update;
    }

    private void Update(double delta)
    {
        for(int i = 0; i < list.Count; i++)
        {
            time[i] += delta;
        }
    }

    public (T, double) GetNext()
    {
        (T, double) result = (list[nextIndex], time[nextIndex]);
        time[nextIndex] = 0.0;
        nextIndex = (nextIndex + 1) % list.Count;
        return result;
    }

}