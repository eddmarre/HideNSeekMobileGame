using System.Threading.Tasks;
using UnityEngine;


public class Shape : MonoBehaviour
{
    public async Task BeginRotation(float duration)
    {
        var end = Time.time + duration;
        while (Time.time < end)
        {
            transform.Rotate(new Vector3(1, 1) * Time.deltaTime * 150f);
            await Task.Yield();
        }
        
    }
}