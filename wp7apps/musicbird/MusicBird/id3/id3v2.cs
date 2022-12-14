using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Globalization;

namespace MusicBird
{
    /// <summary>
    /// Summary description for ID3v2.
    /// </summary>
    public class ID3v2 : IDisposable
    {
        public delegate void TagsReadEventHandler( object sender, string artist, string title, string url );
        public event TagsReadEventHandler TagsRead;

        public string fileName;
        public Uri uri;

        // id3v2 header
        public int MajorVersion;
        public int MinorVersion;

        public bool FA_Unsynchronisation;
        public bool FB_ExtendedHeader;
        public bool FC_ExperimentalIndicator;
        public bool FD_Footer;

        // ext header 
        public ulong extHeaderSize;
        public int extNumFlagBytes;

        public bool EB_Update;
        public bool EC_CRC;
        public bool ED_Restrictions;

        public ulong extC_CRC;
        public byte extD_Restrictions;



        private BinaryReader br;
        public bool hasTag;

        public ulong headerSize;

        public Dictionary<String, id3v2Frame> framesHash;
        public List<id3v2Frame> frames;

        //private bool fileOpen;

        //public bool header;

        // song info
        public string Title;
        public string Artist;
        public string Album;
        public string Year;
        public string Comment;
        public string Genre;
        public string Track;
        public string totalTracks;




        private void Initialize_Components()
        {
            this.fileName = "";
            this.uri = null;

            this.MajorVersion = 0;
            this.MinorVersion = 0;

            this.FA_Unsynchronisation = false;
            this.FB_ExtendedHeader = false;
            this.FC_ExperimentalIndicator = false;

            //this.fileOpen = false;

            this.frames = new List<id3v2Frame>();
            this.framesHash = new Dictionary<String, id3v2Frame>();

            this.Album = "";
            this.Artist = "";
            this.Comment = "";
            this.extC_CRC = 0;
            this.EB_Update = false;
            this.EC_CRC = false;
            this.ED_Restrictions = false;
            this.extD_Restrictions = 0;
            this.extHeaderSize = 0;
            this.extNumFlagBytes = 0;
            //this.fileOpen = false;
            this.hasTag = false;
            this.headerSize = 0;
            this.Title = "";
            this.totalTracks = "";
            this.Track = "";
            this.Year = "";

        }


        public ID3v2()
        {
            Initialize_Components();
        }

        public ID3v2( string fileName )
        {
            Initialize_Components();
            this.fileName = fileName;
        }

        public ID3v2( Uri uri ) {
            Initialize_Components();
            this.uri = uri;
        }

        private void ReadHeader()
        {


            // bring in the first three bytes.  it must be ID3 or we have no tag
            // TODO add logic to check the end of the file for "3D1" and other
            // possible starting spots
            string id3start = new string(this.br.ReadChars(3));

            // check for a tag
            if(!id3start.Equals("ID3"))
            {
                // TODO we are fucked.
                //throw id3v2ReaderException;
                this.hasTag = false;
                return;

            }
            else
            {
                this.hasTag = true;

                // read id3 version.  2 bytes:
                // The first byte of ID3v2 version is it's major version,
                // while the second byte is its revision number
                this.MajorVersion = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);
                this.MinorVersion = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);

                // read next byte for flags
                bool[] boolar = BitReader.ToBitBool(this.br.ReadByte());
                // set the flags
                this.FA_Unsynchronisation = boolar[0];
                this.FB_ExtendedHeader = boolar[1];
                this.FC_ExperimentalIndicator = boolar[2];

                // read teh size
                // this code is courtesy of Daniel E. White w/ minor modifications by me  Thanx Dan
                //Dan Code 
                char[] tagSize = this.br.ReadChars(4);    // I use this to read the bytes in from the file
                int[] bytes = new int[4];      // for bit shifting
                ulong newSize = 0;    // for the final number
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

                newSize = ((UInt64)10 + (UInt64)bytes[3] |
                    ((UInt64)bytes[2] << 8) |
                    ((UInt64)bytes[1] << 16) |
                    ((UInt64)bytes[0] << 24));
                //End Dan Code

