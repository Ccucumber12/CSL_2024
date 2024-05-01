using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public AudioClip sfx;

    public ParticleSystem[] confetti;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && GameManager.Instance.GoalCheck())
        {
            AudioSource audioSoruce = other.GetComponentInParent<AudioSource>();
            audioSoruce.clip = sfx;
            audioSoruce.Play();

            GameManager.Instance.GoalPassed();
            foreach(ParticleSystem particleSystem in confetti)
            {
                particleSystem.Play();
            }
        }
    }
}
