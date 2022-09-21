using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Button _button;

    [SerializeField] private float _timeForButtonsToDestroy = 1f;
    [SerializeField] private float _timeForOutlineToClose = 60f;
    [SerializeField] private float _timeToWait = 5f;
    [SerializeField] private int _delayTime = 1000;
    public static GameManager Instance { get; private set; }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Extreme,
        Impossible
    };

    [SerializeField] private Difficulty _difficulty;

    public float GetTimeForButtonsToDestroy
    {
        get { return _timeForButtonsToDestroy; }
        private set { }
    }

    public float GetTimeForOutlineToClose
    {
        get { return _timeForOutlineToClose; }
        private set { }
    }

    public int GetDelayTime
    {
        get { return _delayTime; }
        private set { }
    }

    public float GetTimeToWait
    {
        get { return _timeToWait; }
        private set { }
    }

    private void Awake()
    {
        Instance = this;
        ChangeDifficulty( /*(int) _difficulty*/);
    }

    private void Start()
    {
        // ButtonSpawner.Instance.BeginGame(_timeToWait, _delayTime, (int) _timeForButtonsToDestroy);
    }

    public void RestartGame()
    {
        ButtonSpawner.Instance.RestartGame(_timeToWait, _delayTime, (int) _timeForButtonsToDestroy);
    }

    public void ChangeDifficulty( /*int difficulty*/)
    {
        // _difficulty = (Difficulty) Enum.ToObject(typeof(Difficulty), difficulty);

        switch (_difficulty)
        {
            case Difficulty.Easy:
                // _timeForButtonsToDestroy = 5f;
                // _timeForOutlineToClose = 1700f;
                _timeForButtonsToDestroy = 4f;
                _timeForOutlineToClose = 1100f;
                _timeToWait = 5f;
                _delayTime = 1500;
                break;
            case Difficulty.Normal:
                _timeForButtonsToDestroy = 3f;
                _timeForOutlineToClose = 600f;
                _timeToWait = 5f;
                _delayTime = 1000;
                break;
            case Difficulty.Hard:
                _timeForButtonsToDestroy = 2f;
                _timeForOutlineToClose = 250f;
                _timeToWait = 7f;
                _delayTime = 750;
                break;
            case Difficulty.Extreme:
                _timeForButtonsToDestroy = 1f;
                _timeForOutlineToClose = 60f;
                _timeToWait = 10f;
                _delayTime = 600;
                break;
            case Difficulty.Impossible:
                _timeForButtonsToDestroy = .5f;
                _timeForOutlineToClose = 15f;
                _timeToWait = 13f;
                _delayTime = 500;
                break;
        }
    }
}