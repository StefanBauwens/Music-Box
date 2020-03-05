using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SFB; //usign UnityStandaloneFileBrowser to open files
using NAudio; //using naudio to read midi files
using NAudio.Midi;
using UnityEngine.UI;
using System.Linq;

//Made by Stefan Bauwens
//It appears that nAudio midi uses 10 octaves (0-10) when reading midi notes. C5 equals 60. On a 88 keyboard piano C5 is key 52, so we have to subtract 8.

public class OpenFile : MonoBehaviour {
	public Button openBtn;
	public Dropdown dropDown;
	public Track trackScript;
	public int MidiNotesPianoKeyDifference = 8;
	protected string[] filePath;
	protected ExtensionFilter[] extensions;
	protected List<Dropdown.OptionData> dropDownOptions = new List<Dropdown.OptionData>();
	protected int tracks; 
	protected MidiFile midi;
	protected List<NoteEvent> finalNotes = new List<NoteEvent> ();
	protected List<NoteEvent> simulatinousNotes = new List<NoteEvent>();
	protected List<MidiEvent> combinedNotes = new List<MidiEvent>();

	void Start () {
		openBtn.onClick.AddListener (OpenMidi);
		extensions = new ExtensionFilter[] { new ExtensionFilter ("Midi files", "mid", "midi") };
		dropDown.interactable = false;
		dropDown.onValueChanged.AddListener (ReadMidi);
	}

    NoteEvent CombineNotes(List<NoteEvent> notes){ // convert multiple notes to one by adding up frequencies //experimental and not used
        float totalFreq = 0;
        int key = 0;
        foreach (var ne in notes)
        {
            totalFreq += Mathf.Pow(2, ne.NoteNumber / 12.0f); 
        }
        key = Mathf.RoundToInt(Mathf.Clamp((12*Mathf.Log(totalFreq, 2)),0,127));
        notes[0].NoteNumber = key;
        return notes[0];
    }

	void OpenMidi(){
		filePath = StandaloneFileBrowser.OpenFilePanel ("Open midi file", "", extensions , false);
		if (filePath[0].Length == 0) {
			return;
		}

		string trimmedPath = filePath [0].Substring (5, filePath [0].Length - 5); //takes away the File:// at the start
		trimmedPath = trimmedPath.Replace ("%20", " "); //replaces the %20 with spaces
		try {
			midi = new MidiFile (trimmedPath); //try because some midi files might give an error.
		} catch (System.Exception ex) {
			return;
		}

		//This gets the ammount of tracks on the midi and enables the dropdown so user can select one of the tracks
		tracks = midi.Events.Tracks; //ammount of tracks 
		dropDown.options.Clear();
		for (int i = 0; i < tracks; i++) {
			dropDownOptions.Add(new Dropdown.OptionData("Track " + i));
		}
		dropDownOptions.Add(new Dropdown.OptionData("Combine tracks"));
		dropDown.options = dropDownOptions;
		dropDown.value = 0;
		dropDown.interactable = true;
		ReadMidi (0);

	}

	public void ReadMidi(int trackNr) {
		trackScript.Start ();
		finalNotes.Clear ();
		simulatinousNotes.Clear ();
		combinedNotes.Clear ();

		List<MidiEvent> midiEvents = new List<MidiEvent> ();

		if (trackNr == dropDown.options.Count-1) { //this means combine is slected
			for (int i = 0; i < tracks; i++) {
				foreach (MidiEvent note in midi.Events[i]) {
					combinedNotes.Add(note);
				}
			}
			midiEvents = combinedNotes.OrderByDescending (x => x.AbsoluteTime).Reverse ().ToList();
		} else {
			midiEvents = midi.Events [trackNr].ToList ();
		}


		foreach (MidiEvent note in midiEvents) //reads note data from track selected on dropdown
		{
			if (note.CommandCode == MidiCommandCode.NoteOn) {
				NoteEvent ne = (NoteEvent)note;
				if (simulatinousNotes.Count > 0) { //when multiple notes are played simulatinously only take the one with the highest freqency.
					if (simulatinousNotes[simulatinousNotes.Count-1].AbsoluteTime == ne.AbsoluteTime) {
						simulatinousNotes.Add (ne);
					} else {
                        finalNotes.Add(simulatinousNotes.OrderByDescending(x => x.NoteNumber).First());
                        //finalNotes.Add(CombineNotes(simulatinousNotes));
						simulatinousNotes.Clear ();
					}
				}
				simulatinousNotes.Add (ne);
			}
		}

		foreach (var note in finalNotes) {
			trackScript.AddToSong ((byte)(note.NoteNumber - MidiNotesPianoKeyDifference)); //prints out the notes
		}
	}
	
}
