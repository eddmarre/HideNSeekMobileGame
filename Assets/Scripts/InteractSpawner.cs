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

        public static InteractSpawner Instance { get; private set; }

        private void Awake()
        {
                Instance = this;
        }


        public void SpawnObjects()
        {
                for (int i = 0; i < numberOfRunesToFind; i++)
                {
                        var testInteract=Instantiate(_interactable.gameObject,
                                new Vector3(Random.Range(-100f, 100f), transform.position.y, Random.Range(-100f, 100f)),
                                Quaternion.identity);
                        testInteract.GetComponent<NetworkObject>().Spawn();
                }        
        }
}
