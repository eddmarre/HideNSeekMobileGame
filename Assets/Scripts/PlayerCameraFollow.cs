using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public static PlayerCameraFollow Instance { get; private set; }
    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;

    private void Awake()
    {
        Instance = this;
    }

    public void FollowPlayer(Transform transform)
    {
        _cinemachineVirtualCamera.Follow = transform;
        _cinemachineVirtualCamera.LookAt = transform;
        var thirdPerson = _cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        thirdPerson.ShoulderOffset = new Vector3(.5f, 1.5f, 0f);
        thirdPerson.CameraDistance = 6f;
        var noise = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = .5f;
        noise.m_FrequencyGain = .5f;
    }
}