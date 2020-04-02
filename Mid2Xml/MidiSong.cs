using System;
using System.Collections.Generic;
using System.Text;

namespace Mid2Xml
{
	internal static class MidiSong
	{
		internal static uint TicksPerBeat;
		internal static Track[] Tracks;

		internal struct Track
		{
			internal Event[] Events;
		}

		internal class Event
		{
			internal uint Delay;
			internal EventType Type;
			internal uint? Channel;
			internal uint? Value;
			internal uint? Value2;
			internal byte[] Data;
		}

		internal enum EventType
		{
			NoteOn,
			NoteOff,
			ProgramChange,
			ControlChange,
			PitchBend,
			SetTempo,
			Delay,
			ChannelPressure,
			KeyPressure
		}
	}
}
