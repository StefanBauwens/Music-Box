using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;// required for dealing with audiomixers

[RequireComponent(typeof(AudioSource))]
public class MicroPhoneListener : MonoBehaviour
{
    const int CENTSACCURACY = 45;
    const float MINVOLUMEPEAK = 0.1f;
    const int LOWESTFREQUENCY= 60;
    const int HIGHESTFREQUENCY = 460;
    const int MICRONUMBER = 0; //index in the microphones array connected to the laptop

    private float[] noteFrequencies = new float[88]; //containing the frequencies of the equal tempered scale (12 semitones f2=f1*POW(2;1/12))
    private float[] noteFrequenciesMax = new float[88];//containing upper frequencies considering the accuracy in cents
    private float[] noteFrequenciesMin = new float[88];//containing lower frequencies considering the accuracy in cents
    private string[] noteNamesSharp =//from A0=27.5 Hz to  C8=4186.009 Hz => 88 notes
        {"A0","A#0","B0",
         "C1","C#1","D1","D#1","E1","F1","F#1","G1","G#1","A1","A#1","B1",
         "C2","C#2","D2","D#2","E2","F2","F#2","G2","G#2","A2","A#2","B2",
         "C3","C#3","D3","D#3","E3","F3","F#3","G3","G#3","A3","A#3","B3",
         "C4","C#4","D4","D#4","E4","F4","F#4","G4","G#4","A4","A#4","B4",
         "C5","C#5","D5","D#5","E5","F5","F#5","G5","G#5","A5","A#5","B5",
         "C6","C#6","D6","D#6","E6","F6","F#6","G6","G#6","A6","A#6","B6",
         "C7","C#7","D7","D#7","E7","F7","F#7","G7","G#7","A7","A#7","B7",
         "C8"
    };
    private string[] noteNamesFlat =//from A0=27.5 Hz to  C8=4186.009 Hz => 88 notes
        {"A0","Bf0","B0",
         "C1","Df1","D1","Ef1","E1","F1","Gf1","G1","Af1","A1","Bf1","B1",
         "C2","Df2","D2","Ef2","E2","F2","Gf2","G2","Af2","A2","Bf2","B2",
         "C3","Df3","D3","Ef3","E3","F3","Gf3","G3","Af3","A3","Bf3","B3",
         "C4","Df4","D4","Ef4","E4","F4","Gf4","G4","Af4","A4","Bf4","B4",
         "C5","Df5","D5","Ef5","E5","F5","Gf5","G5","Af5","A5","Bf5","B5",
         "C6","Df6","D6","Ef6","E6","F6","Gf6","G6","Af6","A6","Bf6","B6",
         "C7","Df7","D7","Ef7","E7","F7","Gf7","G7","Af7","A7","Bf7","B7",
         "C8"
    };
    //an audio source also attached to the same object as this script is
    AudioSource src;

    //double click the audio mixer and next to the "groups" section, click the "+" icon to add a 
    //child to the master group, rename it to "microphone".  Then in the audio source, in the "output" option, 
    //select this child of the master you have just created.
    //go back to the audiomixer inspector window, and click the "microphone" you just created, then in the 
    //inspector window, right click "Volume" and select "Expose Volume (of Microphone)" to script,
    //then back in the audiomixer window, in the corner click "Exposed Parameters", click on the "MyExposedParameter"
    //and rename it to "Volume"
    private string[] microphones= {"","","","","","","","","","" };
    private string microphone;

    private int minSamplingFreq, maxSamplingFreq;
    private float[] sampleArray;
    private int sampleArrayLength;

