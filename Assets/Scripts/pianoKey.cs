using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class pianoKey : MonoBehaviour {
	public byte keyNr; //1 is outerleft, 88 outerright
	public AudioClip soundSample;

	protected AudioSource audio;
	protected Button button;

	protected GameObject notesImage;

	void Start () {
		notesImage = GameObject.FindGameObjectWithTag ("texture");
		audio = gameObject.AddComponent<AudioSource>();
		audio.clip = soundSample;
		audio.playOnAwake = false;
		button = this.gameObject.GetComponent<Button>();
		button.onClick.AddListener(PlayAndRecord);
	}
	
	void PlayAndRecord () {
		audio.Play ();
		notesImage.GetComponent<Track> ().AddToSong (keyNr);
	}
}
