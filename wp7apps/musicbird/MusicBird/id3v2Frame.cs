using System;
using System.IO;
using System.Text;

namespace MusicBird
{
    /// <summary>
    /// Piece of ID3v2 reader.  reads frmes one at a time given the proper position in a binary stream
    /// </summary>
    public class id3v2Frame
    {
        public string frameName;
        public byte[] frameContents;
        public ulong frameSize;
        public int MajorVersion;


        public bool F_TagAlterPreservation;
        public bool F_FileAlterPreservation;
        public bool F_ReadOnly;
        public bool F_Compression;
        public bool F_Encryption;
        public bool F_GroupingIdentity;
        public bool F_Unsynchronisation;
        public bool F_DataLengthIndicator;
        public bool padding;


        public id3v2Frame()
        {
            this.padding = false;
            this.frameName = "";
            this.frameSize = 0;
            this.MajorVersion = 0;

            this.F_TagAlterPreservation = false;
            this.F_FileAlterPreservation = false;
            this.F_ReadOnly = false;
            this.F_Compression = false;
            this.F_Encryption = false;
            this.F_GroupingIdentity = false;
            this.F_Unsynchronisation = false;
            this.F_DataLengthIndicator = false;


        }

        public id3v2Frame ReadFrame( BinaryReader br, int version )
        {
            char[] tagSize;    // I use this to read the bytes in from the file
            int[] bytes;      // for bit shifting
            ulong newSize = 0;    // for the final number

            int nameSize = 4;
            if(version == 2)
            {
                nameSize = 3;
            }
            else if((version == 3) || (version == 4))
            {
                nameSize = 4;
            }

            id3v2Frame f1 = new id3v2Frame();
            f1.frameName = new string(br.ReadChars(nameSize));
            f1.MajorVersion = version;


            // in order to check for padding I have to build a string of 4 (or 3 if v2.2) null bytes
            // there must be a better way to do this
            char nullChar = System.Convert.ToChar(0);
            StringBuilder sb = new StringBuilder(0, nameSize);
            sb.Append(nullChar);
            sb.Append(nullChar);
            sb.Append(nullChar);
            if(nameSize == 4)
            {
                sb.Append(nullChar);
            }

            if(f1.frameName == sb.ToString())
            {
                f1.padding = true;
                return f1;
            }


            if(version == 2)
            {
                // only have 3 bytes for size ;


                tagSize = br.ReadChars(3);    // I use this to read the bytes in from the file
                bytes = new int[3];      // for bit shifting
                newSize = 0;    // for the final number
                // The ID3v2 tag size is encoded with four bytes
                // where the most significant bit (bit 7)
                // is set to zero in every byte,
                // making a total of 28 bits.
                // The zeroed bits are ignored
                //
                // Some bit grinding is necessary.  Hang on.

                bytes[3] = tagSize[2] | ((tagSize[1] & 1) << 7);
                bytes[2] = ((tagSize[1] >> 1) & 63) | ((tagSize[0] & 3) << 6);
                bytes[1] = ((tagSize[0] >> 2) & 31);

                newSize = ((UInt64)bytes[3] |
                    ((UInt64)bytes[2] << 8) |
                    ((UInt64)bytes[1] << 16));
                //End Dan Code
            }


            else if(version == 3 || version == 4)
            {
                // version  2.4
                tagSize = br.ReadChars(4);    // I use this to read the bytes in from the file
                bytes = new int[4];      // for bit shifting
                newSize = 0;    // for the final number

                // The ID3v2 tag size is encoded with four bytes
                // where the most significant bit (bit 7)
                // is set to zero in every byte,
                // making a total of 28 bits.
                // The zeroed bits are ignored
                //
                // Some bit grinding is necessary.  Hang on.


                bytes[3] = tagSize[3] | ((tagSize[2] & 1) << 7);
                bytes[2] = ((tagSize[2] >> 1) & 63) | ((tagSize[1] & 3) << 6);
                bytes[1] = ((tagSize[1] >> 2) & 31) | ((tagSize[0] & 7) << 5);
                bytes[0] = ((tagSize[0] >> 3) & 15);

                newSize = ((UInt64)bytes[3] |
                    ((UInt64)bytes[2] << 8) |
                    ((UInt64)bytes[1] << 16) |
                    ((UInt64)bytes[0] << 24));
                //End Dan Code
            }
            f1.frameSize = newSize;


            if(version > 2)
            {
                // versions 3+ have frame tags.
                if(version == 3)
                {

                    bool[] c;
                    // read teh tags
                    c = BitReader.ToBitBool(br.ReadByte());
                    f1.F_TagAlterPreservation = c[0];
                    f1.F_FileAlterPreservation = c[1];
                    f1.F_ReadOnly = c[2];

                    c = BitReader.ToBitBool(br.ReadByte());
                    f1.F_Compression = c[0];
                    f1.F_Encryption = c[1];
                    f1.F_GroupingIdentity = c[2];
                }
                else if(version == 4)
                {
                    //%0abc0000 %0h00kmnp
                    //a - Tag alter preservation
                    //     0     Frame should be preserved.
                    //     1     Frame should be discarded.
                    // b - File alter preservation
                    //  0     Frame should be preserved.
                    //1     Frame should be discarded.
                    //  c - Read only
                    //  h - Grouping identity
                    //    0     Frame does not contain group information
                    //    1     Frame contains group information
                    //   k - Compression
                    //     0     Frame is not compressed.
                    //     1     Frame is compressed using zlib [zlib] deflate method.
                    //           If set, this requires the 'Data Length Indicator' bit
                    //           to be set as well.
                    //  m - Encryption
                    //     0     Frame is not encrypted.
                    //     1     Frame is encrypted.
                    //  n - Unsynchronisation
                    //      0     Frame has not been unsynchronised.
                    //      1     Frame has been unsyrchronised.
                    //   p - Data length indicator
                    //     0      There is no Data Length Indicator.
                    //    1      A data length Indicator has been added to the frame.


                    bool[] c;
                    // read teh tags
                    c = BitReader.ToBitBool(br.ReadByte());
                    f1.F_TagAlterPreservation = c[1];
                    f1.F_FileAlterPreservation = c[2];
                    f1.F_ReadOnly = c[3];

                    c = BitReader.ToBitBool(br.ReadByte());
                    f1.F_GroupingIdentity = c[1];
                    f1.F_Compression = c[4];
                    f1.F_Encryption = c[5];
                    f1.F_Unsynchronisation = c[6];
                    f1.F_DataLengthIndicator = c[7];
                }

                if(f1.frameSize > 0)
                {
                    f1.frameContents = br.ReadBytes((int)f1.frameSize);
                }
            }
            return f1;
        }

    }
}