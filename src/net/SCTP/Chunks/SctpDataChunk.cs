﻿//-----------------------------------------------------------------------------
// Filename: SctpDataChunk.cs
//
// Description: Represents the SCTP DATA chunk.
//
// Remarks:
// Defined in section 3 of RFC4960:
// https://tools.ietf.org/html/rfc4960#section-3.3.1.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
// 
// History:
// 18 Mar 2021	Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using SIPSorcery.Sys;

namespace SIPSorcery.Net
{
    public class SctpDataChunk : SctpChunk
    {
        /// <summary>
        /// The length in bytes of the fixed parameters used by the DATA chunk.
        /// </summary>
        public const int FIXED_PARAMETERS_LENGTH = 12;

        /// <summary>
        /// The (U)nordered bit, if set to true, indicates that this is an
        /// unordered DATA chunk.
        /// </summary>
        public bool Unordered { get; set; } = true;

        /// <summary>
        /// The (B)eginning fragment bit, if set, indicates the first fragment
        /// of a user message.
        /// </summary>
        public bool Begining { get; set; } = true;

        /// <summary>
        /// The (E)nding fragment bit, if set, indicates the last fragment of
        /// a user message.
        /// </summary>
        public bool Ending { get; set; } = true;

        /// <summary>
        /// This value represents the Transmission Sequence Number (TSN) for
        /// this DATA chunk.
        /// </summary>
        public uint TSN;

        /// <summary>
        /// Identifies the stream to which the following user data belongs.
        /// </summary>
        public ushort StreamID;

        /// <summary>
        /// This value represents the Stream Sequence Number of the following
        /// user data within the stream using the <seealso cref="StreamID"/>.
        /// </summary>
        public uint StreamSeqNum;

        /// <summary>
        /// Payload Protocol Identifier (PPID). This value represents an application 
        /// (or upper layer) specified protocol identifier.This value is passed to SCTP 
        /// by its upper layer and sent to its peer.
        /// </summary>
        public uint PPID;

        /// <summary>
        /// This is the payload user data.
        /// </summary>
        public byte[] UserData;

        private SctpDataChunk()
            : base(SctpChunkType.DATA)
        { }

        /// <summary>
        /// Creates a new DATA chunk.
        /// </summary>
        /// <param name="tsn">The Transmission Sequence Number for this chunk.</param>
        /// <param name="data">The data to send.</param>
        public SctpDataChunk(uint tsn, byte[] data) : base(SctpChunkType.DATA)
        {
            ChunkFlags = (byte)(
                (Unordered ? 0x04 : 0x0) +
                (Begining ? 0x02 : 0x0) +
                (Ending ? 0x01 : 0x0));

            TSN = tsn;
            UserData = data;
        }

        /// <summary>
        /// Calculates the un-padded length for DATA chunk.
        /// </summary>
        /// <returns>The un-padded length of the chunk.</returns>
        public override ushort GetChunkLength()
        {
            ushort len = SCTP_CHUNK_HEADER_LENGTH + FIXED_PARAMETERS_LENGTH;
            len += (ushort)(UserData != null ? UserData.Length : 0);
            return len;
        }

        /// <summary>
        /// Serialises an INIT or INIT ACK chunk to a pre-allocated buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write the serialised chunk bytes to. It
        /// must have the required space already allocated.</param>
        /// <param name="posn">The position in the buffer to write to.</param>
        /// <returns>The number of bytes, including padding, written to the buffer.</returns>
        public override ushort WriteTo(byte[] buffer, int posn)
        {
            WriteChunkHeader(buffer, posn);

            // Write fixed parameters.
            int startPosn = posn + SCTP_CHUNK_HEADER_LENGTH;

            NetConvert.ToBuffer(TSN, buffer, startPosn);
            NetConvert.ToBuffer(StreamID, buffer, startPosn + 4);
            NetConvert.ToBuffer(StreamSeqNum, buffer, startPosn + 6);
            NetConvert.ToBuffer(PPID, buffer, startPosn + 8);

            int userDataPosn = startPosn + FIXED_PARAMETERS_LENGTH;

            if (UserData != null)
            {
                Buffer.BlockCopy(UserData, 0, buffer, userDataPosn, UserData.Length);
            }

            return GetChunkPaddedLength();
        }

        /// <summary>
        /// Parses the DATA chunk fields
        /// </summary>
        /// <param name="buffer">The buffer holding the serialised chunk.</param>
        /// <param name="posn">The position to start parsing at.</param>
        public static SctpDataChunk ParseChunk(byte[] buffer, int posn)
        {
            var dataChunk = new SctpDataChunk();
            ushort chunkLen = dataChunk.ParseFirstWord(buffer, posn);

            if (chunkLen < FIXED_PARAMETERS_LENGTH)
            {
                throw new ApplicationException($"SCTP data chunk cannot be parsed as buffer too short for fixed parameter fields.");
            }

            int startPosn = posn + SCTP_CHUNK_HEADER_LENGTH;

            dataChunk.TSN = NetConvert.ParseUInt32(buffer, startPosn);
            dataChunk.StreamID = NetConvert.ParseUInt16(buffer, startPosn + 4);
            dataChunk.StreamSeqNum = NetConvert.ParseUInt16(buffer, startPosn + 6);
            dataChunk.PPID = NetConvert.ParseUInt32(buffer, startPosn + 8);

            int userDataPosn = startPosn + FIXED_PARAMETERS_LENGTH;
            int userDataLen = chunkLen - SCTP_CHUNK_HEADER_LENGTH - FIXED_PARAMETERS_LENGTH;

            if (userDataLen > 0)
            {
                dataChunk.UserData = new byte[userDataLen];
                Buffer.BlockCopy(buffer, userDataPosn, dataChunk.UserData, 0, dataChunk.UserData.Length);
            }

            return dataChunk;
        }
    }
}