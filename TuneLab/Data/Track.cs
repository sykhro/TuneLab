﻿using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuneLab.Audio;
using TuneLab.Base.Data;
using TuneLab.Base.Structures;
using TuneLab.Base.Science;
using TuneLab.Extensions.Formats.DataInfo;
using TuneLab.Extensions.Voices;
using NAudio.CoreAudioApi;
using TuneLab.Utils;
using TuneLab.Base.Utils;

namespace TuneLab.Data;

internal class Track : DataObject, ITrack
{
    public IProject Project => mProject;
    public ITempoManager TempoManager => mProject.TempoManager;
    public ITimeSignatureManager TimeSignatureManager => mProject.TimeSignatureManager;
    public DataProperty<string> Name { get; }
    public DataProperty<bool> IsMute { get; }
    public DataProperty<bool> IsSolo { get; }
    public DataProperty<double> Gain { get; }
    public DataProperty<double> Pan { get; }
    public IReadOnlyDataObjectLinkedList<IPart> Parts => mParts;

    IDataProperty<string> ITrack.Name => Name;

    IDataProperty<bool> ITrack.IsMute => IsMute;

    IDataProperty<bool> ITrack.IsSolo => IsSolo;

    IDataProperty<double> ITrack.Gain => Gain;
    IDataProperty<double> ITrack.Pan => Pan;

    public Track(IProject project, TrackInfo info)
    {
        mProject = project;
        Name = new DataString(this, string.Empty);
        IsMute = new DataStruct<bool>(this);
        IsSolo = new DataStruct<bool>(this);
        Gain = new DataStruct<double>(this);
        Pan = new DataStruct<double>(this);
        mParts = new();
        mParts.Attach(this);
        IDataObject<TrackInfo>.SetInfo(this, info);
    }

    public void InsertPart(IPart part)
    {
        mParts.Insert(part);
    }

    public bool RemovePart(IPart part)
    {
        return mParts.Remove(part);
    }

    public MidiPart CreatePart(MidiPartInfo info)
    {
        return new MidiPart(TempoManager, TimeSignatureManager, info);
    }

    public AudioPart CreatePart(AudioPartInfo info)
    {
        return new AudioPart(TempoManager, TimeSignatureManager, info);
    }

    public Part CreatePart(PartInfo info)
    {
        if (info is MidiPartInfo midiPartInfo)
            return CreatePart(midiPartInfo);
        else if (info is AudioPartInfo audioPartInfo)
            return CreatePart(audioPartInfo);

        throw new ArgumentException();
    }

    public void ReSegment()
    {
        foreach (var part in mParts)
        {
            if (part is MidiPart midiPart)
                midiPart.ReSegment();
        }
    }

    public TrackInfo GetInfo()
    {
        return new()
        {
            Name = Name,
            Mute = IsMute,
            Solo = IsSolo,
            Gain = Gain,
            Pan = Pan,
            Parts = mParts.GetInfo().ToInfo()
        };
    }

    void IDataObject<TrackInfo>.SetInfo(TrackInfo info)
    {
        IDataObject<TrackInfo>.SetInfo(Name, info.Name);
        IDataObject<TrackInfo>.SetInfo(IsMute, info.Mute);
        IDataObject<TrackInfo>.SetInfo(IsSolo, info.Solo);
        IDataObject<TrackInfo>.SetInfo(Gain, info.Gain);
        IDataObject<TrackInfo>.SetInfo(Pan, info.Pan);
        IDataObject<TrackInfo>.SetInfo(mParts, info.Parts.Convert(CreatePart).ToArray());
    }

    class PartList : DataObjectLinkedList<IPart>
    {
        protected override bool IsInOrder(IPart prev, IPart next)
        {
            if (prev.StartPos() < next.StartPos())
                return true;

            if (prev.StartPos() > next.StartPos())
                return false;

            if (prev.EndPos() > next.EndPos())
                return true;

            if (prev.EndPos() < next.EndPos())
                return false;

            return true;
        }
    }

    readonly IProject mProject;
    readonly PartList mParts;
}
