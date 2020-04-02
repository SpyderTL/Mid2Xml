using System;
using System.Collections.Generic;

namespace Mid2Xml
{
	internal static class MidFile
	{
		internal static void Load(string input)
		{
			using (var stream = System.IO.File.OpenRead(input))
			using (var reader = new System.IO.BinaryReader(stream))
			{
				ReadHeader(reader);

				for (var track = 0; track < MidiSong.Tracks.Length; track++)
					ReadTrack(reader, track);
			}
		}

		internal static void Save(string output)
		{
			using (var writer = new System.IO.BinaryWriter(System.IO.File.Create(output)))
			{
				// Header
				writer.Write("MThd".ToCharArray());
				writer.WriteBigEndian(6U);
				writer.WriteBigEndian(1);
				writer.WriteBigEndian((ushort)MidiSong.Tracks.Length);
				writer.WriteBigEndian((ushort)MidiSong.TicksPerBeat);

				foreach (var track in MidiSong.Tracks)
				{
					writer.Write("MTrk".ToCharArray());
					var length = writer.BaseStream.Position;
					writer.Write(0);

					var start = writer.BaseStream.Position;

					var last = 0u;

					foreach (var event2 in track.Events)
					{
						switch (event2.Type)
						{
							case MidiSong.EventType.NoteOn:
								writer.WriteQuantity(event2.Delay);
								if (last != (0x90 | event2.Channel.Value))
									writer.Write((byte)(0x90 | event2.Channel.Value));
								writer.Write((byte)event2.Value.Value);
								writer.Write((byte)event2.Value2.Value);

								last = (0x90 | event2.Channel.Value);
								break;

							case MidiSong.EventType.NoteOff:
								writer.WriteQuantity(event2.Delay);
								if (last != (0x80 | event2.Channel.Value))
									writer.Write((byte)(0x80 | event2.Channel.Value));
								writer.Write((byte)event2.Value.Value);
								writer.Write((byte)event2.Value2.Value);

								last = (0x80 | event2.Channel.Value);
								break;

							case MidiSong.EventType.ProgramChange:
								writer.WriteQuantity(event2.Delay);
								if (last != (0xc0 | event2.Channel.Value))
									writer.Write((byte)(0xc0 | event2.Channel.Value));
								writer.Write((byte)event2.Value.Value);
								last = (0xc0 | event2.Channel.Value);
								break;

							case MidiSong.EventType.ControlChange:
								writer.WriteQuantity(event2.Delay);
								if (last != (0xb0 | event2.Channel.Value))
									writer.Write((byte)(0xb0 | event2.Channel.Value));
								writer.Write((byte)event2.Value.Value);
								writer.Write((byte)event2.Value2.Value);
								last = (0xb0 | event2.Channel.Value);
								break;

							case MidiSong.EventType.PitchBend:
								writer.WriteQuantity(event2.Delay);
								if (last != (0xe0 | event2.Channel.Value))
									writer.Write((byte)(0xe0 | event2.Channel.Value));
								writer.Write((ushort)event2.Value.Value);
								last = (0xe0 | event2.Channel.Value);
								break;

							case MidiSong.EventType.SetTempo:
								writer.WriteQuantity(event2.Delay);
								writer.Write((byte)0xff);
								writer.Write((byte)0x51);
								writer.WriteQuantity(3);
								writer.Write((byte)(event2.Value.Value >> 16));
								writer.Write((byte)((event2.Value.Value >> 8) & 0xff));
								writer.Write((byte)(event2.Value.Value & 0xff));
								last = 0xff;
								break;

							default:
								writer.WriteQuantity(event2.Delay);
								writer.Write((byte)0xff);
								writer.Write((byte)0x06);
								writer.WriteQuantity(0);
								last = 0xff;
								break;
						}
					}

					writer.WriteQuantity(0);
					writer.Write((byte)0xff);
					writer.Write((byte)0x2f);
					writer.WriteQuantity(0);

					var end = writer.BaseStream.Position;

					writer.BaseStream.Position = length;

					writer.WriteBigEndian((uint)(end - start));

					writer.BaseStream.Position = end;
				}

				writer.Flush();
			}
		}

		internal static void WriteBigEndian(this System.IO.BinaryWriter writer, uint value)
		{
			writer.Write((byte)(value >> 24));
			writer.Write((byte)((value >> 16) & 0xFF));
			writer.Write((byte)((value >> 8) & 0xFF));
			writer.Write((byte)(value & 0xFF));
		}

