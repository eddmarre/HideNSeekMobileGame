using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBehaviour : MonoBehaviour
{
    private Button _button;
    private Transform _outLine;
    private Vector3 initialScale;
    private TextMeshPro _numberText;
    private float _currentTime;

    private void Awake()
    {
        _numberText = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        _button = GetComponent<Button>();
        _outLine = transform.GetChild(0);
        
        // _button.onClick.AddListener(PopButton);

        initialScale = _outLine.localScale;
    }

    private void Update()
    {
        var X = Mathf.Clamp(_outLine.localScale.x, 0f, 1f);
        var Y = Mathf.Clamp(_outLine.localScale.y, 0f, 1f);
        var Z = Mathf.Clamp(_outLine.localScale.z, 0f, 1f);
        _outLine.localScale -= new Vector3(X, Y, Z) * (GameManager.Instance.GetTimeForButtonsToDestroy /
                                                       GameManager.Instance.GetTimeForOutlineToClose);

        _currentTime += Time.deltaTime;
        if (_currentTime > GameManager.Instance.GetTimeForButtonsToDestroy)
        {
            Debug.Log("missed");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        ButtonSpawner.Instance.QueueRemoveButton(gameObject);
    }

    public void PopButton()
    {
        // Debug.Log(_outLine.localScale);
        var scale = _outLine.localScale;
        var excellentCompareVectors = new CompareVectors(scale, new Vector3(1.1f, 1.1f, 1.1f));
        var okCompareVectors = new CompareVectors(scale, new Vector3(1.5f, 1.5f, 1.5f));
        var badCompareVectors = new CompareVectors(scale, new Vector3(2.0f, 2.0f, 2.0f));

        if (excellentCompareVectors.LessThan())
        {
            Debug.Log($"Excellent {_outLine.localScale}");
        }
        else if (excellentCompareVectors.GreaterThan() && okCompareVectors.LessThan())
        {
            Debug.Log($"OK {_outLine.localScale}");
        }
        else if (badCompareVectors.GreaterThan())
        {
            Debug.Log($"bad {_outLine.localScale}");
        }

        Destroy(gameObject);
    }

    public void SetNumberText(int number)
    {
        _numberText.text = number.ToString();
    }
}

internal struct CompareVectors
{
    private Vector3 _a, _b;

    public CompareVectors(Vector3 a, Vector3 b)
    {
        _a = a;
        _b = b;
    }


    public bool LessThan()
    {
        if (_a.x < _b.x && _a.y < _b.y && _a.z < _b.z)
        {
            return true;
        }

        return false;
    }

    public bool GreaterThan()
    {
        return !LessThan();
    }
}