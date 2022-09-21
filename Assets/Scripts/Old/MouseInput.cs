using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            var hit = Physics2D.Raycast(_camera.transform.position, ray.direction,
                float.MaxValue);

            if (hit.collider == null) return;

            if (!hit.transform.TryGetComponent(out ButtonBehaviour button)) return;
            button.PopButton();
        }
    }
}