    void Start()
    {
        //fill array of the notefrequencies
        noteFrequencies[0] = 27.5f;
        for (int note = 1; note < 88; note++)
        {
            noteFrequencies[note] = noteFrequencies[note - 1] * Mathf.Pow(2.0f, 1.0f / 12.0f);
        }

        //fill upper limit accuray note frequency table using centsAccuracy
        for (int note = 0; note < 88; note++)
        {
            noteFrequenciesMax[note] = noteFrequencies[note] * Mathf.Pow(2.0f, CENTSACCURACY / 1200.0f);
        }

        //fill lower limit accuray note frequency table using centsAccuracy
        for (int note = 0; note < 88; note++)
        {
            noteFrequenciesMin[note] = noteFrequencies[note] / Mathf.Pow(2.0f, CENTSACCURACY / 1200.0f);
        }

        Debug.Log("min " + noteFrequenciesMin[36] + " freq " + noteFrequencies[36] + " max " + noteFrequenciesMax[36]);

        int index = 0;
        foreach (string device in Microphone.devices)
        {
            //Debug.Log(device);
            microphones[index]= device;
            index++;
            if (index == 10)
            {
                Debug.Log("Max. 1O input devices...");
                break;
            }
        }
        foreach (string device  in microphones)
        {
            if (device != "")
            {
                
                Microphone.GetDeviceCaps(device, out minSamplingFreq, out maxSamplingFreq);
                Debug.Log(device + " minSamplingFreq: " + minSamplingFreq + " maxSamplingFreq: " + maxSamplingFreq);
            }
        }
        //choose microphone
        microphone = microphones[MICRONUMBER];
        Microphone.GetDeviceCaps(microphone, out minSamplingFreq, out maxSamplingFreq);
        Debug.Log("Frequency: " + microphone + " = "+ maxSamplingFreq);
        //maxFreq is maximum sampling frequency: lengthOfLineRenderer and sampleArray have maxFreq/MINFREQ*2 samples 48000/60*2 = 1600 samples
        sampleArrayLength = maxSamplingFreq / LOWESTFREQUENCY*2;
        sampleArray = new float[sampleArrayLength]; 
        Debug.Log("number of samples = " + sampleArrayLength);
        src = GetComponent<AudioSource>();
        //remove any soundfile in the audiosource
        src.clip = null;
        //start recording from microphone in loop, 1 second is minimum, use maxFreq from selected microphone
        src.clip = Microphone.Start(microphone, true, 1, maxSamplingFreq);
    }

