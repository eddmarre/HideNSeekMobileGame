using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ShapeManager : MonoBehaviour
{
    [SerializeField] private Shape[] _shapes;

    public async void MoveShapes()
    {
        Debug.Log("start");
        var tasks = new List<Task>();
        for (int i = 0; i < _shapes.Length; i++)
        {
            tasks.Add(_shapes[i].BeginRotation(1 + 1 * i));
            // await _shapes[i].BeginRotation(1 + 1 * i);
        }

        await Task.WhenAll(tasks);
        Debug.Log("finished");
    }
}