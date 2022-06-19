﻿using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
namespace RageCoop.Core
{
    public partial class Packets
    {

        public class CustomEvent : Packet
        {
            public int Hash { get; set; }
            public List<object> Args { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                message.Write((byte)PacketTypes.CustomEvent);

                List<byte> result = new List<byte>();
                result.AddInt(Hash);
                result.AddInt(Args.Count);
                (byte, byte[]) tup;
                foreach (var arg in Args)
                {
                    tup=CoreUtils.GetBytesFromObject(arg);
                    if (tup.Item2==null)
                    {
                        throw new ArgumentException($"Object of type {arg.GetType()} is not supported");
                    }
                    result.Add(tup.Item1);
                    result.AddRange(tup.Item2);
                }

                message.Write(result.Count);
                message.Write(result.ToArray());
            }

            public override void Unpack(byte[] array)
            {
                BitReader reader = new BitReader(array);

                Hash = reader.ReadInt();
                var len=reader.ReadInt();
                for (int i = 0; i < len; i++)
                {
                    byte argType = reader.ReadByte();
                    switch (argType)
                    {
                        case 0x01:
                            Args.Add(reader.ReadByte());
                            break;
                        case 0x02:
                            Args.Add(reader.ReadShort());
                            break;
                        case 0x03:
                            Args.Add(reader.ReadUShort());
                            break;
                        case 0x04:
                            Args.Add(reader.ReadInt());
                            break;
                        case 0x05:
                            Args.Add(reader.ReadUInt());
                            break;
                        case 0x06:
                            Args.Add(reader.ReadLong());
                            break;
                        case 0x07:
                            Args.Add(reader.ReadULong());
                            break;
                        case 0x08:
                            Args.Add(reader.ReadFloat());
                            break;
                        case 0x09:
                            Args.Add(reader.ReadBool());
                            break;
                        case 0x10:
                            Args.Add(reader.ReadString());
                            break;
                    }
                }
            }
        }
    }
}