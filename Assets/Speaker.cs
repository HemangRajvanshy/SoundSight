using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speaker : MonoBehaviour {

	public AudioSource audioSource;

	public void Play() {
		audioSource.Play();
	}

	public void Stop() {
		audioSource.Pause();
	}
}
