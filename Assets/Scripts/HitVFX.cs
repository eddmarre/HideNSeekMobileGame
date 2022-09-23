using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HitVFX : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 2f);
    }
}