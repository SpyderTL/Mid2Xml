using System;
using System.Collections.Generic;
using System.Linq;

namespace Mid2Xml
{
	internal class XmlFile
	{
		internal static void Save(string output)
		{
			using (var writer = System.Xml.XmlWriter.Create(output, new System.Xml.XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t"
			}))
			{
				writer.WriteStartElement("midi");

				writer.WriteAttributeString("ticksPerBeat", MidiSong.TicksPerBeat.ToString());

				foreach (var track in MidiSong.Tracks)
				{
					writer.WriteStartElement("track");

					foreach (var event2 in track.Events)
					{
						writer.WriteStartElement("event");

						writer.WriteAttributeString("delay", event2.Delay.ToString());
						writer.WriteAttributeString("type", event2.Type.ToString());

						if(event2.Channel.HasValue)
							writer.WriteAttributeString("channel", event2.Channel.Value.ToString());

						if (event2.Value.HasValue)
							writer.WriteAttributeString("value", event2.Value.Value.ToString());

						if (event2.Value2.HasValue)
							writer.WriteAttributeString("value2", event2.Value2.Value.ToString());

						writer.WriteEndElement();
					}

					writer.WriteEndElement();
				}

				writer.WriteEndElement();

				writer.Flush();
			}
		}

		internal static void Load(string input)
		{
			using (var reader = System.Xml.XmlReader.Create(input))
			{
				var tracks = new List<MidiSong.Track>();
				var events = new List<MidiSong.Event>();

				while (reader.Read())
				{
					switch (reader.NodeType)
					{
						case System.Xml.XmlNodeType.Element:
							switch (reader.Name)
							{
								case "midi":
									MidiSong.TicksPerBeat = uint.Parse(reader.GetAttribute("ticksPerBeat"));
									break;

								case "track":
									events.Clear();
									break;

								case "event":
									var event2 = new MidiSong.Event();

									if (uint.TryParse(reader["channel"], out uint channel))
										event2.Channel = channel;

									if (uint.TryParse(reader["delay"], out uint delay))
										event2.Delay = delay;

									if (Enum.TryParse(reader["type"], out MidiSong.EventType type))
										event2.Type = type;

									if (uint.TryParse(reader["value"], out uint value))
										event2.Value = value;

									if (uint.TryParse(reader["value2"], out uint value2))
										event2.Value2 = value2;

									events.Add(event2);
									break;
							}
							break;

						case System.Xml.XmlNodeType.EndElement:
							switch (reader.Name)
							{
								case "midi":
									MidiSong.Tracks = tracks.ToArray();
									break;

								case "track":
									tracks.Add(new MidiSong.Track { Events = events.ToArray() });
									break;
							}
							break;
					}
				}

				MidiSong.Tracks = tracks.ToArray();
			}
		}
	}
}