                this.headerSize = newSize;
            }
        }

        private void ReadFrames()
        {
            id3v2Frame f = new id3v2Frame();
            do
            {
                f = f.ReadFrame(this.br, this.MajorVersion);

                // check if we have hit the padding.
                if(f.padding == true)
                {
                    //we hit padding.  lets advance to end and stop reading.
                    this.br.BaseStream.Position = System.Convert.ToInt64(this.headerSize);
                    break;
                }
                this.frames.Add(f);
                this.framesHash.Add(f.frameName, f);

            } while(this.br.BaseStream.Position <= System.Convert.ToInt64(this.headerSize));
        }

        public void Read()
        {
            if(!String.IsNullOrEmpty(this.fileName))
            {
                using(IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using(IsolatedStorageFileStream fs = store.OpenFile(this.fileName, FileMode.Open, FileAccess.Read))
                    {
                        readFromStream(fs);
                        fs.Close();
                    }
                }
            }
            else if(this.uri != null)
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(this.uri);
                request.Headers["Range"] = "bytes=0-1000";
                request.BeginGetResponse(( ar ) =>
                {
                    /*var response = request.EndGetResponse(ar);
                    using(var stream = response.GetResponseStream())
                    {
                        System.Diagnostics.Debug.WriteLine(stream);
                        readFromStream(stream);
                    }*/

                    var response = request.EndGetResponse(ar);
                    using(var stream = response.GetResponseStream())
                    {
                        readFromStream(stream);
                    }
                    
                }, null);
            }
            else {
                throw new Exception("no fileName / url");
            }
        }

        private void readFromStream( Stream fs ) {
            br = new BinaryReader(fs);
            ReadHeader();

            if(this.hasTag)
            {
                if(this.FB_ExtendedHeader)
                {
                    ReadExtendedHeader();
                }

                ReadFrames();

                if(this.FD_Footer)
                {
                    ReadFooter();
                }
                ParseCommonHeaders();
            }

            this.br.Close();
            if(this.TagsRead != null)
            {
                this.TagsRead(null, this.Artist, this.Title, this.uri.ToString());
                System.Diagnostics.Debug.WriteLine("Found " + this.Artist + " " + this.Title);
            }
        }

        private void ParseCommonHeaders()
        {
            if(this.MajorVersion == 2)
            {
                if(this.framesHash.ContainsKey("TT2"))
                {

                    byte[] bytes = ((id3v2Frame)this.framesHash["TT2"]).frameContents;
                    StringBuilder sb = new StringBuilder();


                    for(int i = 1; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }

                        //this.Title = myString.Substring(1);
                        this.Title = sb.ToString();
                    }
                }



                if(this.framesHash.ContainsKey("TP1"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TP1"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Artist = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TAL"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TAL"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Album = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TYE"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TYE"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Year = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TRK"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TRK"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Track = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TCO"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TCO"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Genre = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("COM"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["COM"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Comment = sb.ToString();
                    }
                }
            }
            else
            { // $id3info["majorversion"] > 2
                if(this.framesHash.ContainsKey("TIT2"))
                {

                    byte[] bytes = ((id3v2Frame)this.framesHash["TIT2"]).frameContents;
                    StringBuilder sb = new StringBuilder();
                    


                    for(int i = 1; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }

                        //this.Title = myString.Substring(1);
                        this.Title = sb.ToString();
                    }
                }



                if(this.framesHash.ContainsKey("TPE1"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TPE1"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Artist = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TALB"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TALB"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Album = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TYER"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TYER"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Year = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TRCK"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TRCK"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Track = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("TCON"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["TCON"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Genre = sb.ToString();
                    }
                }
                if(this.framesHash.ContainsKey("COMM"))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] bytes = ((id3v2Frame)this.framesHash["COMM"]).frameContents;
                    

                    for(int i = 0; i < bytes.Length; i++)
                    {
                        if(i == 0)
                        {
                            // read the text encoding.
                            
                        }
                        else
                        {
                            sb.Append(System.Convert.ToChar(bytes[i]));
                        }
                        this.Comment = sb.ToString();
                    }
                }
            }

            string[] trackHolder = this.Track.Split('/');
            this.Track = trackHolder[0];
            if(trackHolder.Length > 1)
                this.totalTracks = trackHolder[1];

        }

        private void ReadFooter()
        {

            // bring in the first three bytes.  it must be ID3 or we have no tag
            // TODO add logic to check the end of the file for "3D1" and other
            // possible starting spots
            string id3start = new string(this.br.ReadChars(3));

            // check for a tag
            if(!id3start.Equals("3DI"))
            {
                // TODO we are fucked.  not really we just don't ahve a tag
                // and we need to bail out gracefully.
                //throw id3v23ReaderException;
            }
            else
            {
                // we have a tag
                this.hasTag = true;
            }

            // read id3 version.  2 bytes:
            // The first byte of ID3v2 version is it's major version,
            // while the second byte is its revision number
            this.MajorVersion = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);
            this.MinorVersion = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);

            // here is where we get fancy.  I am useing silisoft's php code as 
            // a reference here.  we are going to try and parse for 2.2, 2.3 and 2.4
            // in one pass.  hold on!!

            if((this.hasTag) && (this.MajorVersion <= 4)) // probably won't work on higher versions
            {
                // (%ab000000 in v2.2, %abc00000 in v2.3, %abcd0000 in v2.4.x)
                // read next byte for flags
                bool[] boolar = BitReader.ToBitBool(this.br.ReadByte());
                // set the flags
                if(this.MajorVersion == 2)
                {
                    this.FA_Unsynchronisation = boolar[0];
                    this.FB_ExtendedHeader = boolar[1];
                }
                else if(this.MajorVersion == 3)
                {
                    // set the flags
                    this.FA_Unsynchronisation = boolar[0];
                    this.FB_ExtendedHeader = boolar[1];
                    this.FC_ExperimentalIndicator = boolar[2];
                }
                else if(this.MajorVersion == 4)
                {
                    // set the flags
                    this.FA_Unsynchronisation = boolar[0];
                    this.FB_ExtendedHeader = boolar[1];
                    this.FC_ExperimentalIndicator = boolar[2];
                    this.FD_Footer = boolar[3];
                }

                // read teh size
                // this code is courtesy of Daniel E. White w/ minor modifications by me  Thanx Dan
                //Dan Code       
                char[] tagSize = this.br.ReadChars(4);    // I use this to read the bytes in from the file
                int[] bytes = new int[4];      // for bit shifting
                ulong newSize = 0;    // for the final number
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

                newSize = ((UInt64)10 + (UInt64)bytes[3] |
                    ((UInt64)bytes[2] << 8) |
                    ((UInt64)bytes[1] << 16) |
                    ((UInt64)bytes[0] << 24));
                //End Dan Code

                this.headerSize = newSize;


            }
        }

        private void ReadExtendedHeader()
        {

            //			Extended header size   4 * %0xxxxxxx
            //			Number of flag bytes       $01
            //			Extended Flags             $xx
            //			Where the 'Extended header size' is the size of the whole extended header, stored as a 32 bit synchsafe integer.
            // read teh size
            // this code is courtesy of Daniel E. White w/ minor modifications by me  Thanx Dan
            //Dan Code 
            char[] extHeaderSize = this.br.ReadChars(4);    // I use this to read the bytes in from the file
            int[] bytes = new int[4];      // for bit shifting
            ulong newSize = 0;    // for the final number
            // The ID3v2 tag size is encoded with four bytes
            // where the most significant bit (bit 7)
            // is set to zero in every byte,
            // making a total of 28 bits.
            // The zeroed bits are ignored
            //
            // Some bit grinding is necessary.  Hang on.


            bytes[3] = extHeaderSize[3] | ((extHeaderSize[2] & 1) << 7);
            bytes[2] = ((extHeaderSize[2] >> 1) & 63) | ((extHeaderSize[1] & 3) << 6);
            bytes[1] = ((extHeaderSize[1] >> 2) & 31) | ((extHeaderSize[0] & 7) << 5);
            bytes[0] = ((extHeaderSize[0] >> 3) & 15);

            newSize = ((UInt64)10 + (UInt64)bytes[3] |
                ((UInt64)bytes[2] << 8) |
                ((UInt64)bytes[1] << 16) |
                ((UInt64)bytes[0] << 24));
            //End Dan Code

            this.extHeaderSize = newSize;
            // next we read the number of flag bytes

            this.extNumFlagBytes = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);

            // read the flag byte(s) and set the flags
            bool[] extFlags = BitReader.ToBitBools(this.br.ReadBytes(this.extNumFlagBytes));

            this.EB_Update = extFlags[1];
            this.EC_CRC = extFlags[2];
            this.ED_Restrictions = extFlags[3];

            // check for flags
            if(this.EB_Update)
            {
                // this tag has no data but will have a null byte so we need to read it in
                //Flag data length      $00

                this.br.ReadByte();
            }

            if(this.EC_CRC)
            {
                
                //       Total frame CRC    5 * %0xxxxxxx
                // read the first byte and check to make sure it is 5.  if not the header is corrupt
                // we will still try to process but we may be funked.

                int extC_FlagDataLength = System.Convert.ToInt32(this.br.ReadByte(), CultureInfo.InvariantCulture);
                if(extC_FlagDataLength == 5)
                {


                    extHeaderSize = this.br.ReadChars(5);    // I use this to read the bytes in from the file
                    bytes = new int[4];      // for bit shifting
                    newSize = 0;    // for the final number
                    // The ID3v2 tag size is encoded with four bytes
                    // where the most significant bit (bit 7)
                    // is set to zero in every byte,
                    // making a total of 28 bits.
                    // The zeroed bits are ignored
                    //
                    // Some bit grinding is necessary.  Hang on.


                    bytes[4] = extHeaderSize[4] | ((extHeaderSize[3] & 1) << 7);
                    bytes[3] = ((extHeaderSize[3] >> 1) & 63) | ((extHeaderSize[2] & 3) << 6);
                    bytes[2] = ((extHeaderSize[2] >> 2) & 31) | ((extHeaderSize[1] & 7) << 5);
                    bytes[1] = ((extHeaderSize[1] >> 3) & 15) | ((extHeaderSize[0] & 15) << 4);
                    bytes[0] = ((extHeaderSize[0] >> 4) & 7);

                    newSize = ((UInt64)10 + (UInt64)bytes[4] |
                        ((UInt64)bytes[3] << 8) |
                        ((UInt64)bytes[2] << 16) |
                        ((UInt64)bytes[1] << 24) |
                        ((UInt64)bytes[0] << 32));

                    this.extHeaderSize = newSize;
                }
                else
                {
                    // we are fucked.  do something
                }
            }

            if(this.ED_Restrictions)
            {
                // Flag data length       $01
                // restrictions           %ppqrrstt

                // advance past flag data lenght byte
                this.br.ReadByte();

                this.extD_Restrictions = this.br.ReadByte();
            }

        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose( bool disposing )
        {
            if(disposing)
            {
                // free managed resources
                if(br != null)
                {
                    br.Dispose();
                    br = null;
                }
            }
        }
    }
}
