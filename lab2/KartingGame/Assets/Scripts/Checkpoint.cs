using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public AudioClip sfx;
    private bool isPassed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && isPassed == false)
        {
            isPassed = true;
            AudioSource audioSoruce = other.GetComponentInParent<AudioSource>();
            audioSoruce.clip = sfx;
            audioSoruce.Play();

            GameManager.Instance.CheckpointPassed();
        }
    }
}