		internal static void WriteBigEndian(this System.IO.BinaryWriter writer, ushort value)
		{
			writer.Write((byte)((value >> 8) & 0xFF));
			writer.Write((byte)(value & 0xFF));
		}

		internal static void WriteQuantity(this System.IO.BinaryWriter writer, uint value)
		{
			if (value < 0x80)
			{
				writer.Write((byte)(value & 0x7F));
			}
			else if (value < 0x4000)
			{
				writer.Write((byte)(((value >> 7) & 0x7F) | 0x80));
				writer.Write((byte)(value & 0x7F));
			}
			else if (value < 0x200000)
			{
				writer.Write((byte)(((value >> 14) & 0x7F) | 0x80));
				writer.Write((byte)(((value >> 7) & 0x7F) | 0x80));
				writer.Write((byte)(value & 0x7F));
			}
			else
			{
				System.Diagnostics.Debugger.Break();
			}
		}

		private static void ReadHeader(System.IO.BinaryReader reader)
		{
			var signature = reader.ReadChars(4);
			var length = reader.ReadBigEndianUInt32();

			var format = reader.ReadBigEndianUInt16();
			var tracks = reader.ReadBigEndianUInt16();
			var ticks = reader.ReadBigEndianUInt16();

			MidiSong.Tracks = new MidiSong.Track[tracks];
			MidiSong.TicksPerBeat = ticks;
		}

		private static void ReadTrack(System.IO.BinaryReader reader, int track)
		{
			var signature = reader.ReadChars(4);
			var length = reader.ReadBigEndianUInt32();

			var start = reader.BaseStream.Position;

			var events = new List<MidiSong.Event>();
			var lastStatus = (byte)0;

			while (true)
			{
				var delay = reader.ReadQuantity();
				var status = reader.ReadByte();

				if ((status & 0x80) == 0)
				{
					status = lastStatus;
					reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
				}

				switch (status)
				{
					case 0xFF:
						ReadMetaEvent(reader, track, delay, events);
						break;

					case 0xF0:
						ReadSystemExclusiveEvent(reader, track);
						break;

					default:
						var e = ReadTrackEvent(reader, track, status);
						e.Delay = delay;

						events.Add(e);
						break;
				}

				lastStatus = status;

				if (reader.BaseStream.Position == start + length)
					break;
			}

			MidiSong.Tracks[track].Events = events.ToArray();
		}

		private static MidiSong.Event ReadTrackEvent(System.IO.BinaryReader reader, int track, byte status)
		{
			var messageType = status >> 4;
			var channel = (byte)(status & 0xf);

			switch (messageType)
			{
				case 0x8:
					return new MidiSong.Event { Type = MidiSong.EventType.NoteOff, Channel = channel, Value = reader.ReadByte(), Value2 = reader.ReadByte() };

				case 0x9:
					return new MidiSong.Event { Type = MidiSong.EventType.NoteOn, Channel = channel, Value = reader.ReadByte(), Value2 = reader.ReadByte() };

				case 0xa:
					return new MidiSong.Event { Type = MidiSong.EventType.KeyPressure, Channel = channel, Value = reader.ReadByte(), Value2 = reader.ReadByte() };

				case 0xb:
					var value1 = reader.ReadByte();
					var value2 = reader.ReadByte();

					switch (value1)
					{
						case 0x78:
							//return "All Sound Off";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x79:
							//return "Reset All Controllers";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7a:
							//return "Local Control";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7b:
							//return "All Notes Off";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7c:
							//return "Omni Mode Off";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7d:
							//return "Omni Mode On";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7e:
							//return "Mono Mode On";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						case 0x7f:
							//return "Poly Mode On";
							return new MidiSong.Event { Type = MidiSong.EventType.Delay, Channel = channel };

						default:
							return new MidiSong.Event { Type = MidiSong.EventType.ControlChange, Channel = channel, Value = value1, Value2 = value2 };
					}

				case 0xc:
					return new MidiSong.Event { Type = MidiSong.EventType.ProgramChange, Channel = channel, Value = reader.ReadByte() };
				//return new ProgramChange { Delay = delay, Channel = channel, Patch = reader.ReadByte(), Unknown = 0 };

				case 0xd:
					return new MidiSong.Event { Type = MidiSong.EventType.ChannelPressure, Channel = channel, Value = reader.ReadByte() };
				//return new ChannelPressure { Delay = delay, Channel = channel, Velocity = reader.ReadByte(), Unknown = 0 };

				case 0xe:
					return new MidiSong.Event { Type = MidiSong.EventType.PitchBend, Channel = channel, Value = reader.ReadByte() | (uint)(reader.ReadByte() << 8) };
				//return new PitchBendChange { Delay = delay, Channel = channel, Value = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray(), 0) };

				default:
					throw new NotSupportedException();
					//value1 = reader.ReadByte();
					//value2 = reader.ReadByte();
					//return "Unknown";
			}

			throw new NotSupportedException();
		}

