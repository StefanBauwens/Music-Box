using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class highlightBlack : MonoBehaviour {
	protected Color standardColour;
	public Color highlightColour;

	protected Button button;
	protected Image btnSprite;

	protected float currentTime = 0f;
	protected float timeToMove = 0.5f;
	protected bool clickedBtn = false;

	void Start () {
		button = GetComponent<Button> ();
		btnSprite = GetComponent<Image> ();
		button.onClick.AddListener (Highlight);
		standardColour = btnSprite.color;
	}

	void Update () {
		if (clickedBtn) {
			if (currentTime <= timeToMove) {
				currentTime += Time.deltaTime;
				btnSprite.color = Color.Lerp (highlightColour, standardColour, currentTime / timeToMove);
			} else {
				currentTime = 0f;
				clickedBtn = false;
			}
		}
	}

	void Highlight()
	{
		btnSprite.color = highlightColour;
		clickedBtn = true;
	}
}
