using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class InteractSpawner : MonoBehaviour
{
        [SerializeField] private Interactable _interactable;
        [SerializeField] private int numberOfRunesToFind;
        [SerializeField] private Transform[] _interactableSpawns;

        public static InteractSpawner Instance { get; private set; }

        private void Awake()
        {
                Instance = this;
        }


        public void SpawnObjects()
        {
                for (int i = 0; i < numberOfRunesToFind; i++)
                {
                        var randomIndex = Random.Range(0, _interactableSpawns.Length);
                        var testInteract=Instantiate(_interactable.gameObject,
                                _interactableSpawns[randomIndex].position,
                              _interactableSpawns[randomIndex].rotation);
                        testInteract.GetComponent<NetworkObject>().Spawn();
                }        
        }
}
