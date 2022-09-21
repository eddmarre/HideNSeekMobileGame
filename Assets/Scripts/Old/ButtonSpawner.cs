using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ButtonSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _button;
    //[SerializeField] private Canvas _canvas;

    private List<GameObject> _buttons;
    private List<GameObject> _buttonsToRemove;

    public static ButtonSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _buttons = new List<GameObject>();
        _buttonsToRemove = new List<GameObject>();
    }


    public async void BeginGame(float timeToWait, int delayTime, int timeToDestroyButton)
    {
        // List<Task> tasks = new List<Task>();
        // for (int i = 0; i < 3; i++)
        // {
        //      tasks.Add(SpawnButtonInRandomLocation(timeToWait, delayTime));
        // }
        // await Task.WhenAll(tasks);
        await Task.Delay(1000);
        for (int i = 0; i < 3; i++)
        {
            await SpawnButtonInRandomLocation(timeToWait, delayTime);
            await Task.Delay(timeToDestroyButton * 1000);
            ClearButtons();
        }
    }

   
    private async Task SpawnButtonInRandomLocation(float timeToWait, int delayTime)
    {
        var currentTime = Time.time + timeToWait;
        while (Time.time < currentTime)
        {
            var randomWidth = Random.Range(-3, 4);
            var randomHeight = Random.Range(-4, 7);


            var newButton = Instantiate(_button, new Vector3(randomWidth, randomHeight), transform.rotation);


            _buttons.Add(newButton);

            newButton.GetComponent<ButtonBehaviour>().SetNumberText(_buttons.Count);

            //NetworkObject.Spawn(newButton);

            await Task.Delay(delayTime);
            await Task.Yield();
        }

        // ClearButtons();
    }
    
    public async void RestartGame(float timeToWait, int delayTime, int timeToDestroyButton)
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                await SpawnButtonInRandomLocation(timeToWait, delayTime);
                await Task.Delay(timeToDestroyButton * 1000);
                ClearButtons();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{e.Message}");
        }
    }

    private void ClearButtons()
    {
        foreach (var button in _buttonsToRemove)
        {
            _buttons.Remove(button);
        }

        _buttonsToRemove.Clear();

        foreach (var button in _buttons)
        {
            Destroy(button.gameObject);
        }

        _buttons.Clear();
    }

    public void QueueRemoveButton(GameObject button)
    {
        _buttonsToRemove.Add(button);
    }
}