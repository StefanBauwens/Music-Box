using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using SFB;

//Made by Stefan Bauwens

public class Track : MonoBehaviour {
	protected string mTrackName;
	protected List<byte> mSong;
	protected List<pianoKey> pianoKeys;
	protected List<Texture2D> trackParts  = new List<Texture2D>(); //list of textures that form the track
	protected int trackIndex;
	protected Texture2D mTrackImage;
	protected int index;
	protected int playIndex;
	protected bool coroutineRunning;
	protected bool settingSlider;
	protected AudioSource audio;
	public Text inputFieldSpeed;
	protected float playSpeedDelay = 120f;
	public Slider indexSlider;
	public Button prevButton;
	public Button nextButton;

	const int TEXTUREWIDTH 	= 1712;
	const int TEXTUREHEIGHT = 342;
    const int RESOLUTIONWIDTH = 1680;
    const int RESOLUTIONHEIGHT = 1050;
    const int SHEETLENGTH = 40;


	public Vector2 size;

	void Awake()
	{
        Screen.SetResolution(RESOLUTIONWIDTH, RESOLUTIONHEIGHT, false);
	}

	public string TrackName
	{
		get{
			return mTrackName;
		}
		set {
			mTrackName = value;
		}
	}

	public Texture2D TrackImage
	{
		get{
			return mTrackImage;
		}
		set{
			mTrackImage = value;
		}
	}

	protected void GreyOutButtons()
	{
		prevButton.interactable = (trackIndex > 0);
		nextButton.interactable = (trackIndex < trackParts.Count-1);
	}

	public void changedSliderValue(float value)
	{
		if (settingSlider) { //if this bool is true, it means the slider has been edited by script. In that case do nothing.
			return;
		}
        index = (int)value + (trackIndex * SHEETLENGTH)-1;
	}

	public void Next()
	{
		if (trackIndex < trackParts.Count-1) {
			trackIndex++;
            index += SHEETLENGTH;
			GreyOutButtons ();
		}
		GetComponent<RawImage> ().texture = trackParts [trackIndex];
	}

	public void Prev()
	{
		if (trackIndex > 0) {
			trackIndex--;
            index -= SHEETLENGTH;
			GreyOutButtons ();
		}
		GetComponent<RawImage> ().texture = trackParts [trackIndex];
	}

	public void ChangeSpeed()
	{
		float newSpeed = playSpeedDelay;
		float.TryParse (inputFieldSpeed.text, out newSpeed);
		if (newSpeed > 0) {
			playSpeedDelay = newSpeed;
		}
	}

	public void AddToSong(byte keyNumber)
	{
		index++;
		mSong[index] = keyNumber; //adds key to list


		string binaryNumber = System.Convert.ToString (keyNumber, 2);

        if (index !=0 && (index)%SHEETLENGTH == 0 && index!=(trackIndex*SHEETLENGTH)) { //checks to see wheter you need to go to next "sheet"			
			trackIndex++;
			if (trackIndex>trackParts.Count-1) {
				Texture2D newTrackImage = new Texture2D ((int)size.x, (int)size.y, TextureFormat.RGB24, false);
				newTrackImage.filterMode = FilterMode.Point;
				trackParts.Add (newTrackImage);
				ClearTrack (trackIndex);
				addNewSheet ();
			}
			GreyOutButtons ();
			GetComponent<RawImage> ().texture = trackParts [trackIndex];
		}

		settingSlider = true;
        indexSlider.value = index%SHEETLENGTH; 
		settingSlider = false;

        //draw it on texture:
        clearRecord (index % SHEETLENGTH);
		for (int i = 0; i < binaryNumber.Length; i++) {
            trackParts[trackIndex].SetPixel(index%SHEETLENGTH, trackParts[trackIndex].height-2-i, (binaryNumber[binaryNumber.Length-i-1]=='1')?Color.black:Color.white); 
		}
		trackParts [trackIndex].Apply ();
	}

	public void deleteTrackRecord()
	{
        if (index%SHEETLENGTH == 0) {
			mSong [index] = 0;
            clearRecord (index%SHEETLENGTH);
			index--;
		}
        else if (index%SHEETLENGTH > 0 && index!=(trackIndex*SHEETLENGTH)-1) {
			mSong[index] = 0;
            clearRecord (index%SHEETLENGTH);
			index--;

			settingSlider = true;
            indexSlider.value = index%SHEETLENGTH; 
			settingSlider = false;
		}
	}

	protected void clearRecord(int index)
	{
		for (int i = 0; i < mTrackImage.height-1; i++) {
			trackParts[trackIndex].SetPixel (index, i, Color.white);
		}
		trackParts [trackIndex].Apply ();
	}

	public void play()
	{
		if (coroutineRunning) {
			StopCoroutine ("playSong");
			coroutineRunning = false;
		} else {
			playIndex = 0;
			StartCoroutine ("playSong");
		}
	}