    void Update()
    {
        
        float maxValue;
        float baseFreq = 0.0f;
        string noteName = "";
        //src.timeSamples = 0;
        src.clip.GetData(sampleArray, maxSamplingFreq / 2);//put the samples from the half of the recording of 1 second in the array (microphone stabilised)
        //search max value in the sampleArray
        maxValue = maxValueSampleArray();
        if (maxValue > MINVOLUMEPEAK) //check if volume is acceptable and show samples
        {
            baseFreq=baseFrequency(sampleArray,sampleArrayLength, LOWESTFREQUENCY, HIGHESTFREQUENCY, maxSamplingFreq);
            noteName = frequency2NoteName(baseFreq, true);
            Debug.Log(noteName);
        }
    }

/*fill the array containing the differences between two consecutive periods over the frequency range to check
* sampleArray[] array containing floats between -1.0 and +1.0 at least containing a value > MINVOLUMEPEAK
* Input variables:
* maxSamplingFreq: depends on microphone capacity (48000)
* LOWESTFREQUENCY: lowest frequency to detect as baseFrequency (60) => minimum two periods required in the sound clip from the microphone => dictates max number of samples to compare
* HIGHESTFREQUENCY: highest frequency to detect as baseFrequency (460) => is less accurate than lowest frequency => dictates min number of samples to compare
* 
* Calculated variables:
* sampleArrayLength = maxSamplingFreq / LOWESTFREQUENCY *2 (48000/60*2=1600)
* minNumberOfSamples = maxSamplingFreq / HIGHESTFREQUENCY (48000/460 = 104) (= 1 period 461.5 Hz is maximum freq detected)
* maxNumberOfSamples = maxSamplingFreq / LOWESTFREQUENCY; // 48000/60=800 ( = 1 period 60 Hz is minimum freq detected
* delta array number of elements = maxNumberOfSamples-minNumberOfSamples+1 (800-104+1=697 elements)
* delta[0] contains minNumberOfSamples difference (104 samples => 1/((1/48000)*104)=461.5 Hz)
* delta[1] contains (minNumberOfSamples + 1) difference (105 samples => 1/((1/48000)*105)=457.1Hz)
* ...
* delta[maxNumberOfSamples-minNumberOfSamples] (delta[696] => (104+696)samples = 60 Hz)
* baseFrequency = 1/((index + minNumberOfSamples)/maxSamplingFreq); 
    */
    float baseFrequency(float[] sampleArray,int sampleArrayLength, int LOWESTFREQUENCY, int HIGHESTFREQUENCY, int samplingFreq )
    {
        float baseFreq = 0.0f;
        int minNumberOfSamples = samplingFreq / HIGHESTFREQUENCY; // 48000/460 = 104 (461.5 Hz is maximum freq detected)
        int maxNumberOfSamples = sampleArrayLength / 2; // 1600/2=800
        float[] delta = new float[maxNumberOfSamples - minNumberOfSamples + 1]; // contains 800-104+1 = 697 elements
        int deltaIndex;
        float dif;
        float sum;
        for (deltaIndex = 0; deltaIndex <= (maxNumberOfSamples - minNumberOfSamples); deltaIndex++) // testnumber from 0 to 696 for 697 elements in delta array
        {
            sum = 0.0f;
            for (int sampleArrayIndex = 0; sampleArrayIndex < (deltaIndex + minNumberOfSamples); sampleArrayIndex++) // sampleArrayIndex from 0 to 799
            {                                                // 0->696 + 104 = 104-> 800 index 103-> 799
                dif = sampleArray[sampleArrayIndex] - sampleArray[sampleArrayIndex + deltaIndex + minNumberOfSamples]; //index1 = max 696+104 + 696+104
                //                                                  0->799 + 0->696 + 104 index 104->1599
                if (dif < 0) dif = -dif;
                sum += dif;
            }
            delta[deltaIndex] = sum / (deltaIndex + minNumberOfSamples);
        }
        /* Search maximum delta and take a percentage as deltaThreshold
           Find first index where delta is lower than deltaThreshold: indexlow
           Then find second index where delta is higher than Threshold: indexhigh
           Then find minimum between indexlow and indexhigh and return that index
        */
        const int PERCENT = 15;
        float maximum = 0;
        float minimum = float.MaxValue;
        int index, indexminimum=0, indexLow=0, indexHigh=0;
        for (index = 0; index < (maxNumberOfSamples - minNumberOfSamples); index++)
        {
            if (delta[index] > maximum)
            {
                maximum = delta[index];
            }
        }
        maximum = maximum * PERCENT / 100;
        //find first index where delta is lower than deltaThreshold

        for (index = 0; index < (maxNumberOfSamples - minNumberOfSamples); index++)
        {
            if (delta[index] < maximum)
            {
                indexLow = index; //first index where delta is lower than threshold
                break;
            }
        }
        index++;
        while (index < (maxNumberOfSamples - minNumberOfSamples))
        {
            if (delta[index] > maximum)
            {
                indexHigh = index; //first index where delta is again higher than threshold
                break;
            }
            index++;
        }
        for (index = indexLow; index <= indexHigh; index++)
        {
            if (delta[index] < minimum)
            {
                minimum = delta[index];
                indexminimum = index;
            }
        }
        //Debug.Log("indexminimum = " + indexminimum);
        //CubeSoundHeight.localScale= new Vector3(50.0f, 30.0f+ indexminimum , 50.0f); //indexminimum = 0 to 714
        //CubeSoundHeight.localPosition = new Vector3(415.0f, -300.0f + indexminimum/2, -242.0f);
        //baseFreq = 1.0f / ((index + minNumberOfSamples) / (float)maxSamplingFreq);
        baseFreq = (float)maxSamplingFreq / (float)(indexminimum + minNumberOfSamples);
        Debug.Log("baseFreq = " + baseFreq);
        return baseFreq;
    }

    /* return a notestring format A#1 'notenameletter' 'sharp # or flat f' 'octavenumber' 
    * accuray is in cents 100 cents is a semitone: noteFrequenciesMin[] and noteFrequenciesMax[] are filled with that accuracy
    */
    string frequency2NoteName(float freq, bool sharps)
    {
        string noteName="false";
        //find the closest value higher than the frequency in the notefrequencyMintable
        int index = 0;
        while(freq >= noteFrequenciesMin[index])
        {
            index++;
        }
        if(index!=0) index--;
        if (freq < noteFrequenciesMax[index])
        {
            if (sharps)
            { 
            noteName = noteNamesSharp[index];
            }
            else
            {
                noteName = noteNamesFlat[index];
            }
        }
        return noteName;
    }



    float maxValueSampleArray()
    {
        float maxValue = 0.0f;
        for (int i = 0; i < sampleArrayLength; i++)
        {
            if (sampleArray[i] > maxValue)
            {
                maxValue = sampleArray[i];
            }
        }
        return maxValue;
    }

    private void OnApplicationQuit()
    {
        src.Stop();
        src.clip = null;
        Microphone.End(microphone);
    }
}
