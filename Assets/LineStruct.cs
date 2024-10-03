using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Line
{
    //add some kind of Audio clip variable


    public string actor;
    public string verse;
    public AudioClip lineAudio;

    public Line(string ac, string ver)
    {
        this.actor = ac;
        this.verse = ver;
        this.lineAudio = null;
    }
}