	void SaveTextureToFile (Texture2D texture, string filename, int factor) { 
		Texture2D copyText =  new Texture2D ((int)size.x, (int)size.y*factor, TextureFormat.RGB24, false);
		Graphics.CopyTexture (texture, copyText);
		TextureScale.Point (copyText, TEXTUREWIDTH, TEXTUREHEIGHT*factor); //scale it up 
		try {
			System.IO.File.WriteAllBytes (filename, copyText.EncodeToPNG());
			Debug.Log("Saved file succesfully!");
		} catch (System.Exception ex) {
			Debug.Log ("Problem saving file: " + ex);
		}
	}

	public void Save()
	{
		string destinationName = StandaloneFileBrowser.SaveFilePanel ("", "", "notes", "");
		int i = 0;
		while (i<trackParts.Count) {
			if (i + 2 < trackParts.Count) {
                SaveToTex (trackParts [i], trackParts [i + 1], trackParts [i + 2], destinationName + i.ToString () + (i + 1).ToString () + (i + 2).ToString ());
				i += 3;
			}
			else if (i + 1 < trackParts.Count) {
                SaveToTex (trackParts [i], trackParts [i + 1], destinationName + i.ToString () + (i + 1).ToString ());
				i += 2;
			}
			else if (i < trackParts.Count){
				SaveToTex (trackParts [i], destinationName + i.ToString ());
				i++;
			}
		}
	}

	protected void SaveToTex(Texture2D sheet1, string name)
	{
		SaveTextureToFile (sheet1, name + ".png", 1);
	}

	protected void SaveToTex(Texture2D sheet1, Texture2D sheet2, string name)
	{
		Texture2D twoTextures = new Texture2D ((int)size.x, (int)size.y*2, TextureFormat.RGB24, false);
		mTrackImage.filterMode = FilterMode.Point;
		twoTextures.SetPixels (0, sheet1.height, sheet1.width, sheet1.height, sheet1.GetPixels());
		twoTextures.SetPixels (0,0, sheet2.width, sheet2.height,sheet2.GetPixels ());

		twoTextures.Apply();
		SaveTextureToFile (twoTextures, name + ".png", 2);
	}

	protected void SaveToTex(Texture2D sheet1, Texture2D sheet2, Texture2D sheet3, string name)
	{
		Texture2D threeTextures = new Texture2D ((int)size.x, (int)size.y*3, TextureFormat.RGB24, false);
		mTrackImage.filterMode = FilterMode.Point;
		threeTextures.SetPixels (0, sheet1.height*2, sheet1.width, sheet1.height, sheet1.GetPixels());
		threeTextures.SetPixels (0, sheet2.height, sheet2.width, sheet2.height,sheet2.GetPixels ());
		threeTextures.SetPixels (0, 0, sheet3.width, sheet3.height, sheet3.GetPixels ());

		threeTextures.Apply();
		SaveTextureToFile (threeTextures, name + ".png", 3);
	}

	protected IEnumerator playSong()
	{
		trackIndex = 1;
		Prev ();
		settingSlider = true;
		indexSlider.value = 0; 
		settingSlider = false;

		while (playIndex < mSong.Count) {
			coroutineRunning = true;
			try {
				pianoKey temp = pianoKeys.Find (x => x.name == ("key" + mSong [playIndex]));
				audio.clip = temp.soundSample;
				audio.Play ();
			} catch (System.Exception ex) {
					
			}
            settingSlider = true;
            indexSlider.value = playIndex % (SHEETLENGTH+1);
            settingSlider = false;

			playIndex++;
            if (playIndex%SHEETLENGTH == 0) {
				Next ();
			}
			yield return new WaitForSeconds (60f/playSpeedDelay);
		}
		coroutineRunning = false;
	}

	public void Start () {

		trackIndex = 0;
		coroutineRunning = false;
		StopAllCoroutines ();
		trackParts.Clear ();
		settingSlider = false;
		indexSlider.value = 0;
		indexSlider.onValueChanged.AddListener (changedSliderValue);

		//get all pianoKeys
		pianoKeys = GameObject.FindObjectsOfType<pianoKey>().ToList();

		ChangeSpeed ();
		audio = GetComponent<AudioSource> ();
		index = -1;
		playIndex = 0;
		mSong = new List<byte>();
		mTrackImage = new Texture2D ((int)size.x, (int)size.y, TextureFormat.RGB24, false);
		mTrackImage.filterMode = FilterMode.Point;

		trackParts.Add (mTrackImage);
		addNewSheet ();

		//Clear the track
		ClearTrack(0);
		GreyOutButtons ();
		GetComponent<RawImage> ().texture = mTrackImage;
	}

	protected void ClearTrack(int trackIndex)
	{
		bool alternate = false;
		for (int i = 0; i < trackParts[trackIndex].width; i++) {
			alternate = !alternate;
			if (alternate) {
				trackParts[trackIndex].SetPixel(i, trackParts[trackIndex].height-1, Color.black);
			} else {
				trackParts[trackIndex].SetPixel(i, trackParts[trackIndex].height-1, Color.white);
			}
			clearRecord (i);
		}
		trackParts[trackIndex].Apply ();
	}

	protected void addNewSheet()
	{
        for (int i = 0; i < (SHEETLENGTH+1); i++) {
			mSong.Add (0);
		}
	}



}