		private static void ReadMetaEvent(System.IO.BinaryReader reader, int track, uint delay, List<MidiSong.Event> events)
		{
			var type = reader.ReadByte();
			var length = (int)ReadQuantity(reader);

			switch (type)
			{
				case 0x00:
					reader.ReadBytes(length);
					//return "Sequence Number";

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });
					break;

				case 0x01:
					var text = new string(reader.ReadChars(length));
					//reader.ReadBytes(length);
					//return "Text Event: " + text;

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });
					break;

				case 0x02:
					var copyright = new string(reader.ReadChars(length));
					//reader.ReadBytes(length);
					//return "Copyright Notice: " + copyright;

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });
					break;

				case 0x03:
					var name = new string(reader.ReadChars(length));
					//return "Sequence/Track Name: " + name;

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });
					break;

				case 0x04:
					var instrument = new string(reader.ReadChars(length));
					//reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Instrument Name: " + instrument;
					break;

				case 0x05:
					var lyric = new string(reader.ReadChars(length));

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Lyric: " + lyric;
					break;

				case 0x06:
					reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Marker";
					break;

				case 0x07:
					reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Cue Point";
					break;

				case 0x20:
					reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "MIDI Channel Prefix";
					break;

				case 0x21:
					var data = reader.ReadBytes(length);
					var port = data[0];

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "MIDI Port Prefix:" + port;
					break;

				case 0x2F:
					reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "End Of Track";
					break;

				case 0x51:
					data = reader.ReadBytes(length);
					var tempo = (uint)(data[0] << 16) | (uint)(data[1] << 8) | data[2];

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.SetTempo, Value = tempo });

					//return "Set Tempo: " + tempo;
					break;

				case 0x54:
					data = reader.ReadBytes(length);

					var hours = data[0] & 0x1f;
					var minutes = data[1];
					var seconds = data[2];
					var frames = data[3];
					var subframes = data[4];

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//events.Add(new MidiFile.Event { Delay = delay, Type = MidiFile.EventType.Delay, Value = seconds * 1000 });
					//return "SMTPE Offset";
					break;

				case 0x58:
					data = reader.ReadBytes(length);

					var numerator = data[0];
					var denominator = data[1];
					var midiClocksPerTick = data[2];
					var thirtySecondNotes = data[3];

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Time Signature: " + numerator + "/" + denominator + " (" + midiClocksPerTick + ") [" + thirtySecondNotes + "]";
					break;

				case 0x59:
					data = reader.ReadBytes(length);

					var key = data[0];
					var minor = data[1];

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Key Signature";
					break;

				case 0x7f:
					data = reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					//return "Sequencer Event";
					break;

				default:
					data = reader.ReadBytes(length);

					events.Add(new MidiSong.Event { Delay = delay, Type = MidiSong.EventType.Delay });

					break;
			}
		}

		private static void ReadSystemExclusiveEvent(System.IO.BinaryReader reader, int track)
		{
			var length = (int)ReadQuantity(reader);

			reader.ReadBytes(length);
		}

		private static uint ReadBigEndianUInt32(this System.IO.BinaryReader reader)
		{
			var data = reader.ReadBytes(4);

			return (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];
		}

		private static uint ReadBigEndianUInt16(this System.IO.BinaryReader reader)
		{
			var data = reader.ReadBytes(2);

			return (uint)data[0] << 8 | data[1];
		}

		private static uint ReadQuantity(this System.IO.BinaryReader reader)
		{
			var quantity = 0U;

			while (true)
			{
				var data = reader.ReadByte();

				var value = (uint)data & 0x7f;

				quantity <<= 7;
				quantity |= value;

				if ((data & 0x80) == 0)
					break;
			}

			return quantity;
		}
	}